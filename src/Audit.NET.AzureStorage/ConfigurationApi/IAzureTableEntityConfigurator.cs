using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    /// <summary>
    /// Defines a fluent configuration for the audit entities to be stored on Azure Tables
    /// </summary>
    public interface IAzureTableEntityConfigurator
    {
        /// <summary>
        /// Sets the partition key to use as a function of the audit event. Default partition key is "event"
        /// </summary>
        /// <param name="partitionKeybuilder">A function that returns the partition key from an audit event</param>
        IAzureTableEntityConfigurator PartitionKey(Func<AuditEvent, string> partitionKeybuilder);
        /// <summary>
        /// Sets the partition key to use for all the audit events. Default partition key is "event"
        /// </summary>
        /// <param name="partitionKey">The partition key to use</param>
        IAzureTableEntityConfigurator PartitionKey(string partitionKey);
        /// <summary>
        /// Sets the row key to use as a function of the audit event. Default is a random Guid.
        /// </summary>
        /// <param name="rowKeybuilder">A function that returns the row key from an audit event</param>
        IAzureTableEntityConfigurator RowKey(Func<AuditEvent, string> rowKeybuilder);
        /// <summary>
        /// Defines a configuration for the extra columns (properties) on the entity. Default is one column "AuditEvent" with the audit event JSON.
        /// </summary>
        /// <param name="columnsConfigurator">A fluent configuration for the columns</param>
        IAzureTableEntityConfigurator Columns(Action<IAzureTableColumnsConfigurator> columnsConfigurator);
    }
}