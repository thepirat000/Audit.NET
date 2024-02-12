using System;
using Audit.Core;

namespace Audit.AmazonQLDB.ConfigurationApi
{
    public class AmazonQldbProviderTableConfigurator : IAmazonQldbProviderTableConfigurator
    {
        internal Func<AuditEvent, string> _tableNameBuilder;
        internal AmazonQldbProviderAttributeConfigurator _attrConfigurator = new AmazonQldbProviderAttributeConfigurator();

        public IAmazonQldbProviderAttributeConfigurator Table(string tableName)
        {
            _tableNameBuilder = _ => tableName;
            return _attrConfigurator;
        }

        public IAmazonQldbProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableNameBuilder = tableNameBuilder;
            return _attrConfigurator;
        }
    }
}
