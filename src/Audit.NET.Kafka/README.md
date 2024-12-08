# Audit.NET.Kafka
**Apache Kafka Server provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Send the audit events to an Apache Kafka topic.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.Kafka
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.Kafka.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.Kafka/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.Kafka.svg)](https://www.nuget.org/packages/Audit.NET.Kafka/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Kafka data provider, or call the `UseKafka` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new KafkaDataProvider(producerConfig)
{
    TopicSelector = _ => "audit-topic"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseKafka(_ => _
        .ProducerConfig(producerConfig)
        .Topic("audit-topic"));
```

If you want to use [keyed messages](https://www.confluent.io/stream-processing-cookbook/ksql-recipes/setting-kafka-message-key/#:~:text=Kafka%20messages%20are%20key%2Fvalue,for%20query%20or%20join%20purposes.)
you have to use the generic `KafkaDataProvider<TKey>` and provide a way to obtain the key for each audit event:

```c#
Audit.Core.Configuration.Setup()
    .UseKafka<string>(_ => _
        .ProducerConfig(producerConfig)
        .Topic("audit-topic")
        .KeySelector(ev => ev.EventType));
```

### Provider Options

- **ProducerConfig**: Instance of [`ProducerConfig`](https://docs.confluent.io/5.5.0/clients/confluent-kafka-dotnet/api/Confluent.Kafka.ProducerConfig.html) with the producer configuration properties
- **Topic**: The topic name to send the messages. Default is "audit-topic".
- **TopicSelector**: A function of the audit event that returns the topic name to use.
- **PartitionSelector**: (optional) A function of the audit event that returns the partition index to use.
- **KeySelector**: When using keyed messages, a function of the audit event that returns the key to use.
- **HeadersSelector**: Optional to use message headers. Configure the message headers to be used for a given audit event.
- **KeySerializer**: When using keyed messages to set a custom serializer for the key.
- **AuditEventSerializer**: Custom AuditEvent serializer. By default the audit event is JSON serialized + UTF8 encoded.
- **ResultHandler**: An action to be called for each kafka response.

