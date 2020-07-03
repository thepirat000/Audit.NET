using System;
using Amazon.QLDB.Driver;
using Amazon.QLDBSession;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;

namespace Audit.NET.AmazonQLDB.ConfigurationApi
{
    public class AmazonQldbProviderConfigurator : IAmazonQldbProviderConfigurator
    {
        internal AmazonQldbProviderTableConfigurator _tableConfigurator = new AmazonQldbProviderTableConfigurator();
        internal Lazy<IQldbDriver> _driverFactory;
        private string _ledger;
        private AWSCredentials _credentials;
        private bool _useRetryLogging;
        private ILogger _logger;
        private AmazonQLDBSessionConfig _sessionConfig;
        private int _maxConcurrentTransactions;

        public IAmazonQldbProviderTableConfigurator And => _tableConfigurator;

        public IAmazonQldbProviderConfigurator UseLedger(string ledger)
        {
            _ledger = ledger;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderConfigurator UseAwsCredentials(AWSCredentials credentials)
        {
            _credentials = credentials;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderConfigurator UseLogger(ILogger logger)
        {
            _logger = logger;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderConfigurator UseQldbSessionConfig(AmazonQLDBSessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderConfigurator UseMaxConcurrentTransactions(int maxConcurrentTransactions)
        {
            _maxConcurrentTransactions = maxConcurrentTransactions;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderConfigurator UseRetryLogging()
        {
            _useRetryLogging = true;
            CreateDriverFactory();
            return this;
        }

        public IAmazonQldbProviderTableConfigurator WithQldbDriver(IQldbDriver driver)
        {
            _driverFactory = new Lazy<IQldbDriver>(() => driver);
            return _tableConfigurator;
        }

        public IAmazonQldbProviderTableConfigurator WithQldbDriver(QldbDriver driver)
        {
            _driverFactory = new Lazy<IQldbDriver>(() => driver);
            return _tableConfigurator;
        }

        public IAmazonQldbProviderTableConfigurator WithQldbDriver(Func<IQldbDriver> driverBuilder)
        {
            _driverFactory = new Lazy<IQldbDriver>(driverBuilder);
            return _tableConfigurator;
        }

        private void CreateDriverFactory()
        {
            _driverFactory = new Lazy<IQldbDriver>(() =>
            {
                var builder = QldbDriver.Builder()
                    .WithAWSCredentials(_credentials)
                    .WithQLDBSessionConfig(_sessionConfig);

                if (!string.IsNullOrEmpty(_ledger))
                {
                    builder.WithLedger(_ledger);
                }

                if (_maxConcurrentTransactions >= 0)
                {
                    builder.WithMaxConcurrentTransactions(_maxConcurrentTransactions);
                }

                if (_useRetryLogging)
                {
                    builder.WithRetryLogging();
                }

                if (_logger != null)
                {
                    builder.WithLogger(_logger);
                }

                return builder
                    .Build();
            });
        }
    }
}
