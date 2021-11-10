using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureActiveDirectoryConfigurator : IAzureActiveDirectoryConfigurator
    {
        internal Func<AuditEvent, string> _authConnectionStringBuilder = null;
        internal Func<AuditEvent, string> _tenantIdBuilder = null;
        internal string _resourceUrl = "https://storage.azure.com/";

        internal Func<AuditEvent, string> _accountNameBuilder;
        internal string _endpointSuffix = "core.windows.net";
        internal bool _useHttps = true;

        public IAzureActiveDirectoryConfigurator AccountName(string accountName)
        {
            _accountNameBuilder = _ => accountName;
            return this;
        }
        public IAzureActiveDirectoryConfigurator AccountName(Func<AuditEvent, string> accountNameBuilder)
        {
            _accountNameBuilder = accountNameBuilder;
            return this;
        }

        public IAzureActiveDirectoryConfigurator EndpointSuffix(string endpointSuffix)
        {
            _endpointSuffix = endpointSuffix;
            return this;
        }

        public IAzureActiveDirectoryConfigurator UseHttps(bool useHttps)
        {
            _useHttps = useHttps;
            return this;
        }

        public IAzureActiveDirectoryConfigurator TenantId(string tenantId)
        {
            _tenantIdBuilder = _ => tenantId;
            return this;
        }
        public IAzureActiveDirectoryConfigurator TenantId(Func<AuditEvent, string> tenantIdBuilder)
        {
            _tenantIdBuilder = tenantIdBuilder;
            return this;
        }

        public IAzureActiveDirectoryConfigurator AuthConnectionString(string authConnectionString)
        {
            _authConnectionStringBuilder = _ => authConnectionString;
            return this;
        }
        public IAzureActiveDirectoryConfigurator AuthConnectionString(Func<AuditEvent, string> authConnectionStringBuilder)
        {
            _authConnectionStringBuilder = authConnectionStringBuilder;
            return this;
        }

        public IAzureActiveDirectoryConfigurator ResourceUrl(string resourceUrl)
        {
            _resourceUrl = resourceUrl;
            return this;
        }

    }
}