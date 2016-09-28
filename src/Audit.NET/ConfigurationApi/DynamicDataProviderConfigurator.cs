using Audit.Core.Providers;
using System;

namespace Audit.Core.ConfigurationApi
{
    public class DynamicDataProviderConfigurator : IDynamicDataProviderConfigurator
    {
        public DynamicDataProvider _dynamicDataProvider;

        public DynamicDataProviderConfigurator(DynamicDataProvider dynamicDataProvider)
        {
            _dynamicDataProvider = dynamicDataProvider;
        }

        public IDynamicDataProviderConfigurator OnInsert(Action<AuditEvent> insertAction)
        {
            _dynamicDataProvider.AttachOnInsert(insertAction);
            return this;
        }

        public IDynamicDataProviderConfigurator OnInsert(Func<AuditEvent, object> insertFunction)
        {
            _dynamicDataProvider.AttachOnInsert(insertFunction);
            return this;
        }

        public IDynamicDataProviderConfigurator OnInsertAndReplace(Action<object, AuditEvent> insertReplaceAction)
        {
            _dynamicDataProvider.AttachOnInsertAndReplace(insertReplaceAction);
            return this;
        }

        public IDynamicDataProviderConfigurator OnInsertAndReplace(Action<AuditEvent> insertReplaceAction)
        {
            _dynamicDataProvider.AttachOnInsertAndReplace(insertReplaceAction);
            return this;
        }

        public IDynamicDataProviderConfigurator OnReplace(Action<object, AuditEvent> replaceAction)
        {
            _dynamicDataProvider.AttachOnReplace(replaceAction);
            return this;
        }
    }
}