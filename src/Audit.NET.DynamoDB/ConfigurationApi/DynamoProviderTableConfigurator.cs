using System;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderTableConfigurator : IDynamoProviderTableConfigurator
    {
        internal Func<AuditEvent, string> _tableNameBuilder;
        internal DynamoProviderAttributeConfigurator _attrConfigurator = new DynamoProviderAttributeConfigurator();

        public IDynamoProviderAttributeConfigurator Table(string tableName)
        {
            _tableNameBuilder = _ => tableName;
            return _attrConfigurator;
        }

        public IDynamoProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return _attrConfigurator;
        }
    }
}
