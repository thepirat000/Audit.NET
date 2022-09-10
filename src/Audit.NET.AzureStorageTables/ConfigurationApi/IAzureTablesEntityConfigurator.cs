using Audit.Core;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public interface IAzureTablesEntityConfigurator
    {
        /// <summary>
        /// Specifies the Table Client Options to use when connecting to the Azure Table Storage.
        /// </summary>
        /// <param name="options">The options to use</param>
        /// <returns></returns>
        IAzureTablesEntityConfigurator ClientOptions(TableClientOptions options);
        /// <summary>
        /// Specifies how to dynamically create a Table Entity from the audit event. By default an AuditEventTableEntity is used.
        /// Use this method as an alternative to EntityMapper to build the columns dynamically.
        /// </summary>
        /// <param name="entityConfigurator">Table entity configurator</param>
        /// <returns></returns>
        IAzureTablesEntityConfigurator EntityBuilder(Action<IAzureTableRowConfigurator> entityConfigurator);
        /// <summary>
        /// Specifies how to map the AuditEvent to an Azure TableEntity object. By default an AuditEventTableEntity is used.
        /// </summary>
        /// <param name="tableEntityMapper">Table entity mapper, a function that takes an AuditEvent and returns an implementation of ITableEntity</param>
        /// <returns></returns>
        IAzureTablesEntityConfigurator EntityMapper(Func<AuditEvent, ITableEntity> tableEntityMapper);
        /// <summary>
        /// Specifies a function that returns the table name to use for an event
        /// </summary>
        /// <param name="tableNameBuilder">A function that returns the table name to use for an event</param>
        IAzureTablesEntityConfigurator TableName(Func<AuditEvent, string> tableNameBuilder);
        /// <summary>
        /// Specifies the table name
        /// </summary>
        /// <param name="tableName">The table name to use.</param>
        IAzureTablesEntityConfigurator TableName(string tableName);
    }
}
