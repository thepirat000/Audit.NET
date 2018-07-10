using System;
using Audit.Core.ConfigurationApi;
using Audit.AzureTableStorage.Providers;
using Audit.AzureTableStorage.ConfigurationApi;
using Microsoft.WindowsAzure.Storage.Table;

namespace Audit.Core
{
    public static class AzureTableConfiguratorExtensions
    {
        /// <summary>
        /// Store the events in an Azure Table Storage.
        /// </summary>
        /// <param name="configurator">The configurator.</param>
        /// <param name="config">The Azure Table provider configuration.</param>
        public static ICreationPolicyConfigurator UseAzureTableStorage(this IConfigurator configurator, Action<IAzureTableProviderConfigurator> config)
        {
            var tableConfig = new AzureTableProviderConfigurator();
            config.Invoke(tableConfig);
            return UseAzureTableStorage(configurator, tableConfig._connectionStringBuilder, tableConfig._tableNameBuilder, tableConfig._tableEntityBuilder);
        }


        /// <summary>
        /// Store the events in an Azure Table Storage.
        /// </summary>
        /// <param name="configurator">The configurator.</param>
        /// <param name="connectionStringBuilder">A function that returns a connection string for an event.</param>
        /// <param name="tableNameBuilder">A function that returns the table name to use for an event.</param>
        /// <param name="tableEntityBuilder">A function that returns the entity to store from an event.</param>
        private static ICreationPolicyConfigurator UseAzureTableStorage(this IConfigurator configurator, Func<AuditEvent, string> connectionStringBuilder = null,
            Func<AuditEvent, string> tableNameBuilder = null, Func<AuditEvent, ITableEntity> tableEntityBuilder = null)
        {
            Configuration.DataProvider = new AzureTableDataProvider()
            {
                ConnectionStringBuilder = connectionStringBuilder,
                TableEntityMapper = tableEntityBuilder,
                TableNameBuilder = tableNameBuilder
            };
            return new CreationPolicyConfigurator();
        }

    }
}
