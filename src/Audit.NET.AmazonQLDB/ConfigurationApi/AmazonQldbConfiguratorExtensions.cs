using System;
using Amazon.QLDB.Driver;
using Audit.Core.ConfigurationApi;
using Audit.AmazonQLDB.ConfigurationApi;
using Audit.AmazonQLDB.Providers;

namespace Audit.Core
{
    public static class AmazonQldbConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in Amazon QLDB tables.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object</param>
        /// <param name="driverFactory">The QLDB driver factory to use</param>
        /// <param name="tableNameBuilder">The table name builder to use</param>
        public static ICreationPolicyConfigurator UseAmazonQldb(this IConfigurator configurator, 
            Lazy<IAsyncQldbDriver> driverFactory, 
            Func<AuditEvent, string> tableNameBuilder)
        {
            Configuration.DataProvider = new AmazonQldbDataProvider
            {
                QldbDriver = driverFactory,
                TableName = tableNameBuilder
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in Amazon AmazonQLDB tables.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object</param>
        /// <param name="config">AmazonQLDB fluent config</param>
        public static ICreationPolicyConfigurator UseAmazonQldb(this IConfigurator configurator, Action<IAmazonQldbProviderConfigurator> config)
        {
            var amazonQldbProviderConfigurator = new AmazonQldbProviderConfigurator();
            config.Invoke(amazonQldbProviderConfigurator);
            var provider = new AmazonQldbDataProvider
            {
                QldbDriver = amazonQldbProviderConfigurator._driverFactory,
                TableName = amazonQldbProviderConfigurator._tableConfigurator._tableName,
                CustomAttributes = amazonQldbProviderConfigurator._tableConfigurator._attrConfigurator?._attributes
            };
            if (amazonQldbProviderConfigurator._jsonSettings != null)
            {
                provider.JsonSettings = amazonQldbProviderConfigurator._jsonSettings;
            }
            Configuration.DataProvider = provider;
            return new CreationPolicyConfigurator();
        }
    }
}
