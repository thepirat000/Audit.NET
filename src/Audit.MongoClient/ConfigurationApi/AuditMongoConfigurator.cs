using System;
using Audit.Core;
using MongoDB.Driver.Core.Events;

namespace Audit.MongoClient.ConfigurationApi
{
    public class AuditMongoConfigurator : IAuditMongoConfigurator
    {
        internal Func<CommandSucceededEvent, bool> _includeReplyPredicate;
        internal Func<CommandStartedEvent, bool> _commandFilter;
        internal Func<CommandStartedEvent, string> _eventTypePredicate;
        internal EventCreationPolicy? _eventCreationPolicy;
        internal AuditDataProvider _auditDataProvider;
        internal IAuditScopeFactory _auditScopeFactory;

        /// <inheritdoc />
        public IAuditMongoConfigurator IncludeReply(Func<CommandSucceededEvent, bool> includeReplyPredicate)
        {
            _includeReplyPredicate = includeReplyPredicate;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator IncludeReply(bool include = true)
        {
            _includeReplyPredicate = _ => include;
            return this;
        }
        
        /// <inheritdoc />
        public IAuditMongoConfigurator EventType(Func<CommandStartedEvent, string> eventTypePredicate)
        {
            _eventTypePredicate = eventTypePredicate;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator EventType(string eventType)
        {
            _eventTypePredicate = _ => eventType;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator CommandFilter(Func<CommandStartedEvent, bool> commandFilter)
        {
            _commandFilter = commandFilter;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
        {
            _auditScopeFactory = auditScopeFactory;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy)
        {
            _eventCreationPolicy = eventCreationPolicy;
            return this;
        }

        /// <inheritdoc />
        public IAuditMongoConfigurator AuditDataProvider(AuditDataProvider auditDataProvider)
        {
            _auditDataProvider = auditDataProvider;
            return this;
        }
    }
}
