using Audit.Core;

using Hangfire.Client;

using System;
using System.Collections.Generic;

namespace Audit.Hangfire.ConfigurationApi
{
    public class AuditHangfireJobCreationConfigurator : IAuditHangfireJobCreationConfigurator
    {
        public AuditJobCreationOptions Options { get; } = new AuditJobCreationOptions();

        public IAuditHangfireJobCreationConfigurator AuditWhen(Func<CreateContext, bool> jobFilter)
        {
            Options.AuditWhen = jobFilter;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator IncludeParameters(Func<CreateContext, bool> includeParameters)
        {
            Options.IncludeParameters = includeParameters;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator IncludeParameters(bool includeParameters = true)
        {
            Options.IncludeParameters = _ => includeParameters;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator ExcludeArguments(Func<CreateContext, bool> excludeArguments)
        {
            Options.ExcludeArguments = excludeArguments;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator ExcludeArguments(bool excludeArguments = true)
        {
            Options.ExcludeArguments = _ => excludeArguments;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator EventType(Func<CreateContext, string> eventType)
        {
            Options.EventType = eventType;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator EventType(string eventTypeTemplate)
        {
            // Allow string templates like "{type}.{method}" to be applied later in the attribute.
            Options.EventType = _ => eventTypeTemplate;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator DataProvider(Func<CreateContext, IAuditDataProvider> dataProvider)
        {
            Options.DataProvider = dataProvider;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator DataProvider(IAuditDataProvider dataProvider)
        {
            Options.DataProvider = _ => dataProvider;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator EventCreationPolicy(EventCreationPolicy eventCreationPolicy)
        {
            Options.EventCreationPolicy = eventCreationPolicy;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory)
        {
            Options.AuditScopeFactory = auditScopeFactory;
            return this;
        }

        public IAuditHangfireJobCreationConfigurator WithCustomFields(Func<CreateContext, Dictionary<string, object>> customFields)
        {
            Options.CustomFields = customFields;
            return this;
        }

}
}
