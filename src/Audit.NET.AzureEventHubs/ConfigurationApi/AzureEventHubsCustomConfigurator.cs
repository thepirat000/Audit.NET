using System;
using Audit.Core;
using Azure.Messaging.EventHubs;

namespace Audit.AzureEventHubs.ConfigurationApi;

public class AzureEventHubsCustomConfigurator : IAzureEventHubsCustomConfigurator
{
    internal Action<EventData, AuditEvent> _customizeEventDataAction;

    public void CustomizeEventData(Action<EventData, AuditEvent> customizeEventDataAction)
    {
        _customizeEventDataAction = customizeEventDataAction;
    }

    public void CustomizeEventData(Action<EventData> customizeEventDataAction)
    {
        _customizeEventDataAction = (eventData, _) => customizeEventDataAction.Invoke(eventData);
    }
}