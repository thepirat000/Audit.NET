using Audit.Core;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    /// <summary>
    /// Azure Table Provider Configurator
    /// </summary>
    public interface IAzureTableProviderConfigurator
    {
        /// <summary>
        /// Specifies the Azure Storage connection string
        /// </summary>
        /// <param name="connectionString">The Azure Storage connection string.</param>
        IAzureTableProviderConfigurator ConnectionString(string connectionString);
        /// <summary>
        /// Specifies a function that returns the connection string for an event
        /// </summary>
        /// <param name="connectionStringBuilder">A function that returns the connection string for an event.</param>
        IAzureTableProviderConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder);
        /// <summary>
        /// Specifies how to map the AuditEvent to an Azure TableEntity object. By default an AuditEventTableEntity is used.
        /// </summary>
        /// <param name="tableEntityMapper">Table entity mapper, a function that takes an AuditEvent and returns an implementation of ITableEntity</param>
        /// <returns></returns>
        IAzureTableProviderConfigurator EntityMapper(Func<AuditEvent, ITableEntity> tableEntityMapper);
        /// <summary>
        /// Specifies how to dynamically create a Table Entity from the audit event. By default an AuditEventTableEntity is used.
        /// Use this method as an alternative to EntityMapper to build the columns dynamically.
        /// </summary>
        /// <param name="entityConfigurator">Table entity configurator</param>
        /// <returns></returns>
        IAzureTableProviderConfigurator EntityBuilder(Action<IAzureTableEntityConfigurator> entityConfigurator);
        /// <summary>
        /// Specifies the table name
        /// </summary>
        /// <param name="tableName">The table name to use.</param>
        IAzureTableProviderConfigurator TableName(string tableName);
        /// <summary>
        /// Specifies a function that returns the table name to use for an event
        /// </summary>
        /// <param name="tableNameBuilder">A function that returns the table name to use for an event</param>
        IAzureTableProviderConfigurator TableName(Func<AuditEvent, string> tableNameBuilder);
    }
}