using System;
using Azure.Messaging.EventHubs.Producer;

namespace Audit.AzureEventHubs.ConfigurationApi;

public interface IAzureEventHubsConnectionConfigurator
{
    /// <summary>
    /// Setup the Azure Event Hub connection with a connection string and an optional hub name.
    /// </summary>
    /// <param name="connectionString">The connection string to the Azure Event Hub namespace.</param>
    /// <param name="hubName">Optional. The name of the Event Hub to send events to. If not provided, the default Event Hub name from the connection string will be used.</param>
    IAzureEventHubsCustomConfigurator WithConnectionString(string connectionString, string hubName = null);

    /// <summary>
    /// Setup the Azure Event Hub connection with a custom client factory.
    /// </summary>
    /// <param name="clientFactory">The factory method to create an EventHubProducerClient. This allows for more control over the client configuration. The factory method is called only once and the same client is reused for all events.</param>
    IAzureEventHubsCustomConfigurator WithClientFactory(Func<EventHubProducerClient> clientFactory);

    /// <summary>
    /// Setup the Azure Event Hub connection with a custom client.
    /// </summary>
    /// <param name="client">The EventHubProducerClient instance to use for sending events. The same client is reused for all events.</param>
    /// <returns></returns>
    IAzureEventHubsCustomConfigurator WithClient(EventHubProducerClient client);
}
