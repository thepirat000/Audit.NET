#Audit.Mvc

**MVC Actions Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** (A small framework to audit executing operations in .NET).

Generate Audit Trails for MVC actions.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.Mvc/)**
```
PM> Install-Package Audit.Mvc
```

##Usage

Add an `AuditAttribute` to the MVC Controller Actions you want to audit.

For example:

```c#
public class HomeController : Controller
{
    [Audit]
    public ActionResult Index(string test)
    {
      //...
      return View(new SomeViewModel() { Id = 0, Name = test });
    }

    [Audit(EventType = "InsertOrderAction", IncludeHeaders = true, IncludeModel = true)]
    [HttpPost]
    public ActionResult InsertOrder(OrderViewModel model)
    {
      //...
    }
}
```

##Configuration

The `AuditAttribute` can be configured with the following properties:
- IncludeHeaders: Boolean to indicate whether to include the Http Request Headers or not.
- IncludeModel: Boolean to indicate whether to include the View Model or not.
- EventType: A string that identifies the event type .

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output-configuration).

##Output

The Audit.Mvc tool will output the following information:

- Authenticated username
- Client IP address
- Form Variables
- Http Headers
- Action Parameters
- Model and Model State Errors

###Output Sample

```javascript
{
  "EventType": "Home/Index (GET)",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "CallingMethodName": "TestMvc.AuditAttribute.OnActionExecuting()",
    "AssemblyName": "TestMvc, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
    "Exception": null,
    "Culture": "en-GB"
  },
  "StartDate": "2016-08-22T11:28:01.2901086-05:00",
  "EndDate": "2016-08-22T11:28:01.4634822-05:00",
  "Action": {
    "HttpMethod": "GET",
    "ControllerName": "Home",
    "ActionName": "Index",
    "FormVariables": {},
    "ActionParameters": {
      "test": null
    },
    "UserName": "federico@mycompany.com",
    "RequestUrl": "/",
    "IpAddress": "::1",
    "ResponseStatus": "200 OK",
    "ResponseStatusCode": 200,
    "Model": {
      "Id": 0,
      "Name": "this is a test"
    },
    "ModelStateValid": true,
    "RedirectLocation": null
  }
}
```


