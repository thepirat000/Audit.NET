using System;
using System.Reflection;

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

        public IAuditScopeOptionsConfigurator DataProvider(Func<AuditDataProvider> dataProviderFactory)
        {
            _options.DataProviderFactory = dataProviderFactory;
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
    }

}
