# Audit.Mvc

**MVC Actions Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for MVC actions. Supporting AspNetCore Mvc.

Audit.Mvc provides the infrastructure to log interactions with MVC applications. It can record action methods calls with caller info and arguments.

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

Decorate with an `AuditAttribute` the MVC Actions/Controllers you want to audit.

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

## Configuration

### Output

The MVC audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings

The `AuditAttribute` can be configured with the following properties:
- **EventType**: A string that identifies the event type. Can contain the following placeholders: 
  - \{controller}: replaced with the controller name.
  - \{action}: replaced with the action method name.
  - \{verb}: replaced with the HTTP verb used (GET, POST, etc).
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
        context.Request.EnableRewind();
        await next();
    });
}
```


## Output details

The following table describes the Audit.Mvc output fields:

- ### [AuditAction object](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.Mvc/AuditAction.cs)

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **HttpMethod** | string | HTTP method (GET, POST, etc) |
| **ControllerName** | string | The controller name |
| **ActionName** | string | The action name |
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

