#Audit.WebApi

**ASP.NET Web API Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for Web API calls.

Audit.WebApi provides the infrastructure to log interactions with ASP.NET Web API Controllers. It can record action method calls with caller info and arguments.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.WebApi/)**
```
PM> Install-Package Audit.WebApi
```

##Usage

Decorate with an `AuditApiAttribute` the Web Api methods you want to audit. 

For example:

```c#
public class UsersController : ApiController
{
    [AuditApi]
    public IEnumerable<ApplicationUser> Get()
    {
      //...
    }

    [AuditApi(EventTypeName = "GetUser", IncludeHeaders = true, IncludeResponseBody = true, IncludeModelState = true)]
    public IHttpActionResult Get(string id)
    {
     //...
    }
}
```

You can also decorate the controller class with the `AuditApiAttribute` so it will apply to all the actions, for example:
```c#
[AuditApi(EventTypeName = "{action}/{controller}", IncludeHeaders = true, IncludeResponseBody = true, IncludeModelState = true)]
public class UsersController : ApiController
{
    public IEnumerable<ApplicationUser> Get()
    {
      //...
    }

    public IHttpActionResult Get(string id)
    {
     //...
    }
}
```


##Configuration

The `AuditApiAttribute` can be configured with the following properties:
- **EventTypeName**: A string that identifies the event type. Can contain the following placeholders: 
 - {controller}: replaced with the controller name.
 - {action}: replaced with the action method name.
 - {verb}: replaced with the HTTP verb used (GET, POST, etc).
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not.
- **IncludeResponseBody**: Boolean to indicate whether to include response body or not.
- **IncludeModelState**: Boolean to indicate whether to include the Model State info or not.

 
To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output-configuration).

##Output

Audit.WebApi output includes:

- Execution time and duration
- Authenticated username
- Client IP address
- Form Variables, Action Arguments
- Http Headers
- Model State
- Exception details
- Response Status and Body

With this information, you can not just know who did the operation, but also measure performance, observe exceptions thrown or get statistics about usage of your API.

###Output Sample

```javascript
{
	"EventType": "Users/Get (GET)",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.WebApi.AuditApiAttribute.OnActionExecuting()",
		"AssemblyName": "Audit.WebApi, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null",
		"Exception": null,
		"Culture": "en-GB"
	},
	"StartDate": "2016-08-26T15:35:24.5396857-05:00",
	"EndDate": "2016-08-26T15:35:25.6622619-05:00",
	"Duration": 1123,
	"Action": {
		"HttpMethod": "GET",
		"ControllerName": "Users",
		"ActionName": "Get",
		"ActionParameters": {
			"id": "federico@mycompnay.com"
		},
		"UserName": "",
		"RequestUrl": "http://localhost:37341/api/Users/federico@mycompany.com/",
		"IpAddress": "127.0.0.1",
		"ResponseStatus": "OK",
		"ResponseStatusCode": 200,
		"ResponseBody": {
			"Type": "ApplicationUser",
			"Value": {
				"Roles": [],
				"Email": "federicoc@herbalife.com",
				"PasswordHash": "AEkE/D4JxB1bscotMjgtYZMmnsAA2KCsfRXAUQ226/hi39lhrfRi9PIJEqWlqjBdPg==",
				"PhoneNumber": null,
				"LockoutEnabled": true,
				"Id": "bc6828e4-6230-421e-8c55-fd80286998cd",
				"UserName": "federico@mycompany.com"
			}
		},
		"Headers": {
			"Connection": "Keep-Alive",
			"Accept": "text/html, application/xhtml+xml, image/jxr, */*",
			"Accept-Encoding": "gzip, deflate",
			"Accept-Language": "en-GB",
			"Host": "localhost:37341",
			"User-Agent": "Mozilla/5.0, (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0), like, Gecko"
		},		
		"ModelStateValid": true
	}
}
```












