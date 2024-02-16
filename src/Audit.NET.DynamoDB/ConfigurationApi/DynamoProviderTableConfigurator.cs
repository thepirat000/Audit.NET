using System;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderTableConfigurator : IDynamoProviderTableConfigurator
    {
        internal Setting<string> _tableName;
        internal DynamoProviderAttributeConfigurator _attrConfigurator = new DynamoProviderAttributeConfigurator();

        public IDynamoProviderAttributeConfigurator Table(string tableName)
        {
            _tableName = tableName;
            return _attrConfigurator;
        }

        public IDynamoProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableName = tableNameBuilder;
            return _attrConfigurator;
        }
    }
}
