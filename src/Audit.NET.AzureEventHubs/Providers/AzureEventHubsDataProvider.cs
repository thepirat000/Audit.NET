using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Audit.AzureEventHubs.ConfigurationApi;
using Audit.Core;

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Audit.AzureEventHubs.Providers
{
    /// <summary>
    /// Data provider for Audit.NET that sends audit events to Azure Event Hubs.
    /// </summary>
    public class AzureEventHubsDataProvider : AuditDataProvider
    {
        private Lazy<EventHubProducerClient> _client;

        /// <summary>
        /// Gets or sets the connection string for the Azure Event Hubs namespace.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the target Event Hub. Optional.
        /// If not set, it will use the default Event Hub name configured in the connection string.
        /// </summary>
        public string HubName { get; set; }

        /// <summary>
        /// Gets or sets a factory method to create an EventHubProducerClient. Alternative to ConnectionString and HubName.
        /// </summary>
        public Func<EventHubProducerClient> ProducerClientFactory { get; set; }

        /// <summary>
        /// Gets or sets a callback function to modify the EventData before sending it to Event Hubs.
        /// </summary>
        public Action<EventData, AuditEvent> CustomizeEventData { get; set; }

        /// <summary>
        /// Creates a new instance of the AzureEventHubsDataProvider with default settings. 
        /// </summary>
        public AzureEventHubsDataProvider()
        {
        }

        /// <summary>
        /// Creates a new instance of the AzureEventHubsDataProvider using the specified client instance
        /// </summary>
        /// <param name="client">The EventHubProducerClient instance to use for sending events. The same client is reused for all events.</param>
        public AzureEventHubsDataProvider(EventHubProducerClient client)
        {
            ProducerClientFactory = () => client;
        }

        /// <summary>
        /// Creates a new instance of the AzureEventHubsDataProvider using the specified configurator.
        /// </summary>
        /// <param name="config"></param>
        public AzureEventHubsDataProvider(Action<IAzureEventHubsConnectionConfigurator> config)
        {
            var azureConfig = new AzureEventHubsConnectionConfigurator();

            config.Invoke(azureConfig);

            ConnectionString = azureConfig._connectionString;
            HubName = azureConfig._hubName;
            ProducerClientFactory = azureConfig._clientFactory;
            CustomizeEventData = azureConfig._azureEventHubsCustomConfigurator._customizeEventDataAction;
        }

        /// <summary>
        /// Ensures an EventHubProducerClient is created and return it.
        /// </summary>
        protected internal virtual EventHubProducerClient EnsureProducerClient()
        {
            if (_client != null)
            {
                return _client.Value;
            }

            _client = ProducerClientFactory != null 
                ? new Lazy<EventHubProducerClient>(ProducerClientFactory.Invoke, LazyThreadSafetyMode.ExecutionAndPublication) 
                : new Lazy<EventHubProducerClient>(() => new EventHubProducerClient(ConnectionString, HubName));

            return _client.Value;
        }

        /// <summary>
        /// Creates the EventData object from the AuditEvent.
        /// </summary>
        /// <param name="auditEvent">The audit event to be serialized.</param>
        protected virtual EventData CreateEventData(AuditEvent auditEvent)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(auditEvent, Configuration.JsonSettings);

            var eventData = new EventData(payload)
            {
                ContentType = "application/json"
            };
            
            return eventData;
        }

        /// <inheritdoc />
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var producer = EnsureProducerClient();

            var eventData = CreateEventData(auditEvent);

            CustomizeEventData?.Invoke(eventData, auditEvent);
            
            using var batch = producer.CreateBatchAsync().GetAwaiter().GetResult();

            if (!batch.TryAdd(eventData))
            {
                throw new InvalidOperationException("Audit event is too large for the Event Hub batch.");
            }

            producer.SendAsync(batch).GetAwaiter().GetResult();

            return null;
        }

        /// <inheritdoc />
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var producer = EnsureProducerClient();

            var eventData = CreateEventData(auditEvent);

            CustomizeEventData?.Invoke(eventData, auditEvent);

            using var batch = await producer.CreateBatchAsync(cancellationToken);

            if (!batch.TryAdd(eventData))
            {
                throw new InvalidOperationException("Audit event is too large for the Event Hub batch.");
            }

            await producer.SendAsync(batch, cancellationToken);

            return null;
        }

        /// <summary>
        /// Returns the EventHubProducerClient instance. Will return null if the client was not created.
        /// </summary>
        public EventHubProducerClient GetProducerClient()
        {
            return _client?.Value;
        }
    }
}
