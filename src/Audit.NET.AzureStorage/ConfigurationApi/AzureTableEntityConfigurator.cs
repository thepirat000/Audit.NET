using System;
using System.Collections.Generic;
using Audit.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureTableEntityConfigurator : IAzureTableEntityConfigurator
    {
        internal Func<AuditEvent, string> _partKeyBuilder = null;
        internal Func<AuditEvent, string> _rowKeyBuilder = null;

        internal Func<AuditEvent, IDictionary<string, EntityProperty>> _propsBuilder = null;

        public IAzureTableEntityConfigurator Columns(Action<IAzureTableColumnsConfigurator> columnsConfigurator)
        {
            var cols = new AzureTableColumnsConfigurator();
            columnsConfigurator.Invoke(cols);
            _propsBuilder = cols._propsBuilder;
            return this;
        }

        public IAzureTableEntityConfigurator PartitionKey(Func<AuditEvent, string> partitionKeybuilder)
        {
            _partKeyBuilder = partitionKeybuilder;
            return this;
        }

        public IAzureTableEntityConfigurator PartitionKey(string partitionKey)
        {
            _partKeyBuilder = _ => partitionKey;
            return this;
        }
        public IAzureTableEntityConfigurator RowKey(Func<AuditEvent, string> rowKeybuilder)
        {
            _rowKeyBuilder = rowKeybuilder;
            return this;
        }
    }
}