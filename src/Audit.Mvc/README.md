# Audit.Mvc

**MVC Actions Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for MVC actions. Supporting AspNetCore Mvc.

Audit.Mvc provides the infrastructure to log interactions with MVC applications. 
It can record action methods calls to controllers and razor pages.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.Mvc
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.Mvc.svg?style=flat)](https://www.nuget.org/packages/Audit.Mvc/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.Mvc.svg)](https://www.nuget.org/packages/Audit.Mvc/)

# IMPORTANT NOTE

If your project targets the full .NET framework, but you are using AspNet Core (`Microsoft.AspNetCore.Mvc.*`) 
you should install and reference the `Audit.Mvc.Core` package instead, otherwise it will assume you are targeting
the old generation of ASP.NET:

```
PM> Install-Package Audit.Mvc.Core
```

If your project targets the NET Core framework (NetStandard >= 1.6), there is no difference between using `Audit.Mvc` or `Audit.Mvc.Core` 
since both assumes AspNet Core.

## Usage

### MVC Controllers

Decorate the MVC Actions or Controllers you want to audit with `[Audit]` action filter.

For example:

```c#
public class HomeController : Controller
{
    [Audit]
    public ActionResult Index(int id, string name)
    {
      //...
      return View(new SomeViewModel() { Id = id, Name = name });
    }

    [Audit(EventType = "InsertOrderAction", IncludeHeaders = true, IncludeModel = true)]
    [HttpPost]
    public ActionResult TestPost(SomeViewModel model)
    {
      //...
    }
}
```

> The `[Audit]` attribute cannot be used on razor pages,
because [action filters are not supported on razor pages](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-3.1#filter-types).

### Razor pages

To audit razor pages, include the `AuditPageFilter` on the filters collection on your startup code, for example:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddRazorPages()
        .AddMvcOptions(options =>
        {
            options.Filters.Add(new AuditPageFilter()
            {
                IncludeHeaders = true
            });
        });
}
```

Or you can apply the filter only to certain pages, for example for pages under `/Movies` path:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddRazorPages(options =>
    {
        options.Conventions.AddFolderApplicationModelConvention("/Movies", model => model.Filters.Add(new AuditPageFilter()
        {
            IncludeResponseBody = true
        }));
    });
}
```

Alternatively, if you want to setup the audit on a particular page and/or don't want to add the filter as a global filter, 
you can override the `OnPageHandlerExecutionAsync` on your page model and manually call the same method
on an `AuditPageFilter` instance:

```c#
public class YourPageModel : PageModel
{
    private readonly AuditPageFilter _pageFilter = new AuditPageFilter() { IncludeHeaders = true };

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        await _pageFilter.OnPageHandlerExecutionAsync(context, next);
    }
    // ...
}
```

## Configuration

### Output

The MVC audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings

The `AuditAttribute` can be configured with the following properties:
- **EventType**: A string that identifies the event type. Can contain the following placeholders: 
  - \{controller}: replaced with the controller name (only for MVC).
  - \{action}: replaced with the action method name (or the display name for razor pages).
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
  - \{area}: replaced with the area name (only for razor pages).
  - \{path}: replaced with the view path (only for razor pages).
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not.
- **IncludeModel**: Boolean to indicate whether to include the View Model or not.
- **IncludeRequestBody**: Boolean to indicate whether to include or exclude the request body from the logs. Default is false. (Check following note)
- **IncludeResponseBody**: Boolean to indicate whether to include response body or not. Default is false.
- **SerializeActionParameters**: Boolean to indicate whether the action arguments should be pre-serialized to the audit event. Default is false.

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers).

### NOTE
When **IncludeRequestBody** is set to true you may need to enable rewind on the request body stream, otherwise the controller won't be able to read
the request body more than once (by default it's a forwand-only stream that can be read only once). You can enable rewind on your startup logic with the following startup code:

```c#
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.Use(async (context, next) => {
        context.Request.EnableBuffering();
        await next();
    });
}
```

## Audit Ignore attribute
To selectively exclude certain controllers, action methods or action parameters, you can decorate them with `AuditIgnore` attribute. 

For example:

```c#
[Audit(IncludeHeaders = true, IncludeModel = true)]
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

    // ...
}
```

You can also decorate the razor pages classes, methods or arguments to be ignored on the audits:

```c#
public class IndexModel : PageModel
{
    [AuditIgnore]
    public void OnGet(string user)
    {
        // this action will not be audited
    }

    public async Task<IActionResult> OnPostAsync([AuditIgnore]string password)
    {
        // password argument will not be audited
    }
}
```


## Output details

The following table describes the Audit.Mvc output fields:

- ### [AuditAction object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/AuditAction.cs)

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **TraceId** | string | A unique identifier per request |
| **HttpMethod** | string | HTTP method (GET, POST, etc) |
| **ControllerName** | string | The controller name (or the area name for razor pages) |
| **ActionName** | string | The action name (or the display name for razor pages) |
| **ViewName** | string | The view name (if any) |
| **ViewPath** | string | View physical path (if any) |
| **FormVariables** | Object | Form-data input variables passed to the action |
| **ActionParameters** | Object | The action parameters passed |
| **RequestBody** | [BodyContent](#bodycontent) | The request body (optional) |
| **ResponseBody** | [BodyContent](#bodycontent) | The response body (optional) |
| **UserName** | string | Username on the HttpContext Identity |
| **RequestUrl** | string | URL of the request |
| **IpAddress** | string | Client IP address |
| **ResponseStatusCode** | integer | HTTP response status code |
| **ResponseStatus** | string | Response status description |
| **Headers** | Object | HTTP Headers (optional) |
| **Model** | Object | The model object returned by the controller (if any) (optional) |
| **ModelStateValid** | boolean | Boolean to indicate if the model is valid |
| **ModelStateErrors** | string | Error description when the model is invalid |
| **RedirectLocation** | string | The redirect location (if any) |
| **Exception** | string | The exception thrown details (if any) |

### [BodyContent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/BodyContent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Type** | string | The body type reported |
| **Length** | long? | The length of the body if reported |
| **Value** | Object | The body content |

## Customization

You can access the Audit Scope from the controller action by calling the Controller extension method `GetCurrentAuditScope()`. 

For example:
```c#
public class HomeController : Controller
{
    [Audit]
    public ActionResult Index(int id, string name)
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

### Output Sample for Get operation

> `HomeController.Index` (GET) with params: id=1234567&name=test

```javascript
{
    "EventType": "Home/Index (GET)",
    "Environment": {
        ...
    },
    "StartDate": "2016-08-22T18:31:14.6550924-05:00",
    "EndDate": "2016-08-22T18:31:23.1834012-05:00",
    "Duration": 8529,
    "Action": {
        "TraceId": "0HLFLQP4HGFAG_00000001",
        "HttpMethod": "GET",
        "ControllerName": "Home",
        "ActionName": "Index",
        "ViewName": "Index",
        "ViewPath": "~/Views/Home/Index.cshtml",
        "FormVariables": {},
        "ActionParameters": {
            "id": 1234567,
            "name": "test",
        },
        "UserName": "federico@mycompany.com",
        "RequestUrl": "/",
        "IpAddress": "127.0.0.1",
        "ResponseStatus": "200 OK",
        "ResponseStatusCode": 200,
        "ModelStateValid": true,
        "RedirectLocation": null
    }
}
```

### Output Sample for Post operation

> `HomeController.TestPost` (POST) with body: id=1234567&name=test

```javascript
{
    "EventType": "InsertOrderAction",
    "Environment": {
        ...
    },
    "StartDate": "2016-08-22T18:31:00.0020036-05:00",
    "EndDate": "2016-08-22T18:31:15.1705128-05:00",
    "Duration": 15000,
    "Action": {
        "TraceId": "0HLFLQP4HGFAG_00000002",
        "HttpMethod": "POST",
        "ControllerName": "Home",
        "ActionName": "TestPost",
        "FormVariables": {
            "id": "1234567",
            "name": "test"
        },
        "ActionParameters": {
            "model": {
                "id": 1234567,
                "name": "test"
            }
        },
        "UserName": "federico@mycompany.com",
        "RequestUrl": "/Home/TestPost",
        "IpAddress": "::1",
        "ResponseStatus": "200 OK",
        "ResponseStatusCode": 200,
        "Headers": {
            "Cache-Control": "max-age=0",
            "Connection": "keep-alive",
            "Content-Length": "24",
            "Content-Type": "application/x-www-form-urlencoded",
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            "Accept-Encoding": "gzip, deflate",
            "Accept-Language": "es-419,es;q=0.8",
            "Host": "localhost:37341",
            "Referer": "http://localhost:37341/",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743",
            "Origin": "http://localhost:37341",
            "Upgrade-Insecure-Requests": "1"
        },
        "ModelStateValid": false,
        "ModelStateErrors": {
            "Id": "The field Id must be between 0 and 9999."
        },
        "RedirectLocation": null
    }
}
```

## MVC template (dotnet new)

If you are creating an ASP.NET Core MVC project from scratch, you can use the 
**dotnet new template** provided on the library [Audit.Mvc.Template](https://www.nuget.org/packages/Audit.Mvc.Template/).
This allows to quickly generate an *audit-enabled* MVC project that can be used as a starting point for your project or as a working example.

To install the template on your system, just type:

```sh
dotnet new -i Audit.Mvc.Template
```

Once you install the template, you should see it on the dotnet new templates list with the name `mvcaudit` as follows:

![capture](https://i.imgur.com/awBKluE.png)

You can now create a new project on the current folder by running:

```sh
dotnet new mvcaudit
```

This will create a new Asp.NET Core 2.1 project.

To get help about the options:

```
dotnet new mvcaudit -h
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
- Buy me a coffee via [ko-fi](https://www.ko-fi.com/X8X3OBB5):
  <br/>[![ko-fi](https://www.ko-fi.com/img/donate_sm.png)](https://ko-fi.com/X8X3OBB5)
