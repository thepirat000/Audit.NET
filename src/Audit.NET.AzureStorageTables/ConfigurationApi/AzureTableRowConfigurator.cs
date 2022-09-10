using Audit.Core;
using System;
using System.Collections.Generic;

namespace Audit.AzureStorageTables.ConfigurationApi
{
    public class AzureTableRowConfigurator : IAzureTableRowConfigurator
    {
        internal Func<AuditEvent, string> _partKeyBuilder = null;
        internal Func<AuditEvent, string> _rowKeyBuilder = null;
        internal Func<AuditEvent, IDictionary<string, object>> _propsBuilder = null;

        public IAzureTableRowConfigurator Columns(Action<IAzureTableColumnsConfigurator> columnsConfigurator)
        {
            var cols = new AzureTableColumnsConfigurator();
            columnsConfigurator.Invoke(cols);
            _propsBuilder = cols._propsBuilder;
            return this;
        }

        public IAzureTableRowConfigurator PartitionKey(Func<AuditEvent, string> partitionKeybuilder)
        {
            _partKeyBuilder = partitionKeybuilder;
            return this;
        }

        public IAzureTableRowConfigurator PartitionKey(string partitionKey)
        {
            _partKeyBuilder = _ => partitionKey;
            return this;
        }
        public IAzureTableRowConfigurator RowKey(Func<AuditEvent, string> rowKeybuilder)
        {
            _rowKeyBuilder = rowKeybuilder;
            return this;
        }
    }
}
