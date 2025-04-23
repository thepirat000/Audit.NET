using System;
using Audit.Core;
using Azure.Messaging.EventHubs;

namespace Audit.AzureEventHubs.ConfigurationApi;

public interface IAzureEventHubsCustomConfigurator
{
    /// <summary>
    /// Customize the EventData before sending it to Azure Event Hubs.
    /// </summary>
    /// <param name="customizeEventDataAction">The action to customize the EventData. This allows you to modify properties and body before sending the event.</param>
    void CustomizeEventData(Action<EventData, AuditEvent> customizeEventDataAction);

    /// <summary>
    /// Customize the EventData before sending it to Azure Event Hubs.
    /// </summary>
    /// <param name="customizeEventDataAction">The action to customize the EventData. This allows you to modify properties and body before sending the event.</param>
    void CustomizeEventData(Action<EventData> customizeEventDataAction);
}