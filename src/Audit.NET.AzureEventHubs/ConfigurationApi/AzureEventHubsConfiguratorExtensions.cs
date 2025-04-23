using Audit.Core.ConfigurationApi;
using System;
using Audit.AzureEventHubs.ConfigurationApi;
using Audit.AzureEventHubs.Providers;

namespace Audit.Core;
public static class AzureEventHubsConfiguratorExtensions
{
    /// <summary>
    /// Send the events to an Azure Event Hub.
    /// </summary>
    /// <param name="configurator">The Audit.NET configurator object.</param>
    /// <param name="config">The Azure Event Hubs provider configuration.</param>
    public static ICreationPolicyConfigurator UseAzureEventHubs(this IConfigurator configurator, Action<IAzureEventHubsConnectionConfigurator> config)
    {
        Configuration.DataProvider = new AzureEventHubsDataProvider(config);
        return new CreationPolicyConfigurator();
    }
}
