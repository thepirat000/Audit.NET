using System;
using Audit.Core;

namespace Audit.AzureTableStorage.ConfigurationApi
{
    public class AzureBlobProviderConfigurator : IAzureBlobProviderConfigurator
    {
        internal AzureBlobProviderEventConfigurator _eventConfig;

        public IAzureBlobProviderEventConfigurator ConnectionString(string connectionString)
        {
            _eventConfig = new AzureBlobProviderEventConfigurator
            {
                _useActiveDirectory = false,
                _connectionStringBuilder = ev => connectionString
            };
            return _eventConfig;
        }

        public IAzureBlobProviderEventConfigurator ConnectionString(Func<AuditEvent, string> connectionStringBuilder)
        {
            _eventConfig = new AzureBlobProviderEventConfigurator
            {
                _useActiveDirectory = false,
                _connectionStringBuilder = connectionStringBuilder
            };
            return _eventConfig;
        }

        public IAzureBlobProviderEventConfigurator AzureActiveDirectory(Action<IAzureActiveDirectoryConfigurator> configuration)
        {
            var adConfig = new AzureActiveDirectoryConfigurator();
            configuration.Invoke(adConfig);
            _eventConfig = new AzureBlobProviderEventConfigurator
            {
                _useActiveDirectory = true,
                _activeDirectoryConfiguration = adConfig
            };
            return _eventConfig;
        }
    }
}