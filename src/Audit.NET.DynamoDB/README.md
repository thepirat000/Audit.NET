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
    Client = new(new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { ServiceURL = "http://localhost:8000" })),
    TableName = "MyTable",
    TableBuilderAction = builder  => builder.AddHashKey("Id", DynamoDBEntryType.String),
    CustomAttributes = new Dictionary<string, Setting<object>>()
    {
        { "Id", new(ev => ev.EventType + Guid.NewGuid()) }
    }
};
```

Or using the constructor overload that accepts a fluent API:

```c#
Audit.Core.Configuration.DataProvider = new DynamoDataProvider(config => config
    .UseUrl("http://localhost:8000")
    .Table("MyTable", table => table
        .AddHashKey("Id", DynamoDBEntryType.String))
    .SetAttribute("Id", ev => ev.EventType + Guid.NewGuid()));
```


Or by using the global setup extension `UseDynamoDB()`:
```c#
Audit.Core.Configuration.Setup()
    .UseDynamoDB(config => config
        .UseUrl("http://localhost:8000")
        .Table("MyTable", table => table
            .AddHashKey("Id", DynamoDBEntryType.String))
        .SetAttribute("Id", ev => ev.EventType + Guid.NewGuid()));
```

> You can provide the table name setting as a string or as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).

> Note that you need to provide the table builder action to define the table schema, which must include the hash key and optionally a range key.

### Provider Options

- **Client**: The DynamoDB client `AmazonDynamoDBClient`. (required)
- **TableNameBuilder**: A function of the audit event that returns the Table Name to use. (required)
- **TableBuilderAction**: A function that receives a `TableBuilder` to define the table schema. This is used to define the hash key and optionally a range key. (required)
- **CustomAttributes**: A dictionary with additional fields to be included in the document and as custom fields on the audit event. (optional)

### Fluent API Methods

The provider options can be set with a fluent API described by the following methods:

##### Connection level
- **WithClient()**: Uses the provided AmazonDynamoDBClient instance.
- **UseConfig()**: Configures the DynamoDB client using the specified AmazonDynamoDBConfig, with optional credentials.
- **UseUrl()**: Sets up the DynamoDB client using only the service URL, with optional credentials.

##### Table level
- **Table()**: To specify the table name (as a string or a function of the audit event) and table configuration.

##### Attributes level
- **SetAttribute()**: To specify additional top-level attributes on the document before saving.

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = dynamoDataProvider.GetEvent((Primitive)1234);
```

> The `eventId` parameter on the generic `GetEvent(object eventId)` must be of type [`Primitive`](https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/DynamoDBv2/TPrimitive.html), 
> [`DynamoDBEntry`](https://docs.aws.amazon.com/sdkfornet1/latest/apidocs/html/T_Amazon_DynamoDB_DocumentModel_DynamoDBEntry.htm) or an array of any of these two types. 
> The first (or only) element must be the Hash key, and the second element should be the range key (or NULL if not using a range).

There are more convenient overloads of the `GetEvent`/`GetEventAsync` methods that accepts the Primitives without needing to cast the parameters:

```c#
// Get event with the given HASH and RANGE
var event = dynamoDataProvider.GetEvent("A001-005283", 2018);
```

```c#
// Get event with the given HASH
var event = dynamoDataProvider.GetEvent("A001-005283");
```

--------

## Constraints

This provider has the following constraints:

- The table to store the audit events must exists on DynamoDB. 
- Its indexes must consist of top-level properties of the audit event. 
(Note you can add properties to the AuditEvent as [Custom Fields](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) with the `SetAttribute()` method on the provider configuration)

The following is an example of a table creation using the [AWSSDK.DynamoDBv2](https://www.nuget.org/packages/AWSSDK.DynamoDBv2/) library:

```c#
var config = new AmazonDynamoDBConfig() { ServiceURL = "http://localhost:8000" };
var client = new AmazonDynamoDBClient(config);

await client.CreateTableAsync(new CreateTableRequest()
{
    TableName = "MyTable",
    KeySchema = new List<KeySchemaElement>()
    {
        new KeySchemaElement("Id", KeyType.HASH),
        new KeySchemaElement("Type", KeyType.RANGE)
    },
    AttributeDefinitions = new List<AttributeDefinition>()
    {
        new AttributeDefinition("Id", ScalarAttributeType.S),
        new AttributeDefinition("Type", ScalarAttributeType.S)
    },
    ProvisionedThroughput = new ProvisionedThroughput(1, 1)
});
```

In this case, the primary key is defined as a Hash and a Range key, with `Id` being the hash, and `Type` being the range. 
Both must be top-level properties of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage), 
but since the `Id` is not a built-in property, you can configure it as a [Custom Field](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments]:

```c#
Audit.Core.Configuration.Setup()
    .UseDynamoDB(config => config
        .UseUrl(url)
        .Table("MyTable", table => table
            .AddHashKey("Id", DynamoDBEntryType.String))
        .SetAttribute("Id", ev => Guid.NewGuid()));
```

Or you can use a global [Custom Action](https://github.com/thepirat000/Audit.NET#custom-actions) instead with the same outcome:

```c#
Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
{
    scope.SetCustomField("Id", Guid.NewGuid());
});
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)