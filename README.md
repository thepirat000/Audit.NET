# Audit.NET
A small framework to audit .NET object changes

Generate an [audit log](https://en.wikipedia.org/wiki/Audit_trail) with evidence for reconstruction and examination of activities that have affected a specific operation or procedure. 

With Audit.NET you can easily generate tracking information about an operation being executed.

###Usage

Surround the operation code you want to audit with a `using` block, indicating the object(s) to track.

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

The library will gather contextual information about the user and the machine, as well as the tracked object's state before and after the operation, and optionally [Comments]() and [Custom Fields]() provided.

It will generate an output (event) for each operation, for example:

```json
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

###Persistence of events

You decide where to save the events by using one of the configurable mechanisms provided:

- [File Log]()
- [Windows Event Log]()
- [Mongo DB]()
- [Sql Server]()
- [Azure Document DB]()

Or inject a custom persistence mechanism, by coding a class that inherits from `AuditDataAccessBase`, for example:

```c#
public class FileDataAccess : AuditDataAccessBase
{
    public override void Save(AuditEvent auditEvent)
    {
        var fileName = Settings.AuditEventTable + "_" + DateTime.Now.ToString("yyyyMMddmmssffff") + ".json";
        var fullPath = Path.Combine(Settings.AuditConnectionString ?? "", fileName);
        var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
        File.WriteAllText(fullPath, json);
    }

    public override bool TestConnection()
    {
        return true;
    }
}
```


```c#
Order order = Db.GetOrder(orderId);
using (var audit = AuditScope.Create("Order:Update", () => order, orderId))
{
    audit.SetCustomField("Items", order.OrderItems);
    order.Status = 4;
    order = Db.OrderUpdate(order);
    audit.Comment("Status Updated to Submitted");
}
```

With `SetCustomField()` you can store an object state as a custom field. The object state is serialized by this method, so further changes to the object will not be reflected on the field value.

With `Comment()` you can add textual comments to the scope.



