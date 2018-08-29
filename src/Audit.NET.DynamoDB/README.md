# Audit.NET.DynamoDB
**Amazon Dynamo DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in Dynamo DB tables using the [AWSSDK.DynamoDBv2](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/) library.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.DynamoDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.DynamoDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.DynamoDB/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.DynamoDB.svg)](https://www.nuget.org/packages/Audit.NET.DynamoDB/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Dynamo DB data provider, or call the `UseDynamoDB` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.


For example:
```c#
Audit.Core.Configuration.DataProvider = new DynamoDataProvider()
{
    Client = new Lazy<IAmazonDynamoDB>(() => new AmazonDynamoDBClient(new AmazonDynamoDBConfig() 
    { 
        ServiceURL = "http://localhost:8000" 
    })),
    TableNameBuilder = ev => "MyTable"
};
```

Or even shorter using the constructor overload that accepts a fluent API:

```c#
Audit.Core.Configuration.DataProvider = new DynamoDataProvider(config => config
    .UseUrl("http://localhost:8000")
    .Table("MyTable"));
```


Or by using the global setup extension `UseDynamoDB()`:
```c#
Audit.Core.Configuration.Setup()
    .UseDynamoDB(config => config
        .UseUrl("http://localhost:8000")
        .Table(ev => ev.EventType));
```

> You can provide the table name setting as a string or as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).


### Provider Options

- **Client**: The DynamoDB client instance creator `AmazonDynamoDBClient`. 
- **TableNameBuilder**: A function of the audit event that returns the Table Name to use.

### Fluent API Methods

Connection level:
- **WithClient()**: Use the given DynamoDB client instance (`AmazonDynamoDBClient`).
- **UseConfig()**: Alternative to `WithClient()`, to use a DynamoDB client with the given settings (`AmazonDynamoDBConfig`).
- **UseUrl()**: Alternative to use a DynamoDB client only specifying the service URL.

Table level:
- **Table()**: To specify the table name (as a string or a function of the audit event).

Attributes level:
- **SetAttribute()**: To specify additional top-level attributes on the document before saving.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = dynamoDataProvider.GetEvent((Primitive)1234);
```

> The `eventId` parameter on `GetEvent(object eventId)` must be of type [`Primitive`](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/DynamoDBv2/TPrimitive.html) or `Primitive[]`


--------

## Constraints

This provider has the following constraints:

- The table to store the audit events must exists on DynamoDB. 
- Its indexes must consist of top-level properties of the audit event. 
(Note you can extend the [AuditEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditEvent.cs) class or add [Custom Fields](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) as new top-level properties if necessary) 

The following is an example of a table creation using the [AWSSDK.DynamoDBv2](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/) library:


```c#
var config = new AmazonDynamoDBConfig() { ServiceURL = "http://localhost:8000" };
var client = new AmazonDynamoDBClient(config);

await client.CreateTableAsync(new CreateTableRequest()
{
    TableName = "AuditEvents",
    KeySchema = new List<KeySchemaElement>()
    {
        new KeySchemaElement("EventId", KeyType.HASH),
        new KeySchemaElement("EventType", KeyType.RANGE)
    },
    AttributeDefinitions = new List<AttributeDefinition>()
    {
        new AttributeDefinition("EventId", ScalarAttributeType.S),
        new AttributeDefinition("EventType", ScalarAttributeType.N)
    },
    ProvisionedThroughput = new ProvisionedThroughput(1, 1)
});
```

In this case, the primary key is defined as a Hash and a Range key, with `EventId` being the hash, 
and `EventType` being the range. 
Both must be top-level properties of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage), 
but since the `EventId` is not a built-in property, you can add it as a [Custom Field](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) with a [Custom Action](https://github.com/thepirat000/Audit.NET#custom-actions):


```c#
// on your startup logic:
Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
{
    scope.SetCustomField("EventId", Guid.NewGuid());
});
```



