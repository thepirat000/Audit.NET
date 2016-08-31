# Audit.NET
An extensible framework to audit executing operations in .NET including support for .NET 4.5 / NetCore 1.0 / NetStandard 1.3.

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected specific operations or procedures. 

With Audit.NET you can generate tracking information about operations being executed. It will automatically log environmental information such as the caller user id, machine name, method name, exceptions, including the execution time and duration, exposing an extensible mechanism in which you can provide extra information or implement your persistence mechanism for the audit logs.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET/)**
```
PM> Install-Package Audit.NET
```

##Usage

Surround the operation code you want to audit with a `using` block, indicating the object to track.

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

The first parameter of the `Create` method is an event type name. The second is the delegate to obtain the object to track.

The library will gather contextual information about the user and the machine, as well as the tracked object's state before and after the operation, and optionally [Comments and Custom Fields](#custom-fields-and-comments) provided.

It will generate and store an output (event) for each operation.

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

##Custom Fields and Comments

The `AuditScope` object provides two methods to extend the event output.

With `SetCustomField()` you can store any object state as a custom field. (The object is serialized upon this method, so further changes to the object are not reflected on the field value).

With `Comment()` you can add textual comments to the scope.

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
	"ReferenceId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
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
	"Comments": ["Status Updated to Cancelled"],
	"StartDate": "2016-08-23T11:34:44.656101-05:00",
	"EndDate": "2016-08-23T11:34:55.1810821-05:00",
	"Duration": 8531
}
```

You can also set the custom fields when creating the `AuditScope`, by passing an anonymous object with the properties you want as extra fields. for example:

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
using (var scope = AuditScope.Create("SomeEvent", () => someTarget, "SomeId"))
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

##Event output configuration

You decide what to do with the events by [configuring](#configuration) one of the mechanisms provided (such as File, EventLog, MongoDB, SQL, DocumentDB), or by injecting your own persistence mechanism, creating a class that inherits from `AuditDataProvider`, for example:

```c#
public class MyFileDataProvider : AuditDataProvider
{
    public override object InsertEvent(AuditEvent auditEvent)
    {
        // AuditEvent provides a ToJson() method
        string json = auditEvent.ToJson();
        // Append the json representation of the event to a text file
        File.AppendAllText("audit.json", json);
        return null;
    }
}
```

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
Call the static `AuditConfiguration.SetDataProvider` method to set the default data provider. The data provider should be set prior to the `AuditScope` creation, i.e. during application startup.

For example, to set your own provider:
```c#
AuditConfiguration.SetDataProvider(new MyFileDataProvider());
```

###Creation Policy
Call the static `AuditConfiguration.SetCreationPolicy` method to set the default creation policy. This should should be set prior to the `AuditScope` creation, i.e. during application startup.

For example, to set the default creation policy to Manual:
```c#
AuditConfiguration.SetCreationPolicy(EventCreationPolicy.Manual);
```

Initialization example to use the File Log provider (save the events to files):
```c#
AuditConfiguration.SetDataProvider(new FileDataProvider()
{
    FilenamePrefix = "Event_",
    DirectoryPath = @"C:\AuditLogs\1"
});
AuditConfiguration.SetCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
```

Initialization example to use the Event Log provider (save the events to the Windows Event Log):
```c#
AuditConfiguration.SetDataProvider(new EventLogDataProvider()
{
    SourcePath = "My Audited Application",
    LogName = "Application",
    MachineName = "."
});
AuditConfiguration.SetCreationPolicy(EventCreationPolicy.InsertOnEnd);
```

##More providers

Apart from the _File_ and _EventLog_ providers, there are other providers included in different packages:

**[Sql Server](https://github.com/thepirat000/Audit.NET/tree/master/Audit.Sql#auditnetsqlserver)**
Store the events as rows in a SQL Table, in JSON format. 

**[Mongo DB](https://github.com/thepirat000/Audit.NET/tree/master/Audit.MongoDB#auditnetmongodb)**
Store the events in a Mongo DB Collection, in BSON format.

**[Azure Document DB](https://github.com/thepirat000/Audit.NET/tree/master/Audit.AzureDocumentDB#auditnetazuredocumentdb)**
Store the events in an Azure Document DB Collection, in JSON format.

##Extensions

**[Audit MVC Actions](https://github.com/thepirat000/Audit.NET/tree/master/Audit.Mvc#auditmvc)**
Decorate ASP.NET MVC Actions and Controllers to generate detailed Audit Logs. 

**[Audit Web API Actions](https://github.com/thepirat000/Audit.NET/tree/master/Audit.WebApi#auditwebapi)**
Decorate ASP.NET Web API Methods and Controllers to generate detailed Audit Logs. 


