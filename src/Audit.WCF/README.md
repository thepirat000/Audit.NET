# Audit.WCF

**WCF Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs for Windows Communication Foundation (WCF) service calls.

Audit.Wcf provides the server-side infrastructure to log interactions with WCF services. It can record service method calls with caller info and arguments.

## Install

**NuGet Package** [![NuGet Status](https://img.shields.io/nuget/v/Audit.Wcf.svg?style=flat)](https://www.nuget.org/packages/Audit.Wcf/)

```
PM> Install-Package Audit.Wcf
```

## Usage

Decorate your WCF service class or methods with the `Audit.WCF.AuditBehavior` attribute.

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

If you can't (or do not want) to change the service code, you can also enable the audit mechanism by adding the `AuditBehavior` extension to your service host configuration file.

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

## Configuration

The `AuditBehavior` attribute or extension can be configured with the following properties:
- **EventTypeName**: A string that identifies the event type. Can contain the following placeholders: 
  - \{contract}: Replaced with the contract name (service interface name)
  - \{operation}: Replaces with the operation name (service method name)

To globally configure the output persistence mechanism, use the `Audit.Core.Configuration` class. For more details please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output).

For example:
```c#
Audit.Core.Configuration.Setup()
	.UseFileLogProvider(config => config.Directory(@"C:\Logs"));
```

This should be done prior to the AuditScope creation, i.e. during application startup.

If you want to configure an Audit Data Provider per service instance, you can add a public instance property named `AuditDataProvider` to your service class and make it return the provider you want, for example:
```c#
[AuditBehavior]
public class OrderService : IOrderService
{
    public AuditDataProvider AuditDataProvider
    {
        get
        {
            return new Audit.Core.Providers.FileDataProvider()
            {
                DirectoryPath = @"C:\Logs"
            };
        }
    }
}
```

The library will automatically detect the property and use the given data provider for that service instance.

## Output

Audit.Wcf output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Authenticated username (identity name)
- Client IP address
- Contract and Operation details
- Method parameters (in and out)
- Response object
- Faults and Exceptions details
- [Comments and Custom Fields](#custom-fields-and-comments) provided

With this information, you can not just know who did the operation, but also measure performance, observe exceptions thrown or get statistics about usage of your WCF service.

## Output details

The following table describes the Audit.Wcf output fields:

- <h3>[AuditWcfEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEvent.cs)</h3>

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

- <h3>[AuditWcfEventFault](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEventFault.cs)</h3>

Describes a WCF fault/exception

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| FaultType | string | Fault type (Exception / Fault) |
| Exception | string | Exception details |
| FaultCode | string | The fault code |
| FaultAction | string | The fault action name |
| FaultReason | string | The fault reason |
| FaultDetails | [AuditWcfEventElement](#AuditWcfEventElement) | The detail object related to the fault |

- <h3>[AuditWcfEventElement](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/AuditWcfEventElement.cs)</h3>

Describes an element/object related to the WCF audit event.

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Type | string | The object type name |
| Value | Object | The object value |

## Customization

You can access the `AuditScope` object for customization from the audited methods, by the static property `Audit.WCF.AuditBehavior.CurrentAuditScope`. 

For example:
```c#
using Audit.WCF;

[AuditBehavior]
public class OrderService : IOrderService
{
    public GetOrderResponse GetOrder(GetOrderRequest request)
    {
        AuditBehavior.CurrentAuditScope.Comment("some comment");
        AuditBehavior.CurrentAuditScope.SetCustomField("User", MySession.CurrentUser);
        ...
    }
}
```

See [Audit.NET](https://github.com/thepirat000/Audit.NET) documentation about [Custom Field and Comments](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) for more information.

### Output Sample

```javascript
{
	"EventType": "IOrderService.GetOrder",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.WCF.AuditOperationInvoker.Invoke()",
		"AssemblyName": "Audit.WCF, Version=4.1.0.0, Culture=neutral, PublicKeyToken=null",
		"Exception": null,
		"Culture": "en-GB"
	},
	"StartDate": "2016-09-13T01:31:58.6843094-05:00",
	"EndDate": "2016-09-13T01:31:58.6858324-05:00",
	"Duration": 2,
	"WcfEvent": {
		"ContractName": "IOrderService",
		"OperationName": "GetOrder",
		"InstanceQualifiedName": "WCF_IIS.OrderService, WCF_IIS, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
		"MethodSignature": "WCF_IIS.GetOrderResponse GetOrder(WCF_IIS.GetOrderRequest)",
		"Action": "http://tempuri.org/IOrderService/GetOrder",
		"ReplyAction": "http://tempuri.org/IOrderService/GetOrderResponse",
		"ClientAddress": "::1",
		"HostAddress": "http://localhost:8733/Design_Time_Addresses/WCF_IIS/OrderService/",
		"InputParameters": [{
			"Type": "GetOrderRequest",
			"Value": {
				"OrderId": 123
			}
		}],
		"Success": true,
		"Result": {
			"Type": "GetOrderResponse",
			"Value": {
				"Success": true,
				"Errors": null,
				"Order": {
					"OrderId": 123,
					"CustomerName": "customer",
					"Total": 10.0
				}
			}
		},
		"OutputParameters": []
	}
}
```
