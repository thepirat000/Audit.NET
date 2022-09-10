using Audit.Core;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public interface IAzureTableConnectionConfigurator
    {
        /// <summary>
        /// Specifies the Azure Table Storage connection string
        /// </summary>
        /// <param name="connectionString">The Azure Table Storage connection string.</param>
        IAzureTablesEntityConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies the Azure Table Storage endpoint to use
        /// </summary>
        /// <param name="endpoint">The endpoint URI</param>
        IAzureTablesEntityConfigurator Endpoint(Uri endpoint);
        /// <summary>
        /// Specifies the Azure Table Storage endpoint and the credentials to use to connect to it.
        /// </summary>
        /// <param name="endpoint">The endpoint URI</param>
        /// <param name="credential">The Azure SAS credential</param>
        IAzureTablesEntityConfigurator Endpoint(Uri endpoint, AzureSasCredential credential);
        /// <summary>
        /// Specifies the Azure Table Storage endpoint and the credentials to use to connect to it.
        /// </summary>
        /// <param name="endpoint">The endpoint URI</param>
        /// <param name="credential">The Table Shared Key credential</param>
        IAzureTablesEntityConfigurator Endpoint(Uri endpoint, TableSharedKeyCredential credential);
        /// <summary>
        /// Specifies the Azure Table Storage endpoint and the credentials to use to connect to it.
        /// </summary>
        /// <param name="endpoint">The endpoint URI</param>
        /// <param name="credential">The token credential</param>
        IAzureTablesEntityConfigurator Endpoint(Uri endpoint, TokenCredential credential);
        /// <summary>
        /// Specifies a table client factory that returns the TableClient to use for a given Audit Event.
        /// </summary>
        /// <param name="clientFactory">The table client factory</param>
        IAzureTablesEntityConfigurator TableClientFactory(Func<AuditEvent, TableClient> clientFactory);
    }
}
