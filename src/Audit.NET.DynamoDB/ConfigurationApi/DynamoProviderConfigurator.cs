using System;
using Amazon.DynamoDBv2;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderConfigurator : IDynamoProviderConfigurator
    {
        internal Lazy<IAmazonDynamoDB> _clientFactory;
        internal DynamoProviderTableConfigurator _tableConfigurator = new DynamoProviderTableConfigurator();

        public IDynamoProviderTableConfigurator WithClient(AmazonDynamoDBClient dynamoDbClient)
        {
            _clientFactory = new Lazy<IAmazonDynamoDB>(() => dynamoDbClient);
            return _tableConfigurator;
        }

        public IDynamoProviderTableConfigurator WithClient(IAmazonDynamoDB dynamoDbClient)
        {
            _clientFactory = new Lazy<IAmazonDynamoDB>(() => dynamoDbClient);
            return _tableConfigurator;
        }

        public IDynamoProviderTableConfigurator UseConfig(AmazonDynamoDBConfig dynamoDbConfig)
        {
            _clientFactory = new Lazy<IAmazonDynamoDB>(() => new AmazonDynamoDBClient(dynamoDbConfig));
            return _tableConfigurator;
        }

        public IDynamoProviderTableConfigurator WithClient(Func<IAmazonDynamoDB> dynamoDbClientBuilder)
        {
            _clientFactory = new Lazy<IAmazonDynamoDB>(dynamoDbClientBuilder);
            return _tableConfigurator;
        }

        public IDynamoProviderTableConfigurator UseUrl(string serviceUrl)
        {
            _clientFactory = new Lazy<IAmazonDynamoDB>(() => new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { ServiceURL = serviceUrl }));
            return _tableConfigurator;
        }
    }
}
