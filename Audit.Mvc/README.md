#Audit.Mvc

**MVC Actions Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for MVC actions.

Audit.Mvc provides the infrastructure to log interactions with MVC applications. It can record action methods calls with caller info and arguments.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.Mvc/)**
```
PM> Install-Package Audit.Mvc
```

##Usage

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

##Configuration

The `AuditAttribute` can be configured with the following properties:
- **EventType**: A string that identifies the event type. Can contain the following placeholders: 
 - {controller}: replaced with the controller name.
 - {action}: replaced with the action method name.
 - {verb}: replaced with the HTTP verb used (GET, POST, etc).
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not.
- **IncludeModel**: Boolean to indicate whether to include the View Model or not.

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output-configuration).

##Output

Audit.Mvc output includes:

- Execution time and duration
- Authenticated username
- Client IP address
- Form Variables, Action Parameters
- Http Headers
- View Model, View Model State Errors
- Exceptions
- Comments and Custom Fields 

With this information, you can not just know who did the operation, but also measure performance, observe exceptions thrown or get statistics about usage of your application.

##Customization
You can access the Audit Scope from the controller action by calling the Controller extension method `GetCurrentAuditScope()`. For example:
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

See Audit.NET [Custom Field and Comments](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) for more information.

###Output Sample for Get operation

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

###Output Sample for Post operation

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

