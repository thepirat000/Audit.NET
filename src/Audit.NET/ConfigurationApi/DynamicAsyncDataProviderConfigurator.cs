using Audit.Core.Providers;
using System;
using System.Threading.Tasks;

namespace Audit.Core.ConfigurationApi
{
    public class DynamicAsyncDataProviderConfigurator : IDynamicAsyncDataProviderConfigurator
    {
#pragma warning disable CS3008 // Identifier is not CLS-compliant
        public DynamicAsyncDataProvider _dynamicAsyncDataProvider;
#pragma warning restore CS3008 // Identifier is not CLS-compliant

        public DynamicAsyncDataProviderConfigurator(DynamicAsyncDataProvider dynamicAsyncDataProvider)
        {
            _dynamicAsyncDataProvider = dynamicAsyncDataProvider;
        }

        public IDynamicAsyncDataProviderConfigurator OnInsert(Func<AuditEvent, Task> insertAction)
        {
            _dynamicAsyncDataProvider.AttachOnInsert(insertAction);
            return this;
        }

        public IDynamicAsyncDataProviderConfigurator OnInsert(Func<AuditEvent, Task<object>> insertFunction)
        {
            _dynamicAsyncDataProvider.AttachOnInsert(insertFunction);
            return this;
        }

        public IDynamicAsyncDataProviderConfigurator OnInsertAndReplace(Func<object, AuditEvent, Task> insertReplaceAction)
        {
            _dynamicAsyncDataProvider.AttachOnInsertAndReplace(insertReplaceAction);
            return this;
        }

        public IDynamicAsyncDataProviderConfigurator OnInsertAndReplace(Func<AuditEvent, Task> insertReplaceAction)
        {
            _dynamicAsyncDataProvider.AttachOnInsertAndReplace(insertReplaceAction);
            return this;
        }

        public IDynamicAsyncDataProviderConfigurator OnReplace(Func<object, AuditEvent, Task> replaceAction)
        {
            _dynamicAsyncDataProvider.AttachOnReplace(replaceAction);
            return this;
        }
    }
}