# Audit.WCF.Client

**WCF client Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs for Windows Communication Foundation (WCF) service calls on **client-side**.

`Audit.Wcf.Client` provides the client-side infrastructure to log interactions with WCF services. 
It records detailed information of the service method calls by using an [IClientMessageInspector](https://docs.microsoft.com/en-us/dotnet/api/system.servicemodel.dispatcher.iclientmessageinspector?view=dotnet-plat-ext-5.0)
to intercept the request and response messages.

If you are looking for server-side audit, please check the [`Audit.WCF`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF/README.md) library.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.Wcf.Client
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.Wcf.Client.svg?style=flat)](https://www.nuget.org/packages/Audit.Wcf.Client/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.Wcf.Client.svg)](https://www.nuget.org/packages/Audit.Wcf.Client/)

## Usage

A custom [endpoint behavior](https://docs.microsoft.com/en-us/dotnet/framework/wcf/extending/configuring-and-extending-the-runtime-with-behaviors#:~:text=Endpoint%20behaviors%20(IEndpointBehavior%20types)%20enable,client%20and%20service%20applications%2C%20respectively.) is provided 
to enable auditing on the requests and responses of the WCF service calls.

There are two ways to configure the endpoint behavior:

- Adding the audit behavior to the client endpoint on the app.config / web.config of your client application.

	For example:
	```xml
	<?xml version="1.0" encoding="utf-8" ?>
	<configuration>
	  <system.serviceModel>
		<bindings>
		  <basicHttpBinding>
			<binding name="BasicHttpBinding_IService" />
		  </basicHttpBinding>
		</bindings>
		<client>
		  <endpoint address="http://localhost/Service.svc" binding="basicHttpBinding"
			bindingConfiguration="BasicHttpBinding_IService" contract="MyServiceReference.IService" name="BasicHttpBinding_IService"
			behaviorConfiguration="auditBehavior" />
		</client>
		<behaviors>
		 <endpointBehaviors>
		  <behavior name="auditBehavior">
		   <auditBehavior includeResponseHeaders="true" eventType="Catalog:{action}" />
		  </behavior>
		 </endpointBehaviors>
		</behaviors>
		<extensions>
		 <behaviorExtensions>
		  <add name="auditBehavior"
			 type="Audit.Wcf.Client.AuditBehavior, Audit.Wcf.Client, PublicKeyToken=571d6b80b242c87e"/>
		 </behaviorExtensions>
		</extensions>
	  </system.serviceModel>
	</configuration>
	```

	Note you have to include the `behaviorConfiguration` property on the client endpoint, 
the endpoint behavior `auditBehavior`, and the behavior extension pointing the provided type `Audit.Wcf.Client.AuditBehavior` from the assembly `Audit.Wcf.Client`

- Adding the audit behavior on code when creating the channel.

	For example:

	```c#
    public static IService GetServiceProxy()
    {
	  var channelFactory = new ChannelFactory<IService>(new BasicHttpBinding(), new EndpointAddress(URL));
	  channelFactory.Endpoint.EndpointBehaviors.Add(new AuditEndpointBehavior()
	  {
	    EventType = "Catalog:{action}",
	    IncludeResponseHeaders = true
	  });
	  return channelFactory.CreateChannel();
    }
	```

## Configuration


The Audit Behavior can be configured with the following properties:
- **EventType**: A string that identifies the event type. Default is "\{action\}".
- Can contain the following placeholders: 
  - \{action}: Replaced with the action URL 
- **IncludeRequestHeaders**: Boolean value that indicates whether the output should include the request headers. Default is false.
- **IncludeResponseHeaders**: Boolean value that indicates whether the output should include the response headers. Default is false.
- **AuditDataProvider**: Allows to set a specific audit data provider. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.
- **AuditScopeFactory**: Allows to set a specific audit scope factory. By default the globally configured [`AuditScopeFactory`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditScopeFactory.cs) is used. 

### Output mechanism

To globally configure the output mechanism, use the `Audit.Core.Configuration` class to set a Data Provider. 
For more details please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output).

For example:
```c#
Audit.Core.Configuration.Setup()
	.UseFileLogProvider(config => config.Directory(@"C:\Logs"));
```

This should be done prior to the AuditScope creation, i.e. during application startup.


## Output

`Audit.Wcf.Client` output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Request body
- Response body
- Http Status Code
- Request headers (optional)
- Response headers (optional)

## Output details

The following table describes the `Audit.Wcf.Client` output fields:

- ### [WcfClientAction](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WCF.Client/WcfClientAction.cs)

Describes an audited WCF client event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Action | string | The requested action URL |
| RequestBody | string | The request body (XML) |
| RequestHeaders | Dictionary<string, string> | The request HTTP headers (optional) |
| HttpMethod | string | The HTTP method used (POST, PUT) |
| ResponseAction | string | The response action (if any) |
| MessageId | string | The message ID (if any) |
| ResponseStatusCode | HttpStatusCode | The response HTTP status code |
| ResponseBody | string | The response body (XML) |
| ResponseHeaders | Dictionary<string, string> | The response HTTP headers (optional) |
| IsFault | string | Value that indicates whether the message generated any SOAP faults. |


### Output Sample

```javascript
{
    "EventType": "Catalog:http://tempuri.org/IService/GetProductDetails",
    "WcfClientEvent": {
        "Action": "http://tempuri.org/IService/GetProductDetails",
        "RequestBody": "<s:Envelope> ... </s:Envelope>",
        "HttpMethod": "POST",
        "ResponseAction": null,
        "MessageId": null,
        "ResponseStatuscode": 200,
        "ResponseBody": "<s:Envelope> ... </s:Envelope>",
        "ResponseHeaders": {
            "Content-Length": "431",
            "Cache-Control": "private",
            "Content-Type": "text/xml; charset=utf-8",
            "Date": "Thu, 29 Jul 2021 23",
            "Server": "Microsoft-IIS/10.0",
            "X-AspNet-Version": "4.0.30319",
            "X-Powered-By": "ASP.NET"
        },
        "IsFault": false
    },
    "Environment": {
        "UserName": "Federico",
        "MachineName": "DESKTOP",
        "DomainName": "DESKTOP",
        "CallingMethodName": "WcfClient.MyServiceReference.ServiceClient.GetProductDetails()",
        "AssemblyName": "WcfClient, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        "Culture": "en-US"
    },
    "StartDate": "2021-07-29T23:31:10.9631278Z",
    "EndDate": "2021-07-29T23:31:11.0641269Z",
    "Duration": 101
}
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)