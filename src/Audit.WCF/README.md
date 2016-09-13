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
[AuditBehavior]
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

If you can't change the service code, you can enable the audit by setting the `AuditBehavior` extension to your service host config file.
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
 
To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output).

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




----- HERE ------------


