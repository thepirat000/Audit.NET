# Audit.NET
An extensible framework to audit executing operations in .NET including support for .NET Framework 4.5 and NetCore 1.0 (NetStandard 1.3).

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected specific operations or procedures. 

With Audit.NET you can generate tracking information about operations being executed. It will automatically log environmental information such as the caller user id, machine name, method name, exceptions, including the execution time and duration, exposing an extensible mechanism in which you can provide extra information or implement your persistence mechanism for the audit logs.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET/)**
```
PM> Install-Package Audit.NET
```

##Usage

Surround the operation code you want to audit with a `using` block that creates an `AuditScope` indicating the object to track.

Suppose you have the following code to cancel an order:

```c#
Order order = Db.GetOrder(orderId);
order.Status = -1;
order.OrderItems = null;
order = Db.OrderUpdate(order);
```

To audit this operation, tracking the _Order_ object, you can add the following `using` statement:
```c#
Order order = Db.GetOrder(orderId);
using (AuditScope.Create("Order:Update", () => order))
{
    order.Status = -1;
    order.OrderItems = null;
    order = Db.OrderUpdate(order);
}
```

The first parameter of the `Create` method is an _event type name_ intended to identify and group the events. The second is the delegate to obtain the object to track. This object is passed as an `Action<object>` to allow the library to inspect the value at the beggining and at the end of the scope.

It is not mandatory to use a `using` block, but it simplifies the syntax allowing to detect exceptions and calculate the duration by implicitly saving the event on disposal. 

If you are not tracking an specific object change, you can use the `CreateAndSave` shortcut method that creates and saves an event immediately. For example:
```c#
AuditScope.CreateAndSave("Event Type", new { ExtraField = "extra value" });
```

The library will generate an output (`AuditEvent`) for each operation, including:
- Tracked object's state before and after the operation.
- Execution time and duration.
- Enviroment information such as user, machine, domain, locale, etc.
- [Comments and Custom Fields](#custom-fields-and-comments) provided

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

#Output details

The following tables describes the output fields:

###[AuditEvent object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditEvent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **EventType** | string | User-defined string to group the events |
| **Environment** | [**Environment**](#environment-object) | Contains information about the execution environment |
| **StartDate** | DateTime | The date and time when the event has started |
| **EndDate** | DateTime | The date and time when the event has ended |
| **Duration** | integer | The duration of the event in milliseconds |
| **Target** | [**Target**](#target-object) | User-defined tracked object |
| **Comments** | Array of strings | User-defined comments |

###[Environment object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditEventEnvironment.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **UserName** | string | The current logged user name |
| **MachineName** | string | The current machine name |
| **DomainName** | string | The user domain |
| **CallingMethodName** | string | The calling method signature information |
| **Exception** | string | Indicates if an Exception has been detected (NULL if no exception has been thrown) |
| **Culture** | string | The current culture identifier |

###[Target object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditTarget.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Type** | string | The tracked object type name |
| **Old** | Object | The value of the tracked object at the beginning of the event |
| **New** | Object | The value of the tracked object at the end of the event |

##Custom Fields and Comments

The `AuditScope` object provides two methods to extend the event output.

- With `SetCustomField()` you can store any object state as a custom field. (The object is serialized upon this method, so further changes to the object are not reflected on the field value).

- With `Comment()` you can add textual comments to the event.

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
The output of the previous example would be:

```javascript
{
	"EventType": "Order:Update",
	"ReferenceId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",           // <-- Custom Field
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
	"Comments": ["Status Updated to Cancelled"],                     // <-- Comments
	"StartDate": "2016-08-23T11:34:44.656101-05:00",
	"EndDate": "2016-08-23T11:34:55.1810821-05:00",
	"Duration": 8531
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

##Discard option

The `AuditScope` object has a `Discard()` method to allow the user to discard an event under certain conditions.

For example, if you want to avoid saving the audit event if an exception is thrown:

```c#
using (var scope = AuditScope.Create("SomeEvent", () => someTarget))
{
    try
    {
        //some operation
        Critical.Operation();
    }
    catch (Exception ex)
    {
        //If an exception is thown, discard the audit event
        scope.Discard();
    }
}
```

##Event output

You decide what to do with the events by [configuring](#configuration) one of the mechanisms provided (such as File, EventLog, [MongoDB](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.MongoDB#auditnetmongodb), [SQL](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.SqlServer#auditnetsqlserver), [DocumentDB](https://github.com/thepirat000/Audit.NET/tree/master/src/Audit.NET.AzureDocumentDB#auditnetazuredocumentdb)), or by injecting your own persistence mechanism, creating a class that inherits from `AuditDataProvider`, for example:

```c#
public class MyFileDataProvider : AuditDataProvider
{
    public override object InsertEvent(AuditEvent auditEvent)
    {
        // AuditEvent provides a ToJson() method
        string json = auditEvent.ToJson();
        // Write the json representation of the event to a randomly named file
        var fileName = Guid.NewGuid().ToString() + ".json";
        File.WriteAllText(fileName, json);
        return fileName;
    }
    // Update an existing event given the ID and the event
    public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
    {
        // Override an existing event
        var fileName = eventId.ToString();
        File.WriteAllText(fileName, auditEvent.ToJson());
    }
}
```

The `InsertEvent` method should return a unique ID for the event. 
The `ReplaceEvent` method should update an event given its ID, this method is only called when the [Creation Policy](#event-creation-policy) is set to **Manual** or **InsertOnStartReplaceOnEnd**.

##Event Creation Policy

The data providers can be configured to persist the event in different ways:
- **Insert on End:** (**default**)
The audit event is saved when the scope is disposed. 

- **Insert on Start, Replace on End:**
The event (on its initial state) is saved when the scope is created, and then the complete event information is updated when the scope is disposed. 

- **Insert on Start, Insert on End:**
Two versions of the event are saved, the initial when the scope is created, and the final when the scope is disposed.

- **Manual:**
The event saving (insert/replace) should be explicitly invoked by calling the `AuditScope.Save()` method.

You can set the Creation Policy per-scope, for example to explicitly set the Creation Policy to Manual:
```c#
using (var scope = AuditScope.Create("MyEvent", () => target, EventCreationPolicy.Manual))
{
    //...
    scope.Save();
}
```

If you don't provide a Creation Policy, the Default Policy Configured will be used (see next section).

##Configuration

###Data provider
To change the default data provider, set the static property `DataProvider` on `Audit.Core.Configuration` class. This should be done prior to the `AuditScope` creation, i.e. during application startup.

For example, to set your own provider as the default data provider:
```c#
Audit.Core.Configuration.DataProvider = new MyFileDataProvider();
```

###Creation Policy
To change the default creation policy, set the static property `SetCreationPolicy` on `Audit.Core.Configuration` class. This should be done prior to the `AuditScope` creation, i.e. during application startup.

For example, to set the default creation policy to Manual:
```c#
Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.Manual;
```

###Custom Actions
You can configure Custom Actions that are executed for all the Audit Scopes in your application. This allows to globally change the behavior and data, intercepting the scopes after they are created or before they are saved.

Call the static `AddCustomAction()` method on `Audit.Core.Configuration` class to attach a custom action. 

For example, to globally discard the events under centain condition:
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

The `ActionType` indicates when to perform the action. The allowed values are:
- `OnScopeCreated`: When the Audit Scope is being created, before any saving. This is executed once per Audit Scope.
- `OnEventSaving`: When an Audit Scope's Event is about to be saved. 

##Configuration Fluent API
Alternatively to the properties/methods mentioned before,  you can also configure the library using a convenient [Fluent API](http://martinfowler.com/bliki/FluentInterface.html) provided by the method `Audit.Core.Configuration.Setup()`, this is the most straightforward way to configure the library.

For example, to set the FileLog Provider with its default settings using a Manual creation policy:
```c#
Audit.Core.Configuration.Setup()
    .UseFileLogProvider()
    .WithCreationPolicy(EventCreationPolicy.Manual);
```

##Configuration examples
- Use the file log provider with an InsertOnStart-ReplaceOnEnd [creation policy](#creation-policy), and a global _ApplicationId_ [Custom Field](#custom-fields-and-comments):
```c#
Audit.Core.Configuration.DataProvider = new FileDataProvider()
{
    FilenamePrefix = "Event_",
    DirectoryPath = @"C:\AuditLogs\1"
};

Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;

Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope => 
{ 
    scope.SetCustomField("ApplicationId", "MyApplication"); 
});
```
Or by using the fluent API:
```c#
Audit.Core.Configuration.Setup()
    .UseFileLogProvider(config => config
        .FilenamePrefix("Event_")
        .Directory(@"C:\AuditLogs\1"))
    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
    .WithAction(x => x.OnScopeCreated(scope => scope.SetCustomField("ApplicationId", "MyApplication")));
```

- Initialization to use the event log provider with an InsertOnEnd creation policy:
```c#
Audit.Core.Configuration.DataProvider = new EventLogDataProvider()
{
    SourcePath = "My Audited Application",
    LogName = "Application"
};
Audit.Core.Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
```
Or by using the fluent API:
```c#
Audit.Core.Configuration.Setup()
    .UseEventLogProvider(config => config
        .SourcePath("My Audited Application")
        .LogName("Application"))
    .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
```

#Storage providers

Apart from the _File_ and _EventLog_ event storage, there are other providers included in different packages:

###**[Audit.NET.SqlServer](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.SqlServer/README.md)**
Store the events as rows in a SQL Table, in JSON format. 

###**[Audit.NET.MongoDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MongoDB/README.md)**
Store the events in a Mongo DB Collection, in BSON format.

###**[Audit.NET.AzureDocumentDB](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.AzureDocumentDB/README.md)**
Store the events in an Azure Document DB Collection, in JSON format.

#Extensions

The following packages are extensions to log interactions with different systems such as MVC, WebApi and Entity Framework: 

###**[Audit.MVC](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/README.md)**
Generate detailed audit logs by decorating MVC Actions and Controllers with an attribute. Includes support for ASP.NET Core MVC.

###**[Audit.WebApi](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/README.md)**
Generate detailed audit logs by decorating Web API Methods and Controllers with an attribute. Includes support for ASP.NET Core MVC.

###**[Audit.EntityFramework](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/README.md)**
Generate detailed audit logs for CRUD operations on Entity Framework, by inheriting from a provided `DbContext`.  Includes support for EF 6 and EF Core.

