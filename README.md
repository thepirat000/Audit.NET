# Audit.NET
A small framework to audit executing operations in .NET.

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected specific operations or procedures. 

With Audit.NET you can easily generate tracking information about an operation being executed.

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
using (AuditScope.Create("Order:Update", () => order, orderId))
{
    order.Status = -1;
    order.OrderItems = null;
    order = Db.OrderUpdate(order);
}
```

The first parameter of the `Create` method is an event type name. The second is the delegate to obtain the object to track, and the third is a string that identifies the object to track.

The library will gather contextual information about the user and the machine, as well as the tracked object's state before and after the operation, and optionally [Comments and Custom Fields](#custom-fields-and-comments) provided.

It will generate and store an output (event) for each operation.

An example of the output in JSON:

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
  "CreatedDate": "2016-08-13T21:18:02.5708415-05:00",
  "CommitDate": "2016-08-13T21:18:02.5718424-05:00",
  "Target": {
    "Type": "Order",
    "Old": {
      "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
      "Status": 2,
      "OrderItems": [
        {
          "Sku": "1002",
          "Quantity": 3.0
        }
      ]    
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
using (var audit = AuditScope.Create("Order:Update", () => order, orderId))
{
    audit.SetCustomField("ItemsCatalog", ItemsList);
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
  "Comments": [
    "Status Updated to Cancelled"
  ],
  "CreatedDate": "2016-08-13T21:18:02.5708415-05:00",
  "CommitDate": "2016-08-13T21:18:02.5718424-05:00",
  "ItemsCatalog": [
    {
      "Sku": "1002",
      "Description": "Some product description"
    }
  ]
}
```

##Event output

You decide what to do with the events by configuring one of the mechanisms provided (such as File or EventLog), or by injecting your own persistence mechanism, creating a class that inherits from `AuditDataProvider`, for example:

```c#
public class MyFileDataProvider : AuditDataProvider
{
    public override void WriteEvent(AuditEvent auditEvent)
    {
        // Here you can provide custom fields for all the scopes
        auditEvent.CustomFields["OSVersion"] = Environment.OSVersion.VersionString;
        // AuditEvent provides a ToJson() method
        string json = auditEvent.ToJson();  
        // Append the json representation of the event to a text file
        File.AppendAllText("audit.json", json);
    }
}
```

##Configuration

Call the static `AuditConfiguration.SetDataProvider` method to set the data provider. The data provider should be set prior to the `AuditScope` creation, i.e. during application startup.

For example, to set your own provider:
```c#
AuditConfiguration.SetDataProvider(new MyFileDataProvider());
```

Initialization example to use the Event Log provider (save the events to the Windows Event Log):
```c#
AuditConfiguration.SetDataProvider(new EventLogDataProvider()
{
    SourcePath = "My Audited Application",
    LogName = "Application",
    MachineName = "."
});
```

Initialization example to use the File Log provider (save the events to files):
```c#
AuditConfiguration.SetDataProvider(new FileDataProvider()
{
    FilenamePrefix = "Event_",
    DirectoryPath = @"C:\AuditLogs\1"
});
```

##More providers

Apart from the _File_ and _EventLog_ providers, there are other providers included in different packages:

**[Sql Server](https://github.com/thepirat000/Audit.NET/blob/master/README.SqlServer.md#auditnetsqlserver)**
Store the events as rows in a SQL Table, in JSON format. 

**[Mongo DB](https://github.com/thepirat000/Audit.NET/blob/master/README.MongoDB.md#auditnetmongodb)**
Store the events in a Mongo DB Collection, in BSON format.

**[Azure Document DB]()**
Store the events in an Azure Document DB Collection, in JSON format.




