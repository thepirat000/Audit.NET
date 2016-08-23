#Audit.Mvc

**MVC Actions Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (An extensible framework to audit executing operations in .NET).

Generate Audit Trails for MVC actions.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.Mvc/)**
```
PM> Install-Package Audit.Mvc
```

##Usage

Decorate with an `AuditAttribute` the MVC Actions you want to audit.

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
- **IncludeHeaders**: Boolean to indicate whether to include the Http Request Headers or not.
- **IncludeModel**: Boolean to indicate whether to include the View Model or not.
- **EventType**: A string that identifies the event type .

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output-configuration).

##Output

The Audit.Mvc tool will output the following information:

- Authenticated username
- Client IP address
- Form Variables
- Http Headers
- Action Parameters
- View Model 
- Model State Errors
- Exceptions

###Output Sample for Get operation

> HTTP GET Home/Index?id=1234567&name=test

```javascript
{
  "EventType": "Home/Index (GET)",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "CallingMethodName": "Audit.Mvc.AuditAttribute.OnActionExecuting()",
    "AssemblyName": "Audit.Mvc, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null",
    "Exception": null,
    "Culture": "en-GB"
  },
  "StartDate": "2016-08-22T18:31:48.1450924-05:00",
  "EndDate": "2016-08-22T18:31:51.1334012-05:00",
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

> HTTP POST Home/TestPost
> BODY: id=1234567&name=test

```javascript
{
  "EventType": "InsertOrderAction",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "CallingMethodName": "Audit.Mvc.AuditAttribute.OnActionExecuting()",
    "AssemblyName": "Audit.Mvc, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null",
    "Exception": null,
    "Culture": "en-GB"
  },
  "StartDate": "2016-08-22T18:31:55.5720036-05:00",
  "EndDate": "2016-08-22T18:31:55.5805128-05:00",
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
      "User-Agent": "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36",
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

