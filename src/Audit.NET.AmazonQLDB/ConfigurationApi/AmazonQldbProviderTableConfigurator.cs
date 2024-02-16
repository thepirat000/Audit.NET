using System;
using Audit.Core;

namespace Audit.AmazonQLDB.ConfigurationApi
{
    public class AmazonQldbProviderTableConfigurator : IAmazonQldbProviderTableConfigurator
    {
        internal Setting<string> _tableName;
        internal AmazonQldbProviderAttributeConfigurator _attrConfigurator = new AmazonQldbProviderAttributeConfigurator();

        public IAmazonQldbProviderAttributeConfigurator Table(string tableName)
        {
            _tableName = tableName;
            return _attrConfigurator;
        }

        public IAmazonQldbProviderAttributeConfigurator Table(Func<AuditEvent, string> tableNameBuilder)
        {
            _tableName = tableNameBuilder;
            return _attrConfigurator;
        }
    }
}
