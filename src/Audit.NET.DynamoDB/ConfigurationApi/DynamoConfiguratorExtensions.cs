using System;
using Audit.Core.ConfigurationApi;
using Audit.DynamoDB.Providers;
using Amazon.DynamoDBv2;
using Audit.DynamoDB.Configuration;

namespace Audit.Core
{
    public static class DynamoConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in Amazon DynamoDB tables.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object</param>
        /// <param name="clientFactory">The client factory to use</param>
        /// <param name="tableNameBuilder">The table name builder to use</param>
        public static ICreationPolicyConfigurator UseDynamoDB(this IConfigurator configurator, Lazy<IAmazonDynamoDB> clientFactory, Func<AuditEvent, string> tableNameBuilder)
        {
            Configuration.DataProvider = new DynamoDataProvider()
            {
                Client = clientFactory,
                TableName = tableNameBuilder
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Store the events in Amazon DynamoDB tables.
        /// </summary>
        /// <param name="configurator">The Audit.NET configurator object</param>
        /// <param name="config">DynamoDB fluent config</param>
        public static ICreationPolicyConfigurator UseDynamoDB(this IConfigurator configurator, Action<IDynamoProviderConfigurator> config)
        {
            Configuration.DataProvider = new DynamoDataProvider(config);
            return new CreationPolicyConfigurator();
        }
    }
}
