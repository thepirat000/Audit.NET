using System;
using Audit.Core;

namespace Audit.SignalR.Configuration
{
    internal class AuditHubConfigurator : IAuditHubConfigurator
    {
        internal string _eventType;
        internal bool _includeHeaders;
        internal bool _includeQueryString;
        internal bool _auditDisabled;
        internal IAuditHubFilterConfigurator _filters;
        internal EventCreationPolicy? _creationPolicy;
        internal AuditDataProvider _dataProvider;

        public IAuditHubConfigurator Filters(Action<IAuditHubFilterConfigurator> config)
        {
            _filters = new AuditHubFilterConfigurator();
            config.Invoke(_filters);
            return this;
        }

        public void DisableAudit()
        {
            _auditDisabled = true;
        }

        public IAuditHubConfigurator EventType(string eventType)
        {
            _eventType = eventType;
            return this;
        }

        public IAuditHubConfigurator IncludeHeaders(bool include = true)
        {
            _includeHeaders = include;
            return this;
        }

        public IAuditHubConfigurator IncludeQueryString(bool include = true)
        {
            _includeQueryString = include;
            return this;
        }

        public IAuditHubConfigurator WithCreationPolicy(EventCreationPolicy? policy)
        {
            _creationPolicy = policy;
            return this;
        }

        public IAuditHubConfigurator WithDataProvider(AuditDataProvider provider)
        {
            _dataProvider = provider;
            return this;
        }
    }
}