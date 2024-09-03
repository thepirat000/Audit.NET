using System;
using System.Collections.Generic;
using System.Reflection;
using Audit.Core.Providers.Wrappers;

namespace Audit.Core
{
    public class AuditScopeOptionsConfigurator : IAuditScopeOptionsConfigurator
    {
        internal readonly AuditScopeOptions _options = new AuditScopeOptions();

        public IAuditScopeOptionsConfigurator AuditEvent(AuditEvent auditEvent)
        {
            _options.AuditEvent = auditEvent;
            return this;
        }

        public IAuditScopeOptionsConfigurator CallingMethod(MethodBase method)
        {
            _options.CallingMethod = method;
            return this;
        }

        public IAuditScopeOptionsConfigurator CreationPolicy(EventCreationPolicy creationPolicy)
        {
            _options.CreationPolicy = creationPolicy;
            return this;
        }

        public IAuditScopeOptionsConfigurator DataProvider(AuditDataProvider dataProvider)
        {
            _options.DataProvider = dataProvider;
            return this;
        }

        public IAuditScopeOptionsConfigurator DataProviderLazyFactory(Func<AuditDataProvider> dataProviderFactory)
        {
            _options.DataProviderFactory = dataProviderFactory;
            return this;
        }

        public IAuditScopeOptionsConfigurator DataProviderDeferredFactory(Func<AuditEvent, AuditDataProvider> dataProviderFactory)
        {
            _options.DataProvider = new DeferredDataProvider(dataProviderFactory);
            return this;
        }

        public IAuditScopeOptionsConfigurator EventType(string eventType)
        {
            _options.EventType = eventType;
            return this;
        }

        public IAuditScopeOptionsConfigurator ExtraFields(object extraFields)
        {
            _options.ExtraFields = extraFields;
            return this;
        }

        public IAuditScopeOptionsConfigurator IsCreateAndSave(bool isCreateAndSave = true)
        {
            _options.IsCreateAndSave = isCreateAndSave;
            return this;
        }

        public IAuditScopeOptionsConfigurator SkipExtraFrames(int extraFrames)
        {
            _options.SkipExtraFrames = extraFrames;
            return this;
        }

        public IAuditScopeOptionsConfigurator Target(Func<object> targetGetter)
        {
            _options.TargetGetter = targetGetter;
            return this;
        }

        public IAuditScopeOptionsConfigurator IncludeStackTrace(bool includeStackTrace = true)
        {
            _options.IncludeStackTrace = includeStackTrace;
            return this;
        }

        public IAuditScopeOptionsConfigurator ExcludeEnvironmentInfo(bool excludeEnvironmentInfo = true)
        {
            _options.ExcludeEnvironmentInfo = excludeEnvironmentInfo;
            return this;
        }

        public IAuditScopeOptionsConfigurator SystemClock(ISystemClock systemClock)
        {
            _options.SystemClock = systemClock;
            return this;
        }

        public IAuditScopeOptionsConfigurator WithItem(string key, object value)
        {
            if (_options.Items == null)
            {
                _options.Items = new Dictionary<string, object>();
            }
            _options.Items[key] = value;
            return this;
        }
    }

}
