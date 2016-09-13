#Audit.WCF

**WCF Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs for Wincows Communication Foundation (WCF) calls.

Audit.Wcf provides the infrastructure to log interactions with WCF Services. It can record service method calls with caller info and arguments.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.Wcf/)**
```
PM> Install-Package Audit.Wcf
```

##Usage

Decorate your WCF service class or methods with `AuditBehaviorAttribute`.

For example:

```c#
[AuditBehavior(EventType = "{contract}.{operation}")]
public class OrderService : IOrderService
{
  public GetOrderResponse GetOrder(GetOrderRequest request)
  {
    ...
  }
}
```

You can also decorate the specific methods you want to audit, for example:
```c#
public class OrderService : IOrderService
{
  [AuditBehavior]
  public GetOrderResponse GetOrder(GetOrderRequest request)
  {
    ...
  }
}
```

If you can't change the service code, you can also enable the audit by adding the `AuditBehavior` extension to your service host configuration file.

For example:

```xml
<configuration>
  <system.serviceModel>
    ...
    <extensions>
      <behaviorExtensions>
	      <add name="auditBehavior" type="Audit.WCF.AuditBehavior, Audit.WCF" />
      </behaviorExtensions>
    </extensions>

    <behaviors>
      <serviceBehaviors>
	      <behavior>
	        <auditBehavior eventType="{contract}.{operation}" />
	      </behavior>
      </serviceBehaviors>
    </behaviors>
    ...
  </system.serviceModel>
</configuration>
```

##Configuration

The `AuditBehavior` attribute or extension can be configured with the following properties:
- **EventTypeName**: A string that identifies the event type. Can contain the following placeholders: 
 - {contract}: Replaced with the contract name (service interface name)
 - {operation}: Replaces with the operation name (service method name)
 
?? pending: property AuditDataProvider ??
 
To configure the output persistence mechanism, use the `Audit.Core.Configuration` class. For more details please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output).

For example:
```c#
Audit.Core.Configuration.Setup()
	.UseFileLogProvider(config => config.Directory(@"C:\Logs"));
```

##Output

Audit.Wcf output includes:

- Execution time and duration
- Authenticated username (identity name)
- Client IP address
- Contract and Operation details
- Method parameters (in and out)
- Response object
- Faults and Exceptions details
- Comments and Custom Fields provided

With this information, you can not just know who did the operation, but also measure performance, observe exceptions thrown or get statistics about usage of your WCF service.

##Output details

The following table describes the Audit.Wcf output fields:

####[AuditWcfEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEvent.cs) 

Describes an audited WCF event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| ContractName | string | Name of the contract (service interface) |
| OperationName | string | Name of the operation (service method) |
| InstanceQualifiedName | string | Assembly qualified type name of the service instance |
| MethodSignature | string | Signature of the audited method |
| Action | string | Action absolute address |
| ReplyAction | string |Reply action absolute address |
| IdentityName | string | Name of the current identity (username) |
| ClientAddress | string | Client address (IP) |
| HostAddress | string | Serice host address |
| Success | boolean | Indicates if the operation completed succesfully |
| Fault | [AuditWcfEventFault](#AuditWcfEventFault) | Fault details when the operation fails |
| Result | Object | The result object value |
| InputParameters | Array of [AuditWcfEventElement](#AuditWcfEventElement) | Input parameters object values |
| OutputParameters | Array of [AuditWcfEventElement](#AuditWcfEventElement) | Output parameters object values |

###[AuditWcfEventFault](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEventFault.cs)

Describes a WCF fault/exception

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| FaultType | string | Fault type (Exception / Fault) |
| Exception | string | Exception details |
| FaultCode | string | The fault code |
| FaultAction | string | The fault action name |
| FaultReason | string | The fault reason |
| FaultDetails | [AuditWcfEventElement](#AuditWcfEventElement) | The detail object related to the fault |

###[AuditWcfEventElement](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEventElement.cs)

Describes an element/object related to the WCF audit event.

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Type | string | The object type name |
| Value | Object | The object value |




