using Amazon.QLDB.Driver;
using Amazon.QLDBSession;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using System;

namespace Audit.NET.AmazonQLDB.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the AmazonQLDB data provider
    /// </summary>
    public interface IAmazonQldbProviderConfigurator
    {
        /// <summary>
        /// Use a driver with the given ledger name
        /// </summary>
        /// <param name="ledger">The name of the ledger to be used</param>
        IAmazonQldbProviderConfigurator UseLedger(string ledger);

        /// <summary>
        /// Use a driver with the given AWS credentials
        /// </summary>
        /// <param name="credentials">The credentials to be used</param>
        IAmazonQldbProviderConfigurator UseAwsCredentials(AWSCredentials credentials);

        /// <summary>
        /// Use a driver with the given logger
        /// </summary>
        /// <param name="logger">The logger to be used</param>
        IAmazonQldbProviderConfigurator UseLogger(ILogger logger);

        /// <summary>
        /// Use a driver with the given session config
        /// </summary>
        /// <param name="sessionConfig">The session config to be used</param>
        IAmazonQldbProviderConfigurator UseQldbSessionConfig(AmazonQLDBSessionConfig sessionConfig);

        /// <summary>
        /// Use a driver with the given maximum concurrent transactions
        /// </summary>
        /// <param name="maxConcurrentTransactions">The number of the maximum concurrent transactions</param>
        IAmazonQldbProviderConfigurator UseMaxConcurrentTransactions(int maxConcurrentTransactions);

        /// <summary>
        /// Use a driver with logging on retry 
        /// </summary>
        IAmazonQldbProviderConfigurator UseRetryLogging();

        /// <summary>
        /// Continues the configuration builder with the table configurator
        /// </summary>
        IAmazonQldbProviderTableConfigurator And { get; }

        /// <summary>
        /// Use the given driver
        /// </summary>
        /// <param name="driver">A Amazon QLDB driver instance</param>
        IAmazonQldbProviderTableConfigurator WithQldbDriver(IAsyncQldbDriver driver);

        /// <summary>
        /// Use the given driver
        /// </summary>
        /// <param name="driver">A Amazon QLDB driver instance</param>
        IAmazonQldbProviderTableConfigurator WithQldbDriver(AsyncQldbDriver driver);

        /// <summary>
        /// Use the given driver builder
        /// </summary>
        /// <param name="driverBuilder">A function that returns the Amazon QLDB driver instance tu use</param>
        IAmazonQldbProviderTableConfigurator WithQldbDriver(Func<IAsyncQldbDriver> driverBuilder);
    }
}
