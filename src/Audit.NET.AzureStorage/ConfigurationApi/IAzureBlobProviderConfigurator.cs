using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    /// <summary>
    /// Azure Blob Provider Configurator
    /// </summary>
    public interface IAzureBlobProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Storage connection string
        /// </summary>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        IAzureBlobProviderEventConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies a function that returns the connection string for an event
        /// </summary>
        /// <param name="connectionStringBuilder">A function that returns the connection string for an event.</param>
        IAzureBlobProviderEventConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder);
        /// <summary>
        /// Uses Azure Active Directory authentication for managed identities. 
        /// </summary>
        /// <param name="configuration">The Azure AD configuration</param>
        /// <remarks>https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-app</remarks>
        IAzureBlobProviderEventConfigurator AzureActiveDirectory(Action<IAzureActiveDirectoryConfigurator> configuration);
    }
}