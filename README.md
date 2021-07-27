# Audit.NET

<sub>Quick navigation</sub><br/>
**[USAGE](#usage) | [OUTPUT](#output) | [CUSTOMIZATION](#custom-fields-and-comments) | [DATA PROVIDERS](#data-providers) | [CREATION POLICY](#event-creation-policy) | [CONFIGURATION](#configuration) | [EXTENSIONS](#extensions)**

<sub>issues</sub> | <sub>chat / support</sub> | <sub>donations</sub>
------------ | ---------------- |  --------------
[![issues-open](https://img.shields.io/github/issues-raw/thepirat000/Audit.NET.svg?label=open)](https://github.com/thepirat000/Audit.NET/issues)[![issues-closed](https://img.shields.io/github/issues-closed-raw/thepirat000/Audit.NET.svg?label=closed)](https://github.com/thepirat000/Audit.NET/issues) | [![Gitter](https://img.shields.io/gitter/room/Audit.NET/Lobby.svg?label=english)](https://gitter.im/Audit-NET/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=body_badge) [![Gitter](https://img.shields.io/gitter/room/Audit.NET/Audit.NET-Spanish.svg?label=español)](https://gitter.im/Audit-NET/Audit.NET-Spanish?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=body_badge) | [![backers](https://opencollective.com/auditnet/tiers/backer/badge.svg?label=backer&color=brightgreen)](https://opencollective.com/auditnet) [![paypal](https://img.shields.io/badge/donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=thepirat000%40hotmail.com&currency_code=USD&source=url)

### An extensible framework to audit executing operations in .NET and .NET Core.

Generate [audit logs](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected specific operations or procedures. 

With Audit.NET you can generate tracking information about operations being executed. It gathers environmental information such as the caller user id, machine name, method name, exceptions, including execution time and exposing an extensible mechanism to enrich the logs and handle the audit output.

[**Output extensions**](#storage-providers) are provided to log to [JSON Files](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/FileDataProvider.cs), 
[Event Log](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/EventLogDataProvider.cs), [SQL](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.SqlServer/README.md), 
[MySQL](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MySql/README.md), 
[PostgreSQL](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.PostgreSql/README.md), 
[MongoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MongoDB/README.md), 
[AzureBlob](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureStorage/README.md), 
[AzureCosmos](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureCosmos/README.md), 
[Redis](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Redis/README.md), 
[Elasticsearch](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.ElasticSearch/README.md), 
[DynamoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.DynamoDB/README.md), 
[UDP datagrams](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.Udp/README.md) and more. 

[**Interaction extensions**](#extensions) to audit different systems are provided, such as [Entity Framework](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md), 
[MVC](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/README.md), 
[WebAPI](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/README.md), 
[WCF](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/README.md), 
[File System](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.FileSystem/README.md), 
[SignalR](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/README.md) 
and [HttpClient](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/README.md).

## [NuGet](https://www.nuget.org/packages/Audit.NET/)

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.svg?style=flat)](https://www.nuget.org/packages/Audit.NET/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.svg)](https://www.nuget.org/packages/Audit.NET/)

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET
```

## Changelog
Check the [CHANGELOG.md](https://github.com/thepirat000/Audit.NET/blob/master/CHANGELOG.md) file.

## <span style="color:red">Breaking change in version 18</span>

Starting on version 18, Audit.NET will default to diferent serialization mechanism depending on the target framework of the client application, 
as shown on the following table:

| Target | Serialization |
| ------------ | ---------------- |
| **≥ .NET 5.0** | `System.Text.Json` |
| **≤ .NETSTANDARD2.1 / .NETCOREAPP3.1** | `Newtonsoft.Json` |
| **≤ .NET 4.8** | `Newtonsoft.Json` |

- `System.Text.Json` is the new default for applications and libraries targeting .NET 5.0 or higher
- `Newtonsoft.Json` will still be the default for applications and libraries targeting lower framework versions.

If you want to change the default behavior, refer to [Custom serialization mechanism](#custom-serialization-mechanism).

## Usage

The **Audit Scope** is the central object of this framework. It encapsulates an audit event, controlling its life cycle. 
The **Audit Event** is an extensible information container of an audited operation. 
See the [audit scope statechart](#auditscope-statechart).



There are several ways to create an Audit Scope:

- Calling the `Create()` / `CreateAsync()` method of an `AuditScopeFactory` instance, for example:

    ```c#
    var factory = new AuditScopeFactory();
    var scope = factory.Create(new AuditScopeOptions(...));
    ```

- Using the overloads of the static methods `Create()` / `CreateAsync()` on `AuditScope`, for example:

    ```c#
    var scope = AuditScope.Create("Order:Update", () => order, new { MyProperty = "value" });
    ```

    The first parameter of the `AuditScope.Create` method is an _event type name_ intended to identify and group the events. The second is the delegate to obtain the object to track (target object). This object is passed as a `Func<object>` to allow the library to inspect the value at the beginning and at the disposal of the scope. It is not mandatory to supply a target object.
    
    You can use the overload that accepts an `AuditScopeOptions` instance to configure any of the available options for the scope:

    ```c#
    var scope = AuditScope.Create(new AuditScopeOptions()
    {
      EventType = "Order:Update",
      TargetGetter = () => order,
      ExtraFields = new { MyProperty = "value" }
    });
    ```

- Using the provided fluent API, for example:

    ```c#
    var scope = AuditScope.Create(_ => _
        .EventType("Order:Update")
        .ExtraFields(new { MyProperty = "value" })
        .Target(() => order));
    ```

#### AuditScope options

Option | Type | Description 
------------ | ---------------- | ---------------- 
EventType | `string` | A string representing the type of the event
TargetGetter | `Func<object>` | Target object getter (a func that returns the object to track)
ExtraFields | `object` | Anonymous object that contains additional fields to be merged into the audit event
DataProvider | `AuditDataProvider` | The [data provider](#data-providers) to use. Defaults to the DataProvider configured on `Audit.Core.Configuration.DataProvider`
CreationPolicy | `EventCreationPolicy` | The [creation policy](#creation-policy) to use. Default is `InsertOnEnd` 
IsCreateAndSave | `bool` | Value indicating whether this scope should be immediately ended and saved after creation. Default is false
AuditEvent | `AuditEvent` | Custom initial audit event to use. By default it will create a new instance of basic `AuditEvent`
SkipExtraFrames | `int` | Value used to indicate how many frames in the stack should be skipped to determine the calling method. Default is 0
CallingMethod | `MethodBase` | Specific calling method to store on the event. Default is to use the calling stack to determine the calling method.

Suppose you have the following code to _cancel an order_ that you want to audit:

```c#
Order order = Db.GetOrder(orderId);
order.Status = -1;
order.OrderItems = null;
order = Db.OrderUpdate(order);
```

To audit this operation, you can surround the code with a `using` block that creates an `AuditScope`, indicating a target object to track:

```c#
Order order = Db.GetOrder(orderId);
using (AuditScope.Create("Order:Update", () => order))
{
    order.Status = -1;
    order.OrderItems = null;
    order = Db.OrderUpdate(order);
}
```

> It is not mandatory to use a `using` block, but it simplifies the syntax when the code to audit is on a single block, allowing the detection of exceptions and calculating the duration by implicitly saving the event on disposal. 

> When using the [extensions](#extensions) that logs interactions with different systems, like [Audit.EntityFramework](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md), [Audit.WebApi](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/README.md), etc. you don't need to explicitly create the `AuditScope` or `AuditEvent`, they are created internally by the extension.

### Simple logging

If you are not tracking an object, nor the duration of an event, you can use the `Log` shortcut method that logs an event immediately. 
For example:
```c#
AuditScope.Log("Event Type", new { ExtraField = "extra value" });
```

### Manual Saving

You can control the creation and saving logic, by creating a _manual_ `AuditScope`. For example to log a pair of `Start`/`End` method calls as a single event:

```c#
public class SomethingThatStartsAndEnds
{
    private AuditScope auditScope;

    public int Status { get; set; }

    public void Start()
    {
        // Create a manual scope
        auditScope = AuditScope.Create(new AuditScopeOptions()
        {
            EventType = "MyEvent",
            TargetGetter = () => this.Status,
            CreationPolicy = EventCreationPolicy.Manual
        });
    }

    public void End()
    {
        // Save the event
        auditScope.Save();  
        // Discard to avoid further saving
        auditScope.Discard();
    }
}
```

For more information about the `EventCreationPolicy` please see [Event Creation Policy](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-creation-policy) section.

## Asynchronous operations

Asynchronous versions of the operations that saves audit logs are also provided. For example:

```c#
public async Task SaveOrderAsync(Order order)
{
    AuditScope auditScope = null;
    try
    {
        // async scope creation
        auditScope = await AuditScope.CreateAsync("order", () => order);
    }
    finally
    {
        // async disposal
        await auditScope.DisposeAsync();
    }
}
```

> Note: On older .NET framework versions [the `Dispose` method was always synchronous](https://github.com/dotnet/roslyn/issues/114), so if your audit code is on async methods and you created the scope within a `using` statement, you should explicitly call the `DisposeAsync()` method. For projects targeting .NET Standard starting on version 2.0 and C# 8, you can simply use the `await using` statement, since the `AuditScope` implements the [`IAsyncDisposable` interface](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncdisposable).

## Output

The library will generate an output (`AuditEvent`) for each operation, including:
- Tracked object's state before and after the operation.
- Execution time and duration.
- Environment information such as user, machine, domain, locale, etc.
- [Comments and Custom Fields](#custom-fields-and-comments) provided.

An example of the output in JSON:

```javascript
{
    "EventType": "Order:Update",
    "Environment": {
        "UserName": "Federico",
        "MachineName": "HP",
        "DomainName": "HP",
        "CallingMethodName": "Audit.UnitTest.AuditTests.TestUpdate()",
        "Exception": null,
        "Culture": "en-GB"
    },
    "StartDate": "2016-08-23T11:33:14.653191-05:00",
    "EndDate": "2016-08-23T11:33:23.1820786-05:00",
    "Duration": 8529,
    "Target": {
        "Type": "Order",
        "Old": {
            "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
            "Status": 2,
            "OrderItems": [{
                "Sku": "1002",
                "Quantity": 3.0
            }]
        },
        "New": {
            "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
            "Status": -1,
            "OrderItems": null
        }
    }
}
```

## Output details

The following tables describes the output fields:

- ### [AuditEvent object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditEvent.cs)
Field Name | Type | Description 
------------ | ---------------- |  -------------- 
**EventType** | string | User-defined string to group the events 
**Environment** | [**Environment**](#environment-object) | Contains information about the execution environment 
**StartDate** | DateTime | Date and time when the event has started 
**EndDate** | DateTime | Date and time when the event has ended 
**Duration** | integer | Duration of the event in milliseconds 
**Target** | [**Target**](#target-object) | User-defined tracked object 
**Comments** | Array of strings | User-defined comments 
**CustomFields** | Dictionary | User-defined custom fields 


- ### [Environment object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditEventEnvironment.cs)
Field Name | Type | Description
------------ | ---------------- |  --------------
**UserName** | string | Current logged user name
**MachineName** | string | Executing machine name
**DomainName** | string | Current user domain
**CallingMethodName** | string | Calling method signature information
**Exception** | string | Indicates if an Exception has been detected (NULL if no exception has been thrown)
**Culture** | string | Current culture identifier

- ### [Target object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditTarget.cs)
Field Name | Type | Description
------------ | ---------------- |  --------------
**Type** | string | Tracked object type name
**Old** | Object | Value of the tracked object at the beginning of the event
**New** | Object | Value of the tracked object at the end of the event

## Custom Fields and Comments

The `AuditScope` object provides two methods to extend the event output.

- Use `SetCustomField()` method to add any object as an extra field to the event.
- Use `Comment()` to add textual comments to the event's `Comments` array.

For example:

```c#
Order order = Db.GetOrder(orderId);
using (var audit = AuditScope.Create("Order:Update", () => order))
{
    audit.SetCustomField("ReferenceId", orderId);
    order.Status = -1;
    order = Db.OrderUpdate(order);
    audit.Comment("Status Updated to Cancelled");
}
```

You can also set Custom Fields when creating the `AuditScope`, by passing an anonymous object with the properties you want as extra fields. For example:

```c#
using (var audit = AuditScope.Create("Order:Update", () => order, new { ReferenceId = orderId }))
{
    order.Status = -1;
    order = Db.OrderUpdate(order);
    audit.Comment("Status Updated to Cancelled");
}
```

You can also access the Custom Fields directly from `Event.CustomFields` property of the scope. For example:
```c#
using (var audit = AuditScope.Create("Order:Update", () => order, new { ReferenceId = orderId }))
{
    audit.Event.CustomFields["ReferenceId"] = orderId;
}
```

> Custom fields are not limited to single properties, you can store any object as well, by default they will be JSON serialized.

### Extending AuditEvent

Another way to enrich the event output is to create a class inheriting from the `AuditEvent` class, then you can pass an instance of your class to the AuditScope.Create method. For example:

```c#
public class YourAuditEvent : AuditEvent
{
    public Guid ReferenceId { get; set; } = Guid.NewGuid();
}

using (var scope = AuditScope.Create(new AuditScopeOptions { AuditEvent = new YourAuditEvent() }))
{
    //...
}
```

The output of the previous examples would be:

```javascript
{
    "EventType": "Order:Update",
    "Environment": {
        "UserName": "Federico",
        "MachineName": "HP",
        "DomainName": "HP",
        "CallingMethodName": "Audit.UnitTest.AuditTests.TestUpdate()",
        "Exception": null,
        "Culture": "en-GB"
    },
    "Target": {
        "Type": "Order",
        "Old": {
            "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
            "Status": 2,
            
        },
        "New": {
            "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
            "Status": -1,
            
        }
    },
    "ReferenceId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",           // <-- Custom Field
    "Comments": ["Status Updated to Cancelled"],                     // <-- Comments
    "StartDate": "2016-08-23T11:34:44.656101-05:00",
    "EndDate": "2016-08-23T11:34:55.1810821-05:00",
    "Duration": 8531
}
```

## Discard option

The `AuditScope` object has a `Discard()` method to allow the user to discard an event. Discarding an event means it won't be saved.

For example, if you want to avoid saving the audit event under certain condition:

```c#
using (var scope = AuditScope.Create(new AuditScopeOptions("SomeEvent", () => someTarget)))
{
    try
    {
        //some operation
        Critical.Operation();
    }
    catch (Exception ex)
    {
        //If an exception is thrown, discard the audit event
        scope.Discard();
    }
}
```

## Data providers

A _data provider_ (or _storage sink_) contains the logic to handle the audit event output, where you define what to do with the audit logs.

You can use one of the [data providers included](#data-providers-included) or inject your own mechanism 
by creating a class that inherits from `AuditDataProvider` and overrides its methods:

- `InsertEvent`: should store the event and return a unique ID for it. 
- `ReplaceEvent`: should update an event given its ID. This method is only called for [Creation Policies](#event-creation-policy) **Manual** or **InsertOnStartReplaceOnEnd**.

If your data provider will support asynchronous operations, you must also implement the following methods:

- `InsertEventAsync`: Asynchronous implementation of the InsertEvent method. 
- `ReplaceEventAsync`: Asynchronous implementation of the ReplaceEvent method.

Also, if your data provider will support event retrieval, you should implement the methods:

- `GetEvent`: Retrieves an event by id. 
- `GetEventAsync`: Asynchronous implementation of the GetEvent method. 


For example:
```c#
public class MyCustomDataProvider : AuditDataProvider
{
    public override object InsertEvent(AuditEvent auditEvent)
    {
        var fileName = $"Log{Guid.NewGuid()}.json";
        File.WriteAllText(fileName, auditEvent.ToJson());
        return fileName;
    }
    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        var fileName = eventId.ToString();
        File.WriteAllText(fileName, auditEvent.ToJson());
    }
    public override T GetEvent<T>(object eventId)
    {
        var fileName = eventId.ToString();
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(fileName));
    }
    // async implementation:
    public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
    {
        var fileName = $"Log{Guid.NewGuid()}.json";
        await File.WriteAllTextAsync(fileName, auditEvent.ToJson());
        return fileName;
    }
    public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
    {
        var fileName = eventId.ToString();
        await File.WriteAllTextAsync(fileName, auditEvent.ToJson());
    }
    public override async Task<T> GetEventAsync<T>(object eventId) 
    {
        var fileName = eventId.ToString();
        return await GetFromFileAsync<T>(fileName);
    }
}
```

### Data provider selection

The data provider can be set globally for the entire application or per audit scope.

To set the global data provider assign the `DataProvider` property on the static `Audit.Core.Configuration` object. For example:

```c#
Audit.Core.Configuration.DataProvider = new MyCustomDataProvider();
```

Or using the fluent API `UseCustomProvider` method:

```c#
Audit.Core.Configuration.Setup()
	.UseCustomProvider(new MyCustomDataProvider());
```

You can also set the global data provider with a factory method that is called when an Audit Scope is created. For example:

```c#
Audit.Core.Configuration.DataProviderFactory = () => new LazyDataProvider();
```

Or using the fluent API `UseFactory`: 

```c#
Audit.Core.Configuration.Setup()
	.UseFactory(() => new LazyDataProvider());
```

**NOTE:** If you don't specify a global data provider, it will default to a `FileDataProvider` that logs events as .json files into the current working directory.

See [Configuration section](#configuration) for more information.

To set the data provider per-scope, use the `AuditScopeOptions` when creating an `AuditScope`. For example:

```c#
var scope = AuditScope.Create(new AuditScopeOptions 
{ 
  DataProvider = new MyCustomDataProvider(), ... }
);
```


### Dynamic data providers 

As an alternative to creating a data provider class, you can define the mechanism at run time by using the `DynamicDataProvider` or `DynamicAsyncDataProvider` classes. For example:

```c#
var dataProvider = new DynamicDataProvider();
// Attach an action for insert
dataProvider.AttachOnInsert(ev => Console.Write(ev.ToJson()));
Audit.Core.Configuration.DataProvider = dataProvider;
```

Or by using the fluent API:
 
```c#
Audit.Core.Configuration.Setup()
	.UseDynamicProvider(config => config
		.OnInsert(ev => Console.Write(ev.ToJson())));
```

For async operations you should use the `DynamicAsyncDataProvider`, for example:

```c#
var dataProvider = new DynamicAsyncDataProvider();
dataProvider.AttachOnInsert(async ev => await File.WriteAllTextAsync(filePath, ev.ToJson()));
Audit.Core.Configuration.DataProvider = dataProvider;
```

Or by using the fluent API:
 
```c#
Audit.Core.Configuration.Setup()
    .UseDynamicAsyncProvider(config => config
        .OnInsert(async ev => await File.WriteAllTextAsync(filePath, ev.ToJson())));
```

#### Data providers included

The Data Providers included are summarized in the following table:

Data Provider | Package | Description | [Configuration API](#configuration-fluent-api) |
------------ | ---------------- |  ----------------------------------------------------------------- | ------------------ |
[FileDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/FileDataProvider.cs) | [Audit.NET](https://github.com/thepirat000/Audit.NET) | Store the audit logs as files. Dynamically configure the directory and path. | `.UseFileLogProvider()`
[EventLogDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/EventLogDataProvider.cs) | [Audit.NET](https://github.com/thepirat000/Audit.NET) [Audit.NET.EventLog.Core](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.EventLog.Core) | Write the audit logs to the Windows EventLog. | `.UseEventLogProvider()`
[DynamicDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/DynamicDataProvider.cs) / [DynamicAsyncDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/Providers/DynamicAsyncDataProvider.cs) | [Audit.NET](https://github.com/thepirat000/Audit.NET) | Dynamically change the behavior at run-time. Define _Insert_ and a _Replace_ actions with lambda expressions. | `.UseDynamicProvider()` / `.UseDynamicAsyncProvider()`
[SqlDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.SqlServer/Providers/SqlDataProvider.cs) | [Audit.NET.SqlServer](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.SqlServer#auditnetsqlserver) | Store the events as rows in a **MS SQL** Table, in JSON format. | `.UseSqlServer()`
[MySqlDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MySql/Providers/MySqlDataProvider.cs) | [Audit.NET.MySql](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.MySql#auditnetmysql) | Store the events as rows in a **MySQL** database table, in JSON format. | `.UseMySql()` 
[PostgreSqlDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.PostgreSql/Providers/PostgreSqlDataProvider.cs) | [Audit.NET.PostgreSql](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.PostgreSql#auditnetpostgresql) | Store the events as rows in a **PostgreSQL** database table, in JSON format. | `.UsePostgreSql()`
[MongoDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MongoDB/Providers/MongoDataProvider.cs) | [Audit.NET.MongoDB](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.MongoDB#auditnetmongodb) | Store the events in a **Mongo DB** collection, in BSON format. | `.UseMongoDB()`
[AzureCosmosDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureCosmos/Providers/AzureCosmosDataProvider.cs) | [Audit.NET.AzureCosmos](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.AzureCosmos#auditnetazurecosmos) | Store the events in an **Azure Cosmos DB** container, in JSON format. | `.UseAzureCosmos()`
[AzureBlobDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureStorage/Providers/AzureBlobDataProvider.cs) | [Audit.NET.AzureStorage](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.AzureStorage#auditnetazurestorage) | Store the events in an **Azure Blob Storage** container, in JSON format. | `.UseAzureBlobStorage()`
[AzureTableDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureStorage/Providers/AzureTableDataProvider.cs) | [Audit.NET.AzureStorage](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.AzureStorage#auditnetazurestorage) | Store the events in an **Azure Table**. | `.UseAzureTableStorage()`
[UdpDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Udp/Providers/UdpDataProvider.cs) | [Audit.NET.Udp](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Udp/README.md) | Send Audit Logs as UDP datagrams to a network. | `.UseUdp()`
[RedisDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Redis/Providers/RedisDataProvider.cs) | [Audit.NET.Redis](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Redis/README.md) | Store audit logs in Redis as Strings, Lists, SortedSets, Hashes or publish to a PubSub channel. | `.UseRedis()`
[Log4netDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.log4net/Providers/Log4netDataProvider.cs) | [Audit.NET.log4net](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.log4net/README.md) | Store the audit events using [Apache log4net™](https://logging.apache.org/log4net/). | `.UseLog4net()`
[EntityFrameworkDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/Providers/EntityFrameworkDataProvider.cs) | [Audit.EntityFramework](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md#entity-framework-data-provider) | Store EntityFramework audit events in the same EF context. (This data provider can only be used for Entity Framework audits) | `.UseEntityFramework()`
[ElasticsearchDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.ElasticSearch/Providers/ElasticsearchDataProvider.cs) | [Audit.NET.Elasticsearch](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.ElasticSearch/README.md) | Store audit events in Elasticsearch indices. | `.UseElasticsearch()`
[DynamoDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.DynamoDB/Providers/DynamoDataProvider.cs) | [Audit.NET.DynamoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.DynamoDB/README.md) | Store audit events in [Amazon DynamoDB™](https://aws.amazon.com/dynamodb/) tables. | `.UseDynamoDB()`
[NLogDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.NLog/Providers/NLogDataProvider.cs) | [Audit.NET.NLog](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.NLog/README.md) | Store the audit events using [NLog](https://nlog-project.org/). | `.UseNLog()`
[AmazonQldbDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AmazonQLDB/Providers/AmazonQldbDataProvider.cs) | [Audit.NET.AmazonQLDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AmazonQLDB/README.md) | Store the audit events using [Amazon QLDB](https://aws.amazon.com/es/qldb/). | `.UseAmazonQldb()`
[KafkaDataProvider](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Kafka/Providers/KafkaDataProvider.cs) | [Audit.NET.Kafka](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Kafka/README.md) | Stream the audit events to [Apache Kafka](https://kafka.apache.org/) topics. | `.UseKafka()` / `.UseKafka<TKey>()`

## Event Creation Policy

The audit scope can be configured to call its data provider in different ways:
- **Insert on End:** (**default**)
The audit event is inserted when the scope is disposed. 

- **Insert on Start, Replace on End:**
The event (on its initial state) is inserted when the scope is created, and then the complete event information is replaced when the scope is disposed. 

- **Insert on Start, Insert on End:**
Two versions of the event are inserted, the initial when the scope is created, and the final when the scope is disposed.

- **Manual:**
The event saving (insert/replace) should be explicitly invoked by calling the `Save()` method on the `AuditScope`.

You can set the Creation Policy per-scope, for example to explicitly set the Creation Policy to Manual:
```c#
using (var scope = AuditScope.Create(new AuditScopeOptions { CreationPolicy = EventCreationPolicy.Manual }))
{
    //...
    scope.Save();
}
```

> If you don't provide a Creation Policy, the default Creation Policy configured will be used (see the [configuration](#configuration) section).

## AuditScope statechart

The following is the internal state machine representation of the `AuditScope` object:

<img src="https://i.imgur.com/7WqGECe.png" alt="AuditScope statecart" />

# Configuration

### Data provider
To change the default data provider, set the static property `DataProvider` on `Audit.Core.Configuration` class. This should be done prior to the `AuditScope` creation, i.e. during application startup.

For example, to set your own provider as the default data provider:
```c#
Audit.Core.Configuration.DataProvider = new MyCustomDataProvider();
```

> If you don't specify a Data Provider, a default `FileDataProvider` will be used to write the events as .json files into the current working directory.

### Creation Policy
To change the default creation policy, set the static property `CreationPolicy` on `Audit.Core.Configuration` class. This should be done prior to the `AuditScope` creation, i.e. during application startup.
 
For example, to set the default creation policy to Manual:
```c#
Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;
```

> If you don't specify a Creation Policy, the default `Insert on End` will be used.

### Custom Actions
You can configure Custom Actions that are executed for all the Audit Scopes in your application. This allows to globally change the behavior and data, intercepting the scopes after they are created or before they are saved.

Call the static `AddCustomAction()` method on `Audit.Core.Configuration` class to attach a custom action. 

For example, to globally discard the events under a certain condition:
```c#
Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
{
    if (DateTime.Now.Hour == 17) // Tea time
    {
        scope.Discard();
    }
});
```

Or to add custom fields / comments globally to all scopes:
```c#
Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
{
    if (scope.Event.Environment.Exception != null)
    {
        scope.SetCustomField("Oops", true);
    }
    scope.Comment("Saved at " + DateTime.Now);
});
```

Custom actions can also be asynchronous, for example:
```c#
Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, async scope =>
{
    var result = await svcProvider.GetService<InfoService>().GetInfoAsync();
    scope.SetCustomField("Info", result);
});
```

The `ActionType` indicates when to perform the action. The allowed values are:
- `OnScopeCreated`: When the Audit Scope is being created, before any saving. This is executed once per Audit Scope.
- `OnEventSaving`: When an Audit Scope's Event is about to be saved. 
- `OnEventSaved`: After an Audit Scope's Event is saved. 

### Global switch off

You can disable audit logging by setting the static property `Configuration.AuditDisabled` to `true`. 
The audit events are globally ignored while this flag is set. For example to disable the audits on certain environment:

```c#
if (environment.IsDevelopment())
{
    Audit.Core.Configuration.AuditDisabled = true;
}
```

### Global serialization settings
Most of the data providers serializes audit events in JSON format. 

The default mechanism for serialization depends on the target framework of your application:

- Targeting **.NET 5.0** or higher: The JSON serialization is done with Microsoft's `System.Text.Json` library.
- Targeting lower framework versions: The JSON serialization is done with James Newton-King's `Newtonsoft.Json` library.

You can change the settings for the default serialization mechanism via the static property `Configuration.JsonSettings`.

For example, when using _Newtonsoft.Json_:

```c#
Audit.Core.Configuration.JsonSettings = new JsonSerializerSettings()
{
    NullValueHandling = NullValueHandling.Ignore,
    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
    Converters = new List<JsonConverter>() { new MyStreamConverter() }
};
```

Or, if you target net5.0, using System.Text.Json:

```c#
Audit.Core.Configuration.JsonSettings = new JsonSerializerOptions
{
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    AllowTrailingCommas = true
};
```

#### Custom serialization mechanism

If you want to use a custom JSON serialization mechanism, you should create a class implementing `IJsonAdapter` and assign it to the 
static property `Configuration.JsonAdapter`.

For example:
```c#
Audit.Core.Configuration.JsonAdapter = new MyCustomAdapter(); 
```

Or by using the fluent API:
```c#
Audit.Core.Configuration.Setup()
    .JsonAdapter<MyCustomAdapter>()
    ...
```

#### Alternative serialization mechanism

There are also two libraries provided to use the alternative JSON serialization mechanism:

- [`Audit.NET.JsonNewtonsoftAdapter`](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.JsonNewtonsoftAdapter): 

    When you target .NET >= 5.0 but want to use `Newtonsoft.Json`.

    Add a reference to the library `Audit.NET.JsonNewtonsoftAdapter` and set an instance of `JsonNewtonsoftAdapter`
    to the static configuration property `Configuration.JsonAdapter`, for example:

    ```c#
    var settings = new JsonSerializerSettings()
    {
        TypeNameHandling = TypeNameHandling.All
    };
    Audit.Core.Configuration.JsonAdapter = new JsonNewtonsoftAdapter(settings);
    ```

    Or by calling the `JsonNewtonsoftAdapter()` fluent configuration API:

    ```c#
    Audit.Core.Configuration.Setup()
        .JsonNewtonsoftAdapter(settings)
        ...
    ```

- [`Audit.NET.JsonSystemSerializer`](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.JsonSystemAdapter): 

    When you target an older .NET Framework, but want to use `System.Text.Json`
 
    Add a reference to the library `Audit.NET.JsonSystemSerializer` and set an instance of `JsonSystemAdapter`
    to the static configuration property `Configuration.JsonAdapter`, for example:

    ```c#
    var options = new JsonSerializerOptions()
    {
        WriteIndented = true
    };
    Audit.Core.Configuration.JsonAdapter = new JsonSystemAdapter(options);
    ```

    Or by calling the `JsonSystemAdapter()` fluent configuration API:

    ```c#
    Audit.Core.Configuration.Setup()
        .JsonSystemAdapter(options)
        ...
    ```




## Configuration Fluent API
Alternatively to the properties/methods mentioned before, you can configure the library using a convenient [Fluent API](http://martinfowler.com/bliki/FluentInterface.html) provided by the method `Audit.Core.Configuration.Setup()`, this is the most straightforward way to configure the library.

For example, to set the FileLog Provider with its default settings using a Manual creation policy:
```c#
Audit.Core.Configuration.Setup()
    .UseFileLogProvider()
    .WithCreationPolicy(EventCreationPolicy.Manual);
```

## Configuration examples

##### File log provider with dynamic directory path and filename:
```c#
Audit.Core.Configuration.Setup()
    .UseFileLogProvider(config => config
        .DirectoryBuilder(_ => $@"C:\Logs\{DateTime.Now:yyyy-MM-dd}")
        .FilenameBuilder(auditEvent => $"{auditEvent.Environment.UserName}_{DateTime.Now.Ticks}.json"));
```

##### File log provider with an InsertOnStart-ReplaceOnEnd creation policy, and a global custom field set in a custom action:
```c#
Audit.Core.Configuration.Setup()
    .UseFileLogProvider(config => config
        .FilenamePrefix("Event_")
        .Directory(@"C:\AuditLogs\1"))
    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
    .WithAction(x => x.OnScopeCreated(scope => scope.SetCustomField("ApplicationId", "MyApplication")));
```


##### Event log provider with an InsertOnEnd creation policy:
```c#
Audit.Core.Configuration.Setup()
    .UseEventLogProvider(config => config
        .SourcePath("My Audited Application")
        .LogName("Application"))
    .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
```

##### Dynamic provider to log to the console:
```c#
Audit.Core.Configuration.Setup()
    .UseDynamicProvider(config => config
        .OnInsert(ev => Console.WriteLine("{0}: {1}->{2}", ev.StartDate, ev.Environment.UserName, ev.EventType)));
```

-----------

# Extensions

The following packages are extensions to log interactions with different systems such as MVC, WebApi, WCF and Entity Framework: 

<a></a> | Package | Description 
------------ | ------------------- | ------------------
<img src="https://i.imgur.com/hVMM5WF.png" alt="icon" width="90"/> | **[Audit.DynamicProxy](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.DynamicProxy/README.md)** | Generate detailed audit logs for **any class** without changing its code by using a proxy.
<img src="https://i.imgur.com/wdVHFoc.png" alt="icon" width="90"/> | **[Audit.EntityFramework](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md)** | Generate detailed audit logs for saving operations on **Entity Framework**, by inheriting from a provided `DbContext` or `IdentityDbContext`.  Includes support for EF 6 and EF 7 (EF Core).
<img src="https://i.imgur.com/Fn4thn0.png" alt="icon" width="90"/> | **[Audit.FileSystem](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.FileSystem/README.md)** | Generate audit logs by intercepting **file system** events via FileSystemWatcher.
<img src="https://i.imgur.com/8lV5DRk.png" alt="icon" width="90" /> | **[Audit.HttpClient](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/README.md)** | Generate detailed client-side audit logs for **HttpClient** REST calls, by configuring a provided message handler.
<img src="https://i.imgur.com/ap6CeoG.png" alt="icon" width="90"/> | **[Audit.MVC](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/README.md)** | Generate detailed audit logs by decorating **MVC** Actions and Controllers with an action filter attribute. Includes support for ASP.NET Core MVC.
<img src="https://i.imgur.com/GB2e52X.jpg" alt="icon" width="90"/> | **[Audit.SignalR](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.SignalR/README.md)** | Generate audit logs for **SignalR** invocations by intercepting the hub processing
<img src="https://i.imgur.com/p6knit4.png" alt="icon" width="90" /> | **[Audit.WCF](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/README.md)** | Generate detailed server-side audit logs for **Windows Communication Foundation (WCF)** service calls, by configuring a provided behavior.
<img src="https://i.imgur.com/9go2b0f.png" alt="icon" width="90"/> | **[Audit.WebApi](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/README.md)** | Generate detailed audit logs by decorating **Web API** Methods and Controllers with an action filter attribute, or by using a middleware. Includes support for ASP.NET Core.

# Storage providers

Apart from the _FileLog_, _EventLog_ and _Dynamic_ event storage providers, there are others included in different packages:

<a></a> | Package | Description
------------- | ------------------- | ------------------
<img src="https://i.imgur.com/yeBZZiP.png" alt="icon" width="80"/> | **[Audit.NET.AzureCosmos](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureCosmos/README.md)** | Store the events in an **Azure Cosmos DB** container, in JSON format.
<img src="https://i.imgur.com/ouaw5CX.png" alt="icon" width="80"/> | **[Audit.NET.AzureStorage](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureStorage/README.md)** | Store the events in an **Azure Blob Storage** container or an **Azure Table**.
<img src="https://i.imgur.com/kIGe4Z5.png" alt="icon" width="80"/> | **[Audit.NET.DynamoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.DynamoDB/README.md)** | Store the audit events in Amazon DynamoDB tables.
<img src="https://i.imgur.com/PbeWVKz.png" alt="icon" width="80"/> | **[Audit.NET.Elasticsearch](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.ElasticSearch/README.md)** | Store the audit events in Elasticsearch indices.
<img src="https://i.imgur.com/qxbK98k.png" alt="icon" width="80"/> | **[Audit.NET.log4net](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.log4net/README.md)** | Store the audit events using Apache log4net™.
<img src="https://i.imgur.com/1nMVLQo.png" alt="icon" width="80"/> | **[Audit.NET.MongoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MongoDB/README.md)** | Store the events in a **Mongo DB** Collection, in BSON format.
<img src="https://i.imgur.com/NHRBp86.png" alt="icon" width="80"/> | **[Audit.NET.MySql](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MySql/README.md)** | Store the events as rows in **MySQL** database, in JSON format.
<img src="https://i.imgur.com/qxbK98k.png" alt="icon" width="80"/> | **[Audit.NET.NLog](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.NLog/README.md)** | Store the audit events using NLog™.
<img src="https://i.imgur.com/ZxbDxAU.png" alt="icon" width="80"/> | **[Audit.NET.PostgreSql](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.PostgreSql/README.md)** | Store the events as rows in a **PostgreSQL** database, in JSON format.
<img src="https://i.imgur.com/abs6duI.png" alt="icon" width="80"/> | **[Audit.NET.Redis](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Redis/README.md)** | Store Audit Logs in a **Redis** database as String, List, Hash, Sorted Set or publishing to a Redis PubSub channel.
<img src="https://i.imgur.com/lmzs1gw.png" alt="icon" width="80"/> | **[Audit.NET.SqlServer](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.SqlServer/README.md)** | Store the events as rows in a **SQL** Table, in JSON format.
<img src="https://i.imgur.com/FItQD9n.png" alt="icon" width="80"/> | **[Audit.NET.Udp](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Udp/README.md)** | Send Audit Logs as **UDP datagrams** to a network.
<img src="https://i.imgur.com/C0Xu3iX.png" alt="icon" width="80"/> | **[Audit.NET.AmazonQLDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AmazonQLDB/README.md)** | Store the audit events in **Amazon QLDB** (Quantum Ledger Database).
<img src="https://i.imgur.com/C0Xu3iX.png" alt="icon" width="80"/> | **[Audit.NET.Kafka](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.Kafka/README.md)** | Stream the audit events to an **Apache Kafka** server.

# Change Log

For detailed information on changes in new release refer to the [change log](https://github.com/thepirat000/Audit.NET/blob/master/CHANGELOG.md).

# Contribute

If you like this project please contribute in any of the following ways:

- [Star](https://github.com/thepirat000/Audit.NET/stargazers) this project on GitHub.
- Request a new feature or expose any bug you found by creating a [new issue](https://github.com/thepirat000/Audit.NET/issues/new).
- Ask any questions about the library on [StackOverflow](http://stackoverflow.com/questions/ask?tags=Audit.NET).
- Subscribe to and use the [Gitter Audit.NET channel](https://gitter.im/Audit-NET/Lobby).
- Support the project by [becoming a Backer](https://opencollective.com/auditnet):
[![Backer](https://opencollective.com/auditnet/tiers/backer.svg?avatarHeight=36&width=600)](https://opencollective.com/auditnet)     
- Spread the word by blogging about it, or sharing it on social networks:
  <p class="share-buttons">
    <a href="https://www.facebook.com/sharer/sharer.php?u=https://nuget.org/packages/Audit.NET/&amp;t=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Share this package on Facebook" src="https://nuget.org/Content/gallery/img/facebook.svg" / >
    </a>
    <a href="https://twitter.com/intent/tweet?url=https://nuget.org/packages/Audit.NET/&amp;text=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Tweet this package" src="https://nuget.org/Content/gallery/img/twitter.svg" />
    </a>
  </p>
- Make a donation via PayPal:
[![paypal](https://img.shields.io/badge/donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=thepirat000%40hotmail.com&currency_code=USD&source=url)
