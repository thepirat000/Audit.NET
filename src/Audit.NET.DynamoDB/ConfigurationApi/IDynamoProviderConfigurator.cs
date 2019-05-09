using System;
using Amazon.DynamoDBv2;

namespace Audit.DynamoDB.Configuration
{
    /// <summary>
    /// Provides a configuration for the DynamoDB data provider
    /// </summary>
    public interface IDynamoProviderConfigurator
    {
        /// <summary>
        /// Use a client with the given Service URL
        /// </summary>
        /// <param name="url">The service URL</param>
        IDynamoProviderTableConfigurator UseUrl(string url);
        /// <summary>
        /// Use a client with the given configuration
        /// </summary>
        /// <param name="dynamoDbConfig">The DynamoDB configuration</param>
        IDynamoProviderTableConfigurator UseConfig(AmazonDynamoDBConfig dynamoDbConfig);
        /// <summary>
        /// Use the given client
        /// </summary>
        /// <param name="dynamoDbClient">A DynamoDB client instance</param>
        IDynamoProviderTableConfigurator WithClient(AmazonDynamoDBClient dynamoDbClient);
        /// <summary>
        /// Use the given client
        /// </summary>
        /// <param name="dynamoDbClient">A DynamoDB client instance</param>
        IDynamoProviderTableConfigurator WithClient(IAmazonDynamoDB dynamoDbClient);
        /// <summary>
        /// Use the given client builder
        /// </summary>
        /// <param name="dynamoDbClientBuilder">A function that returns the DynamoDB client instance to use</param>
        IDynamoProviderTableConfigurator WithClient(Func<IAmazonDynamoDB> dynamoDbClientBuilder);
    }

}
