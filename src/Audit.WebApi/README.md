# Audit.WebApi

**ASP.NET MVC Web API Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for ASP.NET MVC Web API calls. This library provides a configurable infrastructure to log interactions with your Asp.NET (or Asp.NET Core) Web API.

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

## How it works

This library is implemented as an [action filter](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-2.1#action-filters) 
that intercepts the execution of action methods to generate a detailed audit trail.

For Asp.NET Core, it is also implemented as a [Middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-2.1) 
class that can be configured to log requests that does not reach the action filter (i.e. unsolved routes, parsing errors, etc).

## Usage

The audit can be enabled in different ways:

1. **Local Action Filter**: Decorating the controllers/actions to be audited with `AuditApi` action filter attribute. 
2. **Global Action Filter**: Adding the `AuditApiGlobalFilter` action filter as a global filter. This method allows more dynamic configuration of the audit settings.
3. **Middleware** (Asp.Net Core): Adding the `AuditMiddleware` to the pipeline. This method allow to audit request that doesn't get to the action filter.
4. **Middleware + Action Filters** (Asp.Net Core): Adding the **Audit Middleware** together with the **Global Action Filter** (or **Local Action Filters**). 
This is the recommended approach.

#### 1- Local Action Filter

Decorate your controller with `AuditApiAttribute`: 

```c#
using Audit.WebApi;

public class UsersController : ApiController
{
    [AuditApi]
    public IEnumerable<ApplicationUser> Get()
    {
      //...
    }

    [AuditApi(EventTypeName = "GetUser", 
        IncludeHeaders = true, IncludeResponseHeaders = true, IncludeResponseBody = true, IncludeRequestBody = true, IncludeModelState = true)]
    public IHttpActionResult Get(string id)
    {
     //...
    }
}
```

You can also decorate the controller class with the `AuditApi` attribute so it will apply to all the actions, for example:
```c#
using Audit.WebApi;

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

You can also add the `AuditApiAttribute` as a global filter, for example for Asp.NET Core:

```c#
using Audit.WebApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(_ => _
            .Filters.Add(new AuditApiAttribute()));
    }
}
```

> **Note**
> 
> For custom configuration it is recommended to use the 
> `AuditApiGlobalFilter` as a global filter. See next section.

#### 2- Global Action Filter

Alternatively, you can add one or more `AuditApiGlobalFilter` as global action filters. 
This method allows to dynamically change the audit settings as functions of the context, via a fluent API.

> Note this action filter cannot be used to statically decorate the controllers.

```c#
using Audit.WebApi;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(mvc =>
        {
            mvc.AddAuditFilter(config => config
                .LogActionIf(d => d.ControllerName == "Orders" && d.ActionName != "GetOrder")
                .WithEventType("{verb}.{controller}.{action}")
                .IncludeHeaders(ctx => !ctx.ModelState.IsValid)
                .IncludeRequestBody()
                .IncludeModelState()
                .IncludeResponseBody(ctx => ctx.HttpContext.Response.StatusCode == 200));
        });
    }

```

#### 3- Middleware

For Asp.NET Core, you can additionally (or alternatively) configure a [middleware](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-2.1)
to be able to log requests that doesn't get into an action filter (i.e. request that cannot be routed, etc). 

On your startup `Configure` method, call the `UseAuditMiddleware()` extension method:

```c#
using Audit.WebApi;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseAuditMiddleware(_ => _
            .FilterByRequest(rq => !rq.Path.Value.EndsWith("favicon.ico"))
            .WithEventType("{verb}:{url}")
            .IncludeHeaders()
            .IncludeResponseHeaders()
            .IncludeRequestBody()
            .IncludeResponseBody());

        app.UseMvc();
    }
}
```

> **Warning**
> 
> You should call `UseAuditMiddleware()` before `UseMvc()`, otherwise the middleware will 
> not be able to process MVC actions.

If you _only_ configure the middleware (no audit action filters) but want to ignore actions via `[AuditIgnoreAttribute]`, you **must** 
add an action filter to discard the `AuditScope`. This is needed because the middleware cannot inspect the 
MVC action attributes. You can use the `AuditIgnoreActionFilter` for this purpose, adding it to the MVC pipeline like this:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddMvc(mvc =>
    {
        mvc.Filters.Add(new AuditIgnoreActionFilter());
    });
}
```

#### 4- Middleware + Action Filters

You can mix the **Audit Middleware** together with the **Global Action Filter** (and/or **Local Action Filters**). Take into account that:

- Middleware will log any request regardless if an MVC action is reached or not.
- If an action is reached, the Action Filter will include specific MVC context info to the Audit Event.
- Only one Audit Event is generated per request, regardless of an action being processed by the Middleware and multiple Action Filters.
- The `AuditIgnore` atribute is handled by the Action Filters, there is no need to add the `AuditIgnoreActionFilter` to the MVC filters when using a mixed approach.

## Configuration

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings (Action Filter)

The `AuditApiAttribute` can be configured with the following properties:
- **EventTypeName**: A string that identifies the event type. Can contain the following placeholders: 
  - \{controller}: replaced with the controller name.
  - \{action}: replaced with the action method name.
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not. Default is false.
- **IncludeResponseHeaders**: Boolean to indicate whether to include the Http Response Headers or not. Default is false.
- **IncludeRequestBody**: Boolean to indicate whether to include or exclude the request body from the logs. Default is false. (Check the following note)
- **IncludeResponseBody**: Boolean to indicate whether to include response body or not. Default is false.
- **IncludeResponseBodyFor**: Alternative to _IncludeResponseBody_, to allow conditionally including the response body on the log, when certain Http Status Codes are returned.
- **ExcludeResponseBodyFor**: Alternative to _IncludeResponseBody_, to allow conditionally excluding the response body from the log, when certain Http Status Codes are returned.
- **IncludeModelState**: Boolean to indicate whether to include the Model State info or not. Default is false.
- **SerializeActionParameters**: Boolean to indicate whether the action arguments should be pre-serialized to the audit event. Default is false.
 
The `AuditApiGlobalFilter` can be configured with the following methods:
- **LogActionIf()** / **LogRequestIf()**: A function of the `ContollerActionDescriptor` / `HttpRequest` to determine whether the action should be logged or not.
- **WithEventType()**: A string (or a function of the executing context that returns a string) that identifies the event type. Can contain the following placeholders: 
  - \{controller}: replaced with the controller name.
  - \{action}: replaced with the action method name.
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
  - \{url}: replaced with the request URL.
- **IncludeHeaders()**: Boolean (or function of the executing context that returns a boolean) to indicate whether to include the Http Request Headers or not. Default is false.
- **IncludeResponseHeaders()**: Boolean (or function of the executing context that returns a boolean) to indicate whether to include the Http Response Headers or not. Default is false.
- **IncludeRequestBody()**: Boolean (or function of the executing context that returns a boolean) to indicate whether to include or exclude the request body from the logs. Default is false. (Check the following note)
- **IncludeResponseBody()**: Boolean (or function of the executed context that returns a boolean) to indicate whether to include response body or not. Default is false.
- **IncludeModelState()**: Boolean (or function of the executed context that returns a boolean) to indicate whether to include the Model State info or not. Default is false.
- **SerializeActionParameters()**: Boolean to indicate whether the action arguments should be pre-serialized to the audit event. Default is false.

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers).

### NOTE
When **IncludeRequestBody** is set to true (or when using **IncludeRequestBodyFor**/**ExcludeRequestBodyFor**), 
you must enable rewind on the request body stream, otherwise the controller won't be able to read
the request body since by default, it's a forwand-only stream that can be read only once. You can enable rewind on your startup logic with the following code:

```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.Use(async (context, next) => {  // <----
        context.Request.EnableBuffering(); // or .EnableRewind();
        await next();
    });
    
    app.UseMvc();
}
```

### Settings (Middleware)

- **FilterByRequest()**: A function of the `HttpRequest` to determine whether the request should be logged or not, by default all requests are logged.
- **IncludeHeaders()**: Boolean (or function of the HTTP context that returns a boolean) to indicate whether to include the Http Request Headers or not. Default is false.
- **IncludeResponseHeaders()**: Boolean (or function of the HTTP context that returns a boolean) to indicate whether to include the Http Response Headers or not. Default is false.
- **IncludeRequestBody()**: Boolean (or function of the HTTP context that returns a boolean) to indicate whether to include or exclude the request body from the logs. Default is false. (Check the following note)
- **IncludeResponseBody()**: Boolean (or function of the HTTP context that returns a boolean) to indicate whether to include response body or not. Default is false.
- **WithEventType()**: A string (or a function of the HTTP context that returns a string) that identifies the event type. Can contain the following placeholders (default is "{verb} {url}"): 
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
  - \{url}: replaced with the request URL.


## Audit Ignore attribute
To selectively exclude certain controllers, actions, action parameters or action responses, you can decorate them with `AuditIgnore` attribute. 

For example:

```c#
[Route("api/[controller]")]
[AuditApi(EventTypeName = "{controller}/{action}")]
public class AccountController : Controller
{
    [HttpGet]
    [AuditIgnore]
    public IEnumerable<string> GetAccounts()
    {
        // this action will not be audited
    }

    [HttpPost]
    public IEnumerable<string> PostAccount(string user, [AuditIgnore]string password)
    {
        // password argument will not be audited
    }

    [HttpGet]
    [return:AuditIgnore]
    public IEnumerable<string> GetSecrets()
    {
        // the return value of this action will not be audited
    }

}
```

## Output details

The following table describes the Audit.WebApi output fields:

### [Action](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.WebApi/AuditApiAction.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **TraceId** | string | A unique identifier per request |
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
| **Headers** | Object | HTTP Request Headers (optional) |
| **ResponseHeaders** | Object | HTTP Response Headers (optional) |
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
      "TraceId": "0HLFLQP4HGFAF_00000001",
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

## Web API template (dotnet new)

If you are creating an ASP.NET Core Web API project from scratch, you can use the 
**dotnet new template** provided on the library [Audit.WebApi.Template](https://www.nuget.org/packages/Audit.WebApi.Template/).
This allows to quickly generate an *audit-enabled* Web API project that can be used
as a starting point for your project or as a working example.

To install the template on your system, just type:

```sh
dotnet new -i Audit.WebApi.Template
```

Once you install the template, you should see it on the dotnet new templates list with the name `webapiaudit` as follows:

![capture](https://i.imgur.com/3zsw7ZP.png)

You can now create a new project on the current folder by running:

```sh
dotnet new webapiaudit
```

This will create a new Asp.NET Core project.

You can optionally include Entity Framework Core by adding the `-E` parameter

```sh
dotnet new webapiaudit -E
```

Also you can include a Swagger endpoint by adding the `-S` parameter

```sh
dotnet new webapiaudit -S
```

To get help about the options:

```
dotnet new webapiaudit -h
```

# Contribute

If you like this project please contribute in any of the following ways:

- [Star](https://github.com/thepirat000/Audit.NET/stargazers) this project on GitHub.
- Request a new feature or expose any bug you found by creating a [new issue](https://github.com/thepirat000/Audit.NET/issues/new).
- Ask any questions about the library on [StackOverflow](http://stackoverflow.com/questions/ask?tags=Audit.NET).
- Subscribe to and use the [Gitter Audit.NET channel](https://gitter.im/Audit-NET/Lobby).
- Support the project by [becoming a Backer](https://opencollective.com/auditnet):
[![Backer](https://opencollective.com/auditnet/tiers/backer.svg?avatarHeight=36&width=600)](https://opencollective.com/auditnet)     
- Spread the word by blogging about it, or sharing it on social networks:
  <p class="share-buttons">
    <a href="https://www.facebook.com/sharer/sharer.php?u=https://nuget.org/packages/Audit.NET/&amp;t=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Share this package on Facebook" src="https://nuget.org/Content/gallery/img/facebook.svg" / >
    </a>
    <a href="https://twitter.com/intent/tweet?url=https://nuget.org/packages/Audit.NET/&amp;text=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Tweet this package" src="https://nuget.org/Content/gallery/img/twitter.svg" />
    </a>
  </p>
- Make a donation via PayPal 
[![paypal](https://img.shields.io/badge/donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=thepirat000%40hotmail.com&currency_code=USD&source=url)
