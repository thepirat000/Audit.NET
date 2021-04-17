using System;
using Amazon.QLDB.Driver;
using Audit.Core.ConfigurationApi;
using Audit.NET.AmazonQLDB.ConfigurationApi;
using Audit.NET.AmazonQLDB.Providers;

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
                TableNameBuilder = tableNameBuilder
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
            Configuration.DataProvider = new AmazonQldbDataProvider
            {
                QldbDriver = amazonQldbProviderConfigurator._driverFactory,
                TableNameBuilder = amazonQldbProviderConfigurator._tableConfigurator?._tableNameBuilder,
                CustomAttributes = amazonQldbProviderConfigurator._tableConfigurator?._attrConfigurator?._attributes
            };
            return new CreationPolicyConfigurator();
        }
    }
}
