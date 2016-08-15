# Audit.NET
A small framework to audit an operation being executed.

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected specific operations or procedures. 

With Audit.NET you can easily generate tracking information about an operation being executed.

##Usage

Surround the operation code you want to audit with a `using` block, indicating the object to track.

Suppose you have the following code to update an order status:

```c#
Order order = Db.GetOrder(orderId);
order.Status = 4;
order = Db.OrderUpdate(order);
```

To audit the operation tracking the order object, you can add the following `using` statement:
```c#
Order order = Db.GetOrder(orderId);
using (AuditScope.Create("Order:Update", () => order, orderId))
{
    order.Status = 4;
    order = Db.OrderUpdate(order);
}
```

The first parameter of the `Create` method is an event type name. The second is the delegate to obtain the object to track, and the third is a string that identifies the object to track.

The library will gather contextual information about the user and the machine, as well as the tracked object's state before and after the operation, and optionally [Comments and Custom Fields]() provided.

It will generate an output (event) for each operation, for example (JSON):

```javascript
{
  "EventType": "Order:Update",
  "ReferenceId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
  "UserName": "Federico",
  "MachineName": "HP1234",
  "DomainName": "MyCompany",
  "CreatedDate": "2016-08-13T21:18:02.5708415-05:00",
  "CommitDate": "2016-08-13T21:18:02.5718424-05:00",
  "CallingMethodName": "Audit.UnitTest.AuditTests.TestUpdate()",
  "Target": {
    "Type": "Order",
    "Old": {
      "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
      "Status": 2
    },
    "New": {
      "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
      "Status": 4
    }
  }
}
```

##Custom Fields and Comments

The `AuditScope` object provides two methods to extend the event output.

With `SetCustomField()` you can store any object state as a custom field. (The object is serialized upon this method, so further changes to the object are not reflected on the field value).

With `Comment()` you can add textual comments to the scope.

```c#
Order order = Db.GetOrder(orderId);
using (var audit = AuditScope.Create("Order:Update", () => order, orderId))
{
    audit.SetCustomField("ItemsCatalog", ItemsList);
    order.Status = 4;
    order = Db.OrderUpdate(order);
    audit.Comment("Status Updated to Submitted");
}
```
An example of the output of the previous example would be:

```javascript
{
  "EventType": "Order:Update",
  "ReferenceId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
  "UserName": "Federico",
  "MachineName": "HP1234",
  "DomainName": "MyCompany",
  "Target": {
    "Type": "Order",
    "Old": {
      "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
      "Status": 2,
    },
    "New": {
      "OrderId": "39dc0d86-d5fc-4d2e-b918-fb1a97710c99",
      "Status": 4,
    }
  },
  "Comments": [
    "Status Updated to Submitted"
  ],
  "CreatedDate": "2016-08-13T21:18:02.5708415-05:00",
  "CommitDate": "2016-08-13T21:18:02.5718424-05:00",
  "CallingMethodName": "Audit.UnitTest.AuditTests.TestUpdate()",
  "ItemsCatalog": [
    {
      "Sku": "1002",
      "Description": "Some product description"
    }
    ]
}
```

##Persistence of events

You decide where to save the events by [configuring]() one of the mechanisms provided:

- [File Log]()
- [Windows Event Log]()
- [Mongo DB]()
- [Sql Server]()
- [Azure Document DB]()

Or by injecting a custom persistence mechanism, creating a class that inherits from `AuditDataAccessBase`, for example:

```c#
public class NaiveFileDataAccess : AuditDataAccessBase
{
    public override void Save(AuditEvent auditEvent)
    {
        // AuditEvent has a ToJson method
        string json = auditEvent.ToJson();
        File.AppendAllText(path, json);
    }
}
```

##Configuration

The library configuration can be provided in the AppSettings section of your `web/app.config` or by code with the `Global.Settings` object properties.

The most important setting is the `AuditDataAccessType` where you indicate the [Assembly Qualified Type Name](https://msdn.microsoft.com/en-us/library/system.type.assemblyqualifiedname(v=vs.110).aspx#Anchor_1) of the data access class; the class that will handle the output.

For example:

```xml
<configuration>
  <appSettings>
      <add key="AuditDataAccessType" value="MyNamespace.NaiveFileDataAccess,MyAssembly" /> 
  </appSettings>
</configuration>
```

Or by code:

```c#
Global.Settings.AuditDataAccessType = typeof(NaiveFileDataAccess).AssemblyQualifiedName;
```

###Settings:

Key: **AuditDataAccessType**

Description: Indicate the [Assembly Qualified Type Name](https://msdn.microsoft.com/en-us/library/system.type.assemblyqualifiedname(v=vs.110).aspx#Anchor_1) of the data access class to use.

To use one of the data access classes provided with the library, you can use the following values for _AuditDataAccessType_:

- For MongoDB: "Audit.MongoDB.MongoDataAccess,Audit.NET.MongoDB"
- For Azure DocumentDB: "Audit.AzureDocumentDB.AzureDbDataAccess,Audit.NET.AzureDocumentDB"
- For SQL Server: "Audit.SqlServer.SqlDataAccess,Audit.NET.SqlServer"
- For SQL Server: "Audit.SqlServer.SqlDataAccess,Audit.NET.SqlServer"



Key: **AuditValidateDatabaseConnection** 
Values: _True|False_
Description: To indicate if the library should test the data connection when creating a scope.

**AuditConnectionString**

When using

AuditDatabase
AuditEventTable
AuditAuthKey








