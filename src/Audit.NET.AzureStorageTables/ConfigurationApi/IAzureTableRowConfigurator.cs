using Audit.Core;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public interface IAzureTableRowConfigurator
    {
        /// <summary>
        /// Sets the partition key to use as a function of the audit event. Default partition key is "event"
        /// </summary>
        /// <param name="partitionKeybuilder">A function that returns the partition key from an audit event</param>
        IAzureTableRowConfigurator PartitionKey(Func<AuditEvent, string> partitionKeybuilder);
        /// <summary>
        /// Sets the partition key to use for all the audit events. Default partition key is "event"
        /// </summary>
        /// <param name="partitionKey">The partition key to use</param>
        IAzureTableRowConfigurator PartitionKey(string partitionKey);
        /// <summary>
        /// Sets the row key to use as a function of the audit event. Default is a random Guid.
        /// </summary>
        /// <param name="rowKeybuilder">A function that returns the row key from an audit event</param>
        IAzureTableRowConfigurator RowKey(Func<AuditEvent, string> rowKeybuilder);
        /// <summary>
        /// Defines a configuration for the extra columns (properties) on the entity. Default is one column "AuditEvent" with the audit event JSON.
        /// </summary>
        /// <param name="columnsConfigurator">A fluent configuration for the columns</param>
        IAzureTableRowConfigurator Columns(Action<IAzureTableColumnsConfigurator> columnsConfigurator);
    }
}
