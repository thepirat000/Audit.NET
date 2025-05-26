using System;
using Amazon.DynamoDBv2.DocumentModel;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderTableConfigurator : IDynamoProviderTableConfigurator
    {
        internal Setting<string> _tableName;
        internal Action<TableBuilder> _tableBuilderAction;

        internal DynamoProviderAttributeConfigurator _attrConfigurator = new DynamoProviderAttributeConfigurator();

        public IDynamoProviderAttributeConfigurator Table(string tableName, Action<TableBuilder> tableBuilderAction)
        {
            _tableName = tableName;
            _tableBuilderAction = tableBuilderAction;
            return _attrConfigurator;
        }

        public IDynamoProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder, Action<TableBuilder> tableBuilderAction)
        {
            _tableName = tableNameBuilder;
            _tableBuilderAction = tableBuilderAction;
            return _attrConfigurator;
        }
    }
}
