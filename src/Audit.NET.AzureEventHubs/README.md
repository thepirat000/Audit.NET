# Audit.NET.AzureEventHubs
**Azure Event Hubs** provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (An extensible framework to audit executing operations in .NET).

Send the audit events to an Azure Event Hub.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AzureEventHubs
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AzureEventHubs.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AzureEventHubs/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AzureEventHubs.svg)](https://www.nuget.org/packages/Audit.NET.AzureEventHubs/)

## How it works

This library uses the [Azure.Messaging.EventHubs](https://www.nuget.org/packages/Azure.Messaging.EventHubs) library to send the Audit Events to an Azure Event Hub.

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to an instance of `AzureEventHubsDataProvider`, or call the `UseAzureEventHubs()` 
methods on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

```c#
Audit.Core.Configuration.DataProvider = new AzureEventHubsDataProvider()
{
    ConnectionString = "..."
};
```

Or

```c#
Audit.Core.Configuration.DataProvider = new AzureEventHubsDataProvider(e => e
        .WithConnectionString("..."));
```

Or 

```c#
Audit.Core.Configuration.Setup()
    .UseAzureEventHubs(e => e
        .WithConnectionString("..."));
```

#### Provider Options

##### Connection options

Depending on the authentication method, you can call one of the following methods:

- **`WithConnectionString`**: Connect using an Azure Storage connection string and an optional Hub Name.
- **`WithClientFactory`**: Connect using a custom `EventHubProducerClient` factory. Alternative to `WithConnectionString`. Useful when you want to use a custom `EventHubClientOptions` or if you want to use a custom `TokenCredential` for authentication. 

##### Customization options

- **`CustomizeEventData`**: Customize the `EventData` object before sending it to the Event Hub. 

#### Configuration examples

Using a connection string and a hub name, with a custom message id and property:
 
```c#
Audit.Core.Configuration.Setup()
    .UseAzureEventHubs(config => config
        .WithConnectionString(Settings.ConnectionString, Settings.HubName)
        .CustomizeEventData((eventData, auditEvent) =>
        {
            eventData.MessageId = Guid.NewGuid().ToString();
            eventData.Properties["EventType"] = auditEvent.EventType;
        }));
```

Using a Client factory to create the `EventHubProducerClient`:

```c#
Audit.Core.Configuration.Setup()
    .UseAzureEventHubs(config => config
        .WithClientFactory(() => serviceProvider.GetService<EventHubProducerClient>()));
```

Using a custom `EventHubProducerClient` instance:

```c#
Audit.Core.Configuration.Setup()
    .UseAzureEventHubs(config => config
        .WithClient(new EventHubProducerClient(Settings.ConnectionString));
```
