using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public interface IAzureActiveDirectoryConfigurator
    {
        /// <summary>
        /// Specifies the Tenant ID (Directory ID) to use.
        /// </summary>
        /// <param name="tenantId">The Tenant ID (Directory ID) to use.</param>
        IAzureActiveDirectoryConfigurator TenantId(string tenantId);
        /// <summary>
        /// Specifies a function that returns the Tenant ID (Directory ID) to use.
        /// </summary>
        /// <param name="tenantIdBuilder">A function that returns the Tenant ID (Directory ID) to use.</param>
        IAzureActiveDirectoryConfigurator TenantId(Func<AuditEvent, string> tenantIdBuilder);
        /// <summary>
        /// Specifies a custom connection string for the Token Provider. Please check https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#connection-string-support for more information.
        /// </summary>
        /// <param name="authConnectionString">A custom auth connection string for the Token Provider</param>
        IAzureActiveDirectoryConfigurator AuthConnectionString(string authConnectionString);
        /// <summary>
        /// Specifies a custom connection string for the Token Provider. Please check https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#connection-string-support for more information.
        /// </summary>
        /// <param name="authConnectionStringBuilder">A function that return the custom auth connection string for the Token Provider</param>
        IAzureActiveDirectoryConfigurator AuthConnectionString(Func<AuditEvent, string> authConnectionStringBuilder);
        /// <summary>
        /// Specifies a custom resource URL to acquire token for. Default is the Azure Storage resource ID "https://storage.azure.com/".
        /// </summary>
        /// <param name="resourceUrl">A custom resource URL to acquire token for.</param>
        IAzureActiveDirectoryConfigurator ResourceUrl(string resourceUrl);
        /// <summary>
        /// Specifies the storage account name to use
        /// </summary>
        /// <param name="accountName">The storage account name to use</param>
        IAzureActiveDirectoryConfigurator AccountName(string accountName);
        /// <summary>
        /// Specifies the storage account name to use as a function of the audit event
        /// </summary>
        /// <param name="accountNameBuilder">A function of the audit event that returns the storage account name to use</param>
        IAzureActiveDirectoryConfigurator AccountName(Func<AuditEvent, string> accountNameBuilder);
        /// <summary>
        /// Specifies a custom DNS endpoint suffix to use for all the storage services. Default is "core.windows.net"
        /// </summary>
        /// <param name="endpointSuffix">The custom DNS endpoint suffix to use.</param>
        IAzureActiveDirectoryConfigurator EndpointSuffix(string endpointSuffix);
        /// <summary>
        /// Specifies whether to use HTTPS to connect to storage service endpoints. Default is true.
        /// </summary>
        /// <param name="useHttps">Value indicating whether to use HTTPS to connect to storage service endpoints</param>
        IAzureActiveDirectoryConfigurator UseHttps(bool useHttps);
    }
}