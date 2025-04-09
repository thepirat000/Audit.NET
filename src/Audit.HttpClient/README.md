# Audit.HttpClient

**HttpClient audit extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs by intercepting `HttpClient` REST calls.
Audit.HttpClient provides the infrastructure to create audit logs for an instance of `HttpClient` class.

It relies on a [message handler](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclienthandler?view=netframework-4.8) to incercept the calls to `HttpClient` methods.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.HttpClient
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.HttpClient.svg?style=flat)](https://www.nuget.org/packages/Audit.HttpClient/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.HttpClient.svg)](https://www.nuget.org/packages/Audit.HttpClient/)

## Usage

To enable the audit log for `HttpClient`, you have to set an `AuditHttpClientHandler` as a message handler for the `HttpClient` instance being audited. 

This can be done in different ways:

- Call the factory provided by `Audit.Http.ClientFactory.Create()` method to get a new _audit-enabled_ instance of `HttpClient`:

```c#
var httpClient = Audit.Http.ClientFactory.Create(_ => _
    .IncludeRequestBody()
    .IncludeResponseHeaders()
    .FilterByRequest(req => req.Method.Method == "GET"));
```

> The `ClientFactory.Create` method is just a shortcut to create a new `HttpClient` with a custom `AuditHttpClientHandler` as its message handler.

- If you use ASP .NET dependency injection / `HttpClientFactory`, you can add the message handler with the extension method `AddAuditHandler()` on your startup:

```c#
using Audit.Http;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient("GitHub", c =>
        {
            c.BaseAddress = new Uri("https://api.github.com/");
        })
        .AddAuditHandler(audit => audit
            .IncludeRequestBody()
            .IncludeResponseHeaders());
    }
}
```

> Note: `AddAuditHandler(config)` is a shortcut for `AddHttpMessageHandler(() => new AuditHttpClientHandler(config))`

- You can also create an audited `HttpClient` passing the handler to its constructor: 

```c#
var httpClient = new HttpClient(new AuditHttpClientHandler(_ => _
    .IncludeRequestBody()
    .IncludeResponseHeaders());
```

Each method call on the audited `HttpClient` instances will generate an Audit Event. 


## Configuration

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

## Settings

The `AuditHttpClientHandler` class allows to configure the following settings:

- **RequestFilter / FilterByRequest**: Set a filter function to determine which events to log depending on the request message. By default all events are logged.
- **ResponseFilter / FilterByResponse**: Set a filter function to determine which events to log depending on the response message. By default all events are logged.
- **EventType**: A string that identifies the event type. Default is "\{verb} \{url}". It can contain the following placeholders: 
  - \{verb}: Replaced by the Http Verb (GET, POST, ...)
  - \{url}: Replaced by the request URL
- **IncludeRequestHeaders**: Specifies whether the HTTP Request headers should be included on the audit output. Default is false.
- **IncludeResponseHeaders**: Specifies whether the HTTP Response headers should be included on the audit output. Default is false.
- **IncludeContentHeaders**: Specifies whether the HTTP Content headers should be included on the audit output. Default is false.
- **IncludeRequestBody**: Specifies whether the HTTP Request body should be included on the audit output. Default is false.
- **IncludeResponseBody**: Specifies whether the HTTP Response body should be included on the audit output. Default is false.
- **IncludeOptions**: Specifies which [HTTP Request Options](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.options?view=net-8.0) should be included in the audit output. Useful to add contextual information to the HTTP Audit Event. By default, the options are not included.
- **CreationPolicy**: Allows to set a specific event creation policy. By default the globally configured creation policy is used. See [Audit.NET Event Creation Policy](https://github.com/thepirat000/Audit.NET#event-creation-policy) section for more information.
- **AuditDataProvider**: Allows to set a specific audit data provider. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.
- **AuditScopeFactory**: Allows to set a specific audit scope factory. By default the globally configured [`AuditScopeFactory`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditScopeFactory.cs) is used. 

## Output Details

The following table describes the Audit.HttpClient output fields:

### [HttpAction](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/HttpAction.cs)

Describes an operation call event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Method | string | HTTP rest method |
| Url | string | Request URL |
| Version | string | Http client version |
| Exception | string | Exception details when an exception is thrown |
| Request | Request | Request audit information  |
| Response | Response | Response audit information |

### [Request](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/Request.cs)

Describes a HTTP request 

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| QueryString | string | Query string portion of the request URL |
| Scheme | string | Request scheme (http, https) |
| Path | string | Path portion of the request URL |
| Headers | Dictionary | Request headers |
| Content | Content | Request content |

### [Response](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/Response.cs)

Describes a HTTP response 

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| StatusCode | int | Response HTTP status code |
| Status | string | String representation of the response status code |
| Reason | string | Response status reason phrase |
| IsSuccess | bool | Indicates if the HTTP response was successful. |
| Headers | Dictionary | Request headers |
| Content | Content | Request content |

### [Content](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.HttpClient/Content.cs)

Describes the content of a HTTP request or response

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Body | string | Response body content decoded as a string |
| Headers | Dictionary | Content headers |


## Output Sample

```javascript
{
	"EventType": "GET http://google.com/doesnotexists",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "FEDE",
		"DomainName": "FEDE",
		"Culture": "en-US"
	},
	"StartDate": "2019-05-21T23:18:38.3251378Z",
	"EndDate": "2019-05-21T23:18:40.4623427Z",
	"Duration": 2137,
	"Action": {
		"Method": "GET",
		"Url": "http://google.com/doesnotexists",
		"Version": "1.1",
		"Request": {
			"QueryString": "",
			"Scheme": "http",
			"Path": "/doesnotexists",
			"Headers": {
				
			}
		},
		"Response": {
			"Headers": {
				"Date": "Tue, 21 May 2019 23:18:19 GMT",
				"Referrer-Policy": "no-referrer"
			},
			"Content": {
				"Body": "<!DOCTYPE html>\n<html lang=en>\n  <meta charset=utf-8>\n ....",
				"Headers": {
					"Content-Length": "1574",
					"Content-Type": "text/html; charset=UTF-8"
				}
			},
			"StatusCode": 404,
			"Status": "NotFound",
			"Reason": "Not Found",
			"IsSuccess": false
		}
	}
}
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)