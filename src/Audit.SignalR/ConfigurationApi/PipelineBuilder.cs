using System;
using Audit.Core;

namespace Audit.SignalR.Configuration
{
    internal class PipelineBuilder : IPipelineBuilder
    {
        internal string _eventType;
        internal bool _includeHeaders;
        internal bool _includeQueryString;
        internal bool _auditDisabled;
        internal EventCreationPolicy? _creationPolicy;
        internal AuditDataProvider _dataProvider;
        internal IPipelineBuilderFilters _filters;

        public IPipelineBuilder Filters(Action<IPipelineBuilderFilters> config)
        {
            _filters = new PipelineBuilderFilters();
            config.Invoke(_filters);
            return this;
        }

        public void DisableAudit()
        {
            _auditDisabled = true;
        }

        public IPipelineBuilder EventType(string eventType)
        {
            _eventType = eventType;
            return this;
        }

        public IPipelineBuilder IncludeHeaders(bool include = true)
        {
            _includeHeaders = include;
            return this;
        }

        public IPipelineBuilder IncludeQueryString(bool include = true)
        {
            _includeQueryString = include;
            return this;
        }

        public IPipelineBuilder WithCreationPolicy(EventCreationPolicy? policy)
        {
            _creationPolicy = policy;
            return this;
        }

        public IPipelineBuilder WithDataProvider(AuditDataProvider provider)
        {
            _dataProvider = provider;
            return this;
        }
    }
}