# Audit.WebApi

**ASP.NET Web API Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for Web API calls.

Audit.WebApi provides the infrastructure to log interactions with ASP.NET Web API Controllers. It can record action method calls with caller info and arguments.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.WebApi
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.WebApi.svg?style=flat)](https://www.nuget.org/packages/Audit.WebApi/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.WebApi.svg)](https://www.nuget.org/packages/Audit.WebApi/)

# IMPORTANT NOTE

If your project targets the full .NET framework, but you are using AspNet Core (`Microsoft.AspNetCore.Mvc.*`) 
you should install and reference the `Audit.WebApi.Core` package instead, otherwise it will assume you are targeting
the old generation of ASP.NET:

```
PM> Install-Package Audit.WebApi.Core
```

If your project targets the NET Core framework (NetStandard >= 1.6), there is no difference between using `Audit.WebApi` or `Audit.WebApi.Core` 
since both assumes AspNet Core.

## Usage

Decorate with `AuditApi` attribute the Web Api methods/controllers you want to audit. 

For example:

```c#
public class UsersController : ApiController
{
    [AuditApi]
    public IEnumerable<ApplicationUser> Get()
    {
      //...
    }

    [AuditApi(EventTypeName = "GetUser", 
        IncludeHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
    public IHttpActionResult Get(string id)
    {
     //...
    }
}
```

You can also decorate the controller class with the `AuditApi` attribute so it will apply to all the actions, for example:
```c#
[AuditApi(EventTypeName = "{controller}/{action} ({verb})", IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
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

To apply the audit filter to all the controllers, you can add the `AuditApiAttribute` as a global filter, for example on your StartUp logic:

```c#
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(_ => _
            .Filters.Add(new AuditApiAttribute()));
    }
}
```

## Configuration

### Output

The audit events are stored using a Data Provider. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings

The `AuditApi` attribute can be configured with the following properties:
- **EventTypeName**: A string that identifies the event type. Can contain the following placeholders: 
  - \{controller}: replaced with the controller name.
  - \{action}: replaced with the action method name.
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not. Default is false.
- **IncludeRequestBody**: Boolean to indicate whether to include the request body or not. Default is false. (Check the following note)
- **IncludeResponseBody**: Boolean to indicate whether to include response body or not. Default is false.
- **IncludeModelState**: Boolean to indicate whether to include the Model State info or not. Default is false.
- **SerializeActionParameters**: Boolean to indicate whether the action arguments should be pre-serialized to the audit event. Default is false.
 
To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers).

### NOTE
When **IncludeRequestBody** is set to true
and you are not using a `[FromBody]` parameter (i.e. reading the request body directly from the Request), 
make sure you enable rewind on the request body stream, otherwise the controller won't be able to read
the request body since, by default, it's a forwand-only stream that can be read only once. You can enable rewind on your startup logic with the following code:

```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.Use(async (context, next) => {  // <----
        context.Request.EnableRewind();
        await next();
    });
    
    app.UseMvc();
}
```

## Output

Audit.WebApi output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Authenticated username
- Client IP address
- Form Variables, Action Arguments
- Http Headers
- Model State
- Exception details
- Request body
- Response Status and Body
- Comments and Custom Fields provided

With this information, you can not just know who did the operation, but also measure performance, observe exceptions thrown or get statistics about usage of your API.

## Output details

The following table describes the Audit.WebApi output fields:

### [Action](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/AuditApiAction.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **HttpMethod** | string | HTTP method (GET, POST, etc) |
| **ControllerName** | string | The controller name |
| **ActionName** | string | The action name |
| **FormVariables** | Object | Form-data input variables passed to the action |
| **ActionParameters** | Object | The action parameters passed |
| **UserName** | string | Username on the HttpContext Identity |
| **RequestUrl** | string | URL of the request |
| **IpAddress** | string | Client IP address |
| **ResponseStatusCode** | integer | HTTP response status code |
| **ResponseStatus** | string | Response status description |
| **RequestBody** | [BodyContent](#bodycontent) | The request body (optional) |
| **ResponseBody** | [BodyContent](#bodycontent) | The response body (optional) |
| **Headers** | Object | HTTP Headers (optional) |
| **ModelStateValid** | boolean | Boolean to indicate if the model is valid |
| **ModelStateErrors** | string | Error description when the model is invalid |
| **Exception** | string | The exception thrown details (if any) |

### [BodyContent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/BodyContent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Type** | string | The body type reported |
| **Length** | long? | The length of the body if reported |
| **Value** | Object | The body content |

## Customization

You can access the Audit Scope object for customization from the API controller action by calling the ApiController extension method `GetCurrentAuditScope()`.

For example:
```c#
[AuditApi]
public class UsersController : ApiController
{
    public IHttpActionResult Get(string id)
    {
       //...
       var auditScope = this.GetCurrentAuditScope();
       auditScope.Comment("New comment from controller");
       auditScope.SetCustomField("TestField", Guid.NewGuid());
       //...
    }
}
```

See [Audit.NET](https://github.com/thepirat000/Audit.NET) documentation about [Custom Field and Comments](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) for more information.

### Output Sample

```javascript
{  
   "EventType":"POST Values/Post",
   "Environment":{  
      "UserName":"Federico",
      "MachineName":"HP",
      "DomainName":"HP",
      "CallingMethodName":"WebApiTest.Controllers.ValuesController.Post()",
      "AssemblyName":"WebApiTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
      "Culture":"en-US"
   },
   "StartDate":"2017-03-09T18:03:05.5287603-06:00",
   "EndDate":"2017-03-09T18:03:05.5307604-06:00",
   "Duration":2,
   "Action":{  
      "HttpMethod":"POST",
      "ControllerName":"Values",
      "ActionName":"Post",
      "ActionParameters":{  
         "value":{  
            "Id":100,
            "Text":"Test"
         }
      },
      "FormVariables":{  
      },
      "RequestUrl":"http://localhost:65080/api/values",
      "IpAddress":"127.0.0.1",
      "ResponseStatus":"OK",
      "ResponseStatusCode":200,
      "RequestBody":{  
         "Type":"application/json",
         "Length":27,
         "Value":"{ Id: 100, Text: \"Test\" }"
      },
      "ResponseBody":{  
         "Type":"SomeObject",
         "Value":{  
            "Id":1795824380,
            "Text":"Test"
         }
      },
      "Headers": {
        "Connection": "Keep-Alive",
        "Accept": "text/html, application/xhtml+xml, image/jxr, */*",
        "Accept-Encoding": "gzip, deflate",
        "Accept-Language": "en-GB",
        "Host": "localhost:37341",
        "User-Agent": "Mozilla/5.0, (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0), like, Gecko"
      }
   }
}
```

