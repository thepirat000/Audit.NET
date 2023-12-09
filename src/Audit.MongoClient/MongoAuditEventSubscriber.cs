using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Audit.Core;
using Audit.Core.Extensions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;

namespace Audit.MongoClient
{
    /// <summary>
    /// A MongoDB event subscriber that logs the commands using Audit.NET framework
    /// </summary>
    public class MongoAuditEventSubscriber : IEventSubscriber
    {
        private readonly ConfigurationApi.AuditMongoConfigurator _config = new ConfigurationApi.AuditMongoConfigurator();
        private readonly IEventSubscriber _subscriber;
        private static readonly HashSet<string> IgnoredCommands = new HashSet<string>(new[] { "isMaster", "buildInfo", "getLastError", "saslStart", "saslContinue" });

        // RequestId -> CommandStartedEvent
        internal readonly ConcurrentDictionary<int, IAuditScope> _requestBuffer = new ConcurrentDictionary<int, IAuditScope>();

        /// <summary>
        /// Specifies the event type name to use in the audit output. The following placeholders can be used as part of the string: 
        /// - {command}: replaced with the command name.
        /// </summary>
        public string EventType { set => _config._eventTypePredicate = _ => value; }

        /// <summary>
        /// Specifies whether the audit event should include the server reply. The reply is not included by default.
        /// </summary>
        public bool IncludeReply { set => _config._includeReplyPredicate = _ => value; }
        
        /// <summary>
        /// Sets a filter function to determine if a command should be logged. By default all commands are logged.
        /// </summary>
        public Func<CommandStartedEvent, bool> CommandFilter { set => _config._commandFilter = value; }

        /// <summary>
        /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
        /// </summary>
        public EventCreationPolicy? CreationPolicy { set => _config._eventCreationPolicy = value; }
        /// <summary>
        /// Specifies the audit data provider to use. Default is NULL to use the globally configured data provider.
        /// </summary>
        public AuditDataProvider AuditDataProvider { set => _config._auditDataProvider = value; }
        /// <summary>
        /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
        /// </summary>
        public IAuditScopeFactory AuditScopeFactory { set => _config._auditScopeFactory = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuditEventSubscriber"/> class with the default configuration.
        /// </summary>
        public MongoAuditEventSubscriber()
        {
            _subscriber = new ReflectionEventSubscriber(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoAuditEventSubscriber"/> with the given fluent configuration.
        /// </summary>
        public MongoAuditEventSubscriber(Action<ConfigurationApi.IAuditMongoConfigurator> config)
            : this()
        {
            if (config != null)
            {
                config.Invoke(_config);
            }
        }

        public bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)
        {
            return _subscriber.TryGetEventHandler(out handler);
        }

        /// <summary>
        /// Handles the Command Started Event
        /// </summary>
        public void Handle(CommandStartedEvent ev)
        {
            if (ShouldSkipEvent(ev.CommandName))
            {
                return;
            }

            if (_config._commandFilter != null && !_config._commandFilter(ev))
            {
                return;
            }

            var eventType = (_config._eventTypePredicate?.Invoke(ev) ?? "{command}")
                .Replace("{command}", ev.CommandName);
            
            // Create the audit event 
            var auditEvent = new AuditEventMongoCommand()
            {
                Command = new MongoCommandEvent()
                {
                    CommandStartedEvent = ev,
                    RequestId = ev.RequestId,
                    Connection = new MongoConnection()
                    {
                        ClusterId = ev.ConnectionId.ServerId.ClusterId.Value,
                        Endpoint = ev.ConnectionId.ServerId.EndPoint.ToString(),
                        LocalConnectionId = ev.ConnectionId.LongLocalValue,
                        ServerConnectionId = ev.ConnectionId.LongServerValue
                    },
                    OperationId = ev.OperationId,
                    CommandName = ev.CommandName,
                    Body = BsonTypeMapper.MapToDotNetValue(ev.Command),
                    Timestamp = ev.Timestamp
                }
            };
            
            // Create the audit scope
            var scopeOptions = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEvent,
                CreationPolicy = _config._eventCreationPolicy,
                DataProvider = _config._auditDataProvider
            };
            var auditScopeFactory = _config._auditScopeFactory ?? Configuration.AuditScopeFactory;
            var auditScope = auditScopeFactory.Create(scopeOptions);

            // Store the scope in the buffer
            _requestBuffer.TryAdd(ev.RequestId, auditScope);
        }

        /// <summary>
        /// Handles the Command Failed Event
        /// </summary>
        public void Handle(CommandFailedEvent ev)
        {
            if (ShouldSkipEvent(ev.CommandName))
            {
                return;
            }

            // Get the audit scope from the buffer
            if (!_requestBuffer.TryRemove(ev.RequestId, out var auditScope))
            {
                return;
            }
            
            // Update the audit event
            var auditEvent = auditScope.EventAs<AuditEventMongoCommand>();

            auditEvent.Command.Error = ev.Failure.GetExceptionInfo();
            auditEvent.Command.Duration = Convert.ToInt32(ev.Duration.TotalMilliseconds);
            auditEvent.Command.Success = false;

            // Dispose the audit scope to trigger saving
            auditScope.Dispose();
        }

        /// <summary>
        /// Handles the Command Succeeded Event
        /// </summary>
        public void Handle(CommandSucceededEvent ev)
        {
            if (ShouldSkipEvent(ev.CommandName))
            {
                return;
            }

            if (!_requestBuffer.TryRemove(ev.RequestId, out var auditScope))
            {
                return;
            }
            
            // Update the audit event
            var auditEvent = auditScope.EventAs<AuditEventMongoCommand>();

            if (_config._includeReplyPredicate?.Invoke(ev) == true)
            {
                auditEvent.Command.Reply = BsonTypeMapper.MapToDotNetValue(ev.Reply);
            }
            
            auditEvent.Command.Duration = Convert.ToInt32(ev.Duration.TotalMilliseconds);
            auditEvent.Command.Success = true;
            
            // Dispose the audit scope 
            auditScope.Dispose();
        }

        private static bool ShouldSkipEvent(string commandName)
        {
            return Configuration.AuditDisabled || IgnoredCommands.Contains(commandName);
        }
    }
}
