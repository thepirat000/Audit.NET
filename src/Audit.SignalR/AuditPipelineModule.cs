#if ASP_NET
using Audit.Core;
using Audit.Core.Extensions;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Linq;

namespace Audit.SignalR
{
    /// <summary>
    /// Audit Pipeline Module that intercepts SignalR events and generate Audit Logs.
    /// </summary>
    public partial class AuditPipelineModule : HubPipelineModule
    {
        internal const string AuditScopeIncomingEnvironmentKey = "AuditScope_Incoming";
        internal const string AuditScopeConnectEnvironmentKey = "AuditScope_Connect";
        internal const string AuditScopeDisconnectEnvironmentKey = "AuditScope_Disconnect";
        internal const string AuditScopeReconnectEnvironmentKey = "AuditScope_Reconnect";

        public Func<SignalrEventIncoming, bool> IncomingEventsFilter { get; set; }
        public Func<SignalrEventOutgoing, bool> OutgoingEventsFilter { get; set; }
        public Func<SignalrEventConnect, bool> ConnectEventsFilter { get; set; }
        public Func<SignalrEventDisconnect, bool> DisconnectEventsFilter { get; set; }
        public Func<SignalrEventReconnect, bool> ReconnectEventsFilter { get; set; }
        public Func<SignalrEventError, bool> ErrorEventsFilter { get; set; }

        public IAuditDataProvider AuditDataProvider { get; set; }
        public IAuditScopeFactory AuditScopeFactory { get; set; }
        public EventCreationPolicy? CreationPolicy { get; set; }
        public string AuditEventType { get; set; }
        public bool AuditDisabled { get; set; }
        public bool IncludeHeaders { get; set; }
        public bool IncludeQueryString { get; set; }

        public AuditPipelineModule() { }

        public AuditPipelineModule(IAuditDataProvider dataProvider, EventCreationPolicy? creationPolicy = null,
            string auditEventType = null, bool includeHeaders = false, bool includeQueryString = false, bool auditDisabled = false, IAuditScopeFactory auditScopeFactory = null)
        {
            AuditDataProvider = dataProvider;
            CreationPolicy = creationPolicy;
            AuditEventType = auditEventType;
            AuditDisabled = auditDisabled;
            IncludeHeaders = includeHeaders;
            IncludeQueryString = includeQueryString;
            AuditScopeFactory = auditScopeFactory;
        }

        private bool AuditEventEnabled(SignalrEventBase @event)
        {
            switch(@event.GetType().Name)
            {
                case nameof(SignalrEventIncoming):
                    return IncomingEventsFilter?.Invoke(@event as SignalrEventIncoming) ?? true;
                case nameof(SignalrEventOutgoing):
                    return OutgoingEventsFilter?.Invoke(@event as SignalrEventOutgoing) ?? true;
                case nameof(SignalrEventConnect):
                    return ConnectEventsFilter?.Invoke(@event as SignalrEventConnect) ?? true;
                case nameof(SignalrEventDisconnect):
                    return DisconnectEventsFilter?.Invoke(@event as SignalrEventDisconnect) ?? true;
                case nameof(SignalrEventReconnect):
                    return ReconnectEventsFilter?.Invoke(@event as SignalrEventReconnect) ?? true;
                case nameof(SignalrEventError):
                    return ErrorEventsFilter?.Invoke(@event as SignalrEventError) ?? true;
                default:
                    return true;
            }
        }

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context)
        {
            if (AuditDisabled)
            {
                return base.OnBeforeIncoming(context);
            }
            var signalrEvent = new SignalrEventIncoming()
            {
                ConnectionId = context.Hub.Context.ConnectionId,
                Args = context.Args?.ToList(),
                Headers = IncludeHeaders ? context.Hub.Context.Headers?.ToDictionary(k => k.Key, v => v.Value) : null,
                QueryString = IncludeQueryString ? context.Hub.Context.QueryString?.ToDictionary(k => k.Key, v => v.Value) : null,
                LocalPath = context.Hub.Context.Request.LocalPath,
                IdentityName = context.Hub.Context.User?.Identity?.Name,
                HubName = context.MethodDescriptor.Hub?.Name,
                HubType = context.MethodDescriptor.Hub?.HubType.FullName,
                MethodName = context.MethodDescriptor.Name,
                InvokerContext = context
            };
            if (AuditEventEnabled(signalrEvent))
            {
                var scope = CreateAuditScope(signalrEvent);
                context.Hub.Context.Request.Environment[AuditScopeIncomingEnvironmentKey] = scope;
            }
            return base.OnBeforeIncoming(context);
        }
        protected override object OnAfterIncoming(object result, IHubIncomingInvokerContext context)
        {
            if (AuditDisabled || !context.Hub.Context.Request.Environment.ContainsKey(AuditScopeIncomingEnvironmentKey))
            {
                return base.OnAfterIncoming(result, context);
            }
            object @return;
            using (var scope = context.Hub.Context.Request.Environment[AuditScopeIncomingEnvironmentKey] as AuditScope)
            {
                (scope.EventAs<AuditEventSignalr>().Event as SignalrEventIncoming).Result = result;
                @return = base.OnAfterIncoming(result, context);
            }
            context.Hub.Context.Request.Environment.Remove(AuditScopeIncomingEnvironmentKey);
            return @return;
        }

        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context)
        {
            return base.OnBeforeOutgoing(context);
        }
        protected override void OnAfterOutgoing(IHubOutgoingInvokerContext context)
        {
            if (AuditDisabled)
            {
                base.OnAfterOutgoing(context);
                return;
            }
            var signalrEvent = new SignalrEventOutgoing()
            {
                Signal = context.Signal,
                Signals = context.Signals?.ToList(),
                HubName = context.Invocation.Hub,
                MethodName = context.Invocation.Method,
                Args = context.Invocation.Args?.ToList(),
                InvokerContext = context
            };
            if (AuditEventEnabled(signalrEvent))
            {
                using (var scope = CreateAuditScope(signalrEvent))
                {
                    base.OnAfterOutgoing(context);
                }
            }
            else
            {
                base.OnAfterOutgoing(context);
            }
        }

        protected override bool OnBeforeConnect(IHub hub)
        {
            if (AuditDisabled)
            {
                return base.OnBeforeConnect(hub);
            }
            var signalrEvent = new SignalrEventConnect()
            {
                ConnectionId = hub.Context.ConnectionId,
                Headers = IncludeHeaders ? hub.Context.Headers?.ToDictionary(k => k.Key, v => v.Value) : null,
                QueryString = IncludeQueryString ? hub.Context.QueryString?.ToDictionary(k => k.Key, v => v.Value) : null,
                LocalPath = hub.Context.Request.LocalPath,
                IdentityName = hub.Context.User?.Identity?.Name,
                HubReference = hub
            };
            if (AuditEventEnabled(signalrEvent))
            {
                var scope = CreateAuditScope(signalrEvent);
                hub.Context.Request.Environment[AuditScopeConnectEnvironmentKey] = scope;
            }
            return base.OnBeforeConnect(hub);
        }
        protected override void OnAfterConnect(IHub hub)
        {
            if (AuditDisabled || !hub.Context.Request.Environment.ContainsKey(AuditScopeConnectEnvironmentKey))
            {
                base.OnAfterConnect(hub);
                return;
            }
            using (var scope = hub.Context.Request.Environment[AuditScopeConnectEnvironmentKey] as AuditScope)
            {
                (scope.EventAs<AuditEventSignalr>().Event as SignalrEventConnect).ConnectionId = hub.Context.ConnectionId;
                base.OnAfterConnect(hub);
            }
            hub.Context.Request.Environment.Remove(AuditScopeConnectEnvironmentKey);
        }

        protected override bool OnBeforeDisconnect(IHub hub, bool stopCalled)
        {
            if (AuditDisabled)
            {
                return base.OnBeforeDisconnect(hub, stopCalled);
            }
            var signalrEvent = new SignalrEventDisconnect()
            {
                ConnectionId = hub.Context.ConnectionId,
                Headers = IncludeHeaders ? hub.Context.Headers?.ToDictionary(k => k.Key, v => v.Value) : null,
                QueryString = IncludeQueryString ? hub.Context.QueryString?.ToDictionary(k => k.Key, v => v.Value) : null,
                LocalPath = hub.Context.Request.LocalPath,
                IdentityName = hub.Context.User?.Identity?.Name,
                StopCalled = stopCalled,
                HubReference = hub
            };
            if (AuditEventEnabled(signalrEvent))
            {
                var scope = CreateAuditScope(signalrEvent);
                hub.Context.Request.Environment[AuditScopeDisconnectEnvironmentKey] = scope;
            }
            return base.OnBeforeDisconnect(hub, stopCalled);
        }
        protected override void OnAfterDisconnect(IHub hub, bool stopCalled)
        {
            if (AuditDisabled || !hub.Context.Request.Environment.ContainsKey(AuditScopeDisconnectEnvironmentKey))
            {
                base.OnAfterDisconnect(hub, stopCalled);
                return;
            }
            using (var scope = hub.Context.Request.Environment[AuditScopeDisconnectEnvironmentKey] as AuditScope)
            {
                (scope.EventAs<AuditEventSignalr>().Event as SignalrEventDisconnect).ConnectionId = hub.Context.ConnectionId;
                base.OnAfterDisconnect(hub, stopCalled);
            }
            hub.Context.Request.Environment.Remove(AuditScopeDisconnectEnvironmentKey);
        }

        protected override bool OnBeforeReconnect(IHub hub)
        {
            if (AuditDisabled)
            {
                return base.OnBeforeReconnect(hub);
            }
            var signalrEvent = new SignalrEventReconnect()
            {
                ConnectionId = hub.Context.ConnectionId,
                Headers = IncludeHeaders ? hub.Context.Headers?.ToDictionary(k => k.Key, v => v.Value) : null,
                QueryString = IncludeQueryString ? hub.Context.QueryString?.ToDictionary(k => k.Key, v => v.Value) : null,
                LocalPath = hub.Context.Request.LocalPath,
                IdentityName = hub.Context.User?.Identity?.Name,
                HubReference = hub
            };
            if (AuditEventEnabled(signalrEvent))
            {
                var scope = CreateAuditScope(signalrEvent);
                hub.Context.Request.Environment[AuditScopeReconnectEnvironmentKey] = scope;
            }
            return base.OnBeforeReconnect(hub);
        }
        protected override void OnAfterReconnect(IHub hub)
        {
            if (AuditDisabled || !hub.Context.Request.Environment.ContainsKey(AuditScopeReconnectEnvironmentKey))
            {
                base.OnAfterReconnect(hub);
                return;
            }
            using (var scope = hub.Context.Request.Environment[AuditScopeReconnectEnvironmentKey] as AuditScope)
            {
                (scope.EventAs<AuditEventSignalr>().Event as SignalrEventReconnect).ConnectionId = hub.Context.ConnectionId;
                base.OnAfterReconnect(hub);
            }
            hub.Context.Request.Environment.Remove(AuditScopeReconnectEnvironmentKey);
        }

        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            if (AuditDisabled)
            {
                base.OnIncomingError(exceptionContext, invokerContext);
                return;
            }
            var signalrEvent = new SignalrEventError()
            {
                ConnectionId = invokerContext.Hub.Context.ConnectionId,
                Args = invokerContext.Args?.ToList(),
                Headers = IncludeHeaders ? invokerContext.Hub.Context.Headers?.ToDictionary(k => k.Key, v => v.Value) : null,
                QueryString = IncludeQueryString ? invokerContext.Hub.Context.QueryString?.ToDictionary(k => k.Key, v => v.Value) : null,
                LocalPath = invokerContext.Hub.Context.Request.LocalPath,
                IdentityName = invokerContext.Hub.Context.User?.Identity?.Name,
                HubName = invokerContext.MethodDescriptor.Hub?.Name,
                HubType = invokerContext.MethodDescriptor.Hub?.HubType.FullName,
                MethodName = invokerContext.MethodDescriptor.Name,
                Exception = exceptionContext.Error.GetExceptionInfo(),
                ExceptionContext = exceptionContext,
                InvokerContext = invokerContext
            };
            if (AuditEventEnabled(signalrEvent))
            {
                using (var scope = CreateAuditScope(signalrEvent))
                {
                    base.OnIncomingError(exceptionContext, invokerContext);
                }
            }
            else
            {
                base.OnIncomingError(exceptionContext, invokerContext);
            }
        }
        
        private IAuditScope CreateAuditScope(SignalrEventBase signalrEvent)
        {
            var auditEvent = new AuditEventSignalr()
            {
                Event = signalrEvent
            };
            var factory = AuditScopeFactory ?? Core.Configuration.AuditScopeFactory;
            var scope = factory.Create(new AuditScopeOptions()
            {
                EventType = (AuditEventType ?? "{event}").Replace("{event}", signalrEvent.EventType.ToString()),
                AuditEvent = auditEvent,
                CreationPolicy = CreationPolicy,
                DataProvider = AuditDataProvider
            });
            return scope;
        }
    }
}
#endif