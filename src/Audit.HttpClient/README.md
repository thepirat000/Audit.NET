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

To enable the audit log for an instance of a `HttpClient`, you have to set an `AuditHttpClientHandler` as the message handler when creating the `HttpClient`. 
This can be done by using the convenient factory provided by `ClientFactory.Create` method.

This will return an _audit-enabled_ instance of `HttpClient`. Each method call on the `HttpClient` instance will generate an Audit Event. 
For example:

```c#
using Audit.Http;

// Create an HttpClient that logs the GET calls
private HttpClient _httpClient = ClientFactory.Create(_ => _
    .IncludeRequestBody()
    .IncludeResponseHeaders()
    .FilterByRequest(req => req.Method.Method == "GET"));

// Use the HttpClient as you normally do
await _httpClient.PostAsync(url, content);
// ...
await _httpClient.GetAsync(url);
```

The `ClientFactory.Create` method is just a shortcut to create a new `HttpClient` with a custom `AuditHttpClientHandler` as its message handler.

You can create the `HttpClient` in some other ways, for example:

```c#
private HttpClient _httpClient = new HttpClient(new AuditHttpClientHandler(_ => _
    .IncludeRequestBody()
    .IncludeResponseHeaders());
```

Or just:

```c#
private HttpClient _httpClient = new HttpClient(new AuditHttpClientHandler()
{
    IncludeRequestBody = true,
    IncludeResponseHeaders = true
});
```

## Configuration

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

## Settings

The `AuditHttpClientHandler` class allows to configure the following settings:

- **RequestFilter / FilterByRequest**: Set a filter function to determine which events to log depending on the request message. By default all events are logged.
- **ResponseFilter / FilterByResponse**: Set a filter function to determine which events to log depending on the response message. By default all events are logged.
- **EventType**: A string that identifies the event type. Default is "\{verb}.\{url}". It can contain the following placeholders: 
  - \{verb}: Replaced by the Http Verb (GET, POST, ...)
  - \{url}: Replaced by the request URL
- **IncludeRequestHeaders**: Specifies whether the HTTP Request headers should be included on the audit output. Default is false.
- **IncludeResponseHeaders**: Specifies whether the HTTP Response headers should be included on the audit output. Default is false.
- **IncludeContentHeaders**: Specifies whether the HTTP Content headers should be included on the audit output. Default is false.
- **IncludeRequestBody**: Specifies whether the HTTP Request body should be included on the audit output. Default is false.
- **IncludeResponseBody**: Specifies whether the HTTP Response body should be included on the audit output. Default is false.
- **CreationPolicy**: Allows to set a specific event creation policy. By default the globally configured data provider is used. See [Audit.NET Event Creation Policy](https://github.com/thepirat000/Audit.NET#event-creation-policy) section for more information.
- **AuditDataProvider**: Allows to set a specific audit data provider. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.

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