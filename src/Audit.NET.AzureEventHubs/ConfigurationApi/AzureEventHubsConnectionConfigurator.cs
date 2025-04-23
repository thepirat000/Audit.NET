using System;

using Azure.Messaging.EventHubs.Producer;

namespace Audit.AzureEventHubs.ConfigurationApi;
public class AzureEventHubsConnectionConfigurator : IAzureEventHubsConnectionConfigurator
{
    internal string _connectionString;
    internal string _hubName;
    internal Func<EventHubProducerClient> _clientFactory;
    internal AzureEventHubsCustomConfigurator _azureEventHubsCustomConfigurator = new AzureEventHubsCustomConfigurator();

    public IAzureEventHubsCustomConfigurator WithConnectionString(string connectionString, string hubName = null)
    {
        _connectionString = connectionString;
        _hubName = hubName;
        _clientFactory = null;

        return _azureEventHubsCustomConfigurator;
    }

    public IAzureEventHubsCustomConfigurator WithClientFactory(Func<EventHubProducerClient> clientFactory)
    {
        _connectionString = null;
        _hubName = null;
        _clientFactory = clientFactory;

        return _azureEventHubsCustomConfigurator;
    }

    public IAzureEventHubsCustomConfigurator WithClient(EventHubProducerClient client)
    {
        _connectionString = null;
        _hubName = null;
        _clientFactory = () => client;

        return _azureEventHubsCustomConfigurator;
    }
}
