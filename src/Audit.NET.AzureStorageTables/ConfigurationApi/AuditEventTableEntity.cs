using Audit.Core;
using Azure;
using Azure.Data.Tables;
using System;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    /// <summary>
    /// Default Table Entity to store an Audit Event as a single JSON column.
    /// </summary>
    public class AuditEventTableEntity : ITableEntity
    {
        /// <summary>
        /// The JSON representation of the Audit Event
        /// </summary>
        public string AuditEvent { get; set; }
        /// <inheritdoc/>
        public string PartitionKey { get; set; }
        /// <inheritdoc/>
        public string RowKey { get; set; }
        /// <inheritdoc/>
        public DateTimeOffset? Timestamp { get; set; }
        /// <inheritdoc/>
        public ETag ETag { get; set; }

        /// <summary>
        /// Creates a new instance of AuditEventTableEntity.
        /// </summary>
        public AuditEventTableEntity() : base()
        {
        }
        
        /// <summary>
        /// Creates a new entity from an audit event, using the Audit Event type as the partition key, and a random Guid as the row key.
        /// </summary>
        public AuditEventTableEntity(AuditEvent auditEvent)
        {
            PartitionKey = auditEvent.GetType().Name;
            RowKey = Guid.NewGuid().ToString();
            AuditEvent = Configuration.JsonAdapter.Serialize(auditEvent);
        }
        
        /// <summary>
        /// Creates a new entity from a partition key and a row key.
        /// </summary>
        public AuditEventTableEntity(string partitionKey, string rowKey) 
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        /// <summary>
        /// Creates a new entity from an audit event, using the provided partition key and row key.
        /// </summary>
        public AuditEventTableEntity(string partitionKey, string rowKey, AuditEvent auditEvent)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            AuditEvent = Configuration.JsonAdapter.Serialize(auditEvent);
        }
    }
}
