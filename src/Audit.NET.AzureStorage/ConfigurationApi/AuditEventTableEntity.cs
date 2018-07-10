using System;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    /// <summary>
    /// Default Table Entity to store an Audit Event as a single JSON column.
    /// </summary>
    public class AuditEventTableEntity : TableEntity
    {
        /// <summary>
        /// The JSON representation of the Audit Event
        /// </summary>
        public string AuditEvent { get; set; }
        /// <summary>
        /// Creates a new instance of AuditEventTableEntity.
        /// </summary>
        public AuditEventTableEntity() : base()
        {
        }
        /// <summary>
        /// Creates a new entity from an audit event, using the Audit Event type as the partition key, and a random Guid as the row key.
        /// </summary>
        public AuditEventTableEntity(AuditEvent auditEvent) : base(auditEvent.GetType().Name, Guid.NewGuid().ToString())
        {
            AuditEvent = JsonConvert.SerializeObject(auditEvent, Configuration.JsonSettings);
        }
        /// <summary>
        /// Creates a new entity from a partition key and a row key.
        /// </summary>
        public AuditEventTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey)
        {
        }
        /// <summary>
        /// Creates a new entity from an audit event, using the provided partition key and row key.
        /// </summary>
        public AuditEventTableEntity(string partitionKey, string rowKey, AuditEvent auditEvent) : base(partitionKey, rowKey)
        {
            AuditEvent = JsonConvert.SerializeObject(auditEvent, Configuration.JsonSettings);
        }
    }
}