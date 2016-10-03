# Audit.DynamicProxy

**Dynamic Proxy Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs by intercepting operations on _virtually_ any class.

Audit.DynamicProxy provides the infrastructure to create audit logs for a class without changing its code.
It relies on [Castle DynamicProxy](http://www.castleproject.org/projects/dynamicproxy/) library to intercept and record the operation calls (methods and properties) including caller info and arguments.

## Install

**NuGet Package** 
```
PM> Install-Package Audit.DynamicProxy
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.DynamicProxy.svg?style=flat)](https://www.nuget.org/packages/Audit.DynamicProxy/)

## Usage

To enable the audit log for an instance of a class, create a proxy for the class by calling the `AuditProxy.Create<>()` method.

This will return a proxied _audit-enabled_ instance that you should use instead of the real instance. Each operation on the proxy (access to a property or method call) will generate an Audit Event. 

Suppose you have a `MyRepository` instance that you want to audit, like this:
```c#
public class MyDataAccess
{
    IMyRepository _repository = new MyRepository(); // <- Audit this object

    public async Task<int> InsertUserAsync(string userName)
    {
        return await _repository.InsertUserAsync(userName);
    }
    // ...
}
```

To enable the audit on the `_repository` object, intercept its assignation by calling `AuditProxy.Create<>()`: 
```c#
public class MyDataAccess
{
    IMyRepository _repository = AuditProxy.Create<IMyRepository>(new MyRepository()); // Audited!

    public async Task<int> InsertUserAsync(string userName)
    {
        return await _repository.InsertUserAsync(userName);
    }
    // ...
}
```

You can also intercept _conditionally_, for example to avoid auditing when a debugger is attached:
```c#
public class MyDataAccess
{
    IMyRepository _repository = new MyRepository();

    public MyDataAccess()
    {
        if (!Debugger.IsAttached)
        {
            // Audit only when no debugger is attached
            _repository = AuditProxy.Create<IMyRepository>(_repository);
        }
    }
}
```

## Creating proxies

The `AuditProxy.Create<>()` method returns an auditable proxy object that inherits from the proxied class/implements proxied interface and forwards calls to the real object.

This is the method signature:

`T AuditProxy.Create<T>(T instance, InterceptionSettings settings = null)`

Give special attention to the generic type argument `T`, it can be:
- **An interface**: Will generate an _interface proxy_ to log all the interface member calls. (Recommended)
- **A class type**: Will generate a _class proxy_ to log virtual member calls. Non-virtual methods can't be automatically audited. 

> When using an _interface proxy_, the interception is limited to the members of the interface. And when using a _class proxy_, the interception is limited to its virtual members.

The `instance` argument is an instance of the object to be audited.

The `settings` argument allows you to change the default settings. See [settings](#settings) section for more information.

## Settings

The `InterceptionSettings` class include the following settings:

- **EventType**: A string that identifies the event type. Default is "\{class}.\{method}". Can contain the following placeholders: 
  - \{class}: Replaced by the class name
  - \{method}: Replaced by the method name
- **IgnoreProperties**: A boolean indicating whether the audit should ignore the property getters and setters.
 If _true_, the property accesses will not be logged. Default is _false_
- **IgnoreEvents**: A boolean indicating whether the audit should ignore the event attach and detach operations.
 If _true_, the event accesses will not be logged. Default is _false_
- **MethodFilter**: A function that takes a `MethodInfo` and returns a boolean indicating whether the method should be taken into account for the logging. Use this setting to have fine grained control over the methods that should be audited. By default all methods are included.
- **AuditDataProvider**: Allows to set a specific audit data provider for this instance. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.

## AuditIgnore Attribute

You can exclude specific members, arguments or return values from the audit, by decorating them with the `AuditIgnore` attribute. For example:

```c#
public class MyRepository : IMyRepository
{
    //Ignore a method (no events will be generated for this method)
    [AuditIgnore] 
    public User GetUser(string userName)
    {
       ...
    }

    //Ignore an argument (argument value will not be included in the output)
    public User FindUser(int type, [AuditIgnore] Filter filter)
    {
        ...
    }

    //Exclude the return value (result will not be included in the output)
    [return:AuditIgnore] 
    public List<User> SearchUsers(string text)
    {
        ...
    }
}
``` 

## Customization

You can access the current audit scope from an audited member by getting the static `AuditProxy.CurrentScope` property. 

> The static property `AuditProxy.CurrentScope` returns the scope for the **current running thread** and should be accessed from the same thread as the executing audited operation.
Calling this from a different thread will lead to an unexpected result. On _async_ methods, you should only access this propery **before** any _await_ ocurrence.

For example:
```c#
public class MyRepository : IMyRepository
{
    public async Task<int> InsertUserAsync(string userName)
    {
        var auditScope = AuditProxy.CurrentScope;   // Get the current scope
        auditScope.SetCustomField("TestField", Guid.NewGuid()); // Set a custom field
        if (pleaseDoNotLog)
        {
            auditScope.Discard(); // Discard the event
        }
        
        //... existing code to insert user ...
        return await _repository.InsertUserAsync(userName);
    }
}
``` 

## Output

Audit.DynamicProxy output includes:

- Execution time and duration (async-aware)
- Environment information such as user, machine, domain and locale.
- Method parameters (input and output)
- Return object
- Exception details
- [Comments and Custom Fields](#custom-fields-and-comments) provided

With this information you can know who did the operation, and also measure performance, observe exceptions thrown and get statistics about usage of your classes.

> **Async** calls are logged when the asynchronous call ends; as a continuation task, so the Audit Event includes the actual duration and result.

## Output Details

The following table describes the Audit.DynamicProxy output fields:

- <h3>[AuditInterceptEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.DynamicProxy/AuditInterceptEvent.cs)</h3>

Describes an operation call event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| ClassName  | string | Name of class where the operation is defined |
| MethodName | string | Name of the audited method |
| IsAsync | boolean | A boolean indicating whether this method is async |
| AsyncStatus | string | If the method is async, this will contain the final [Task status](https://msdn.microsoft.com/en-us/library/system.threading.tasks.taskstatus(v=vs.110).aspx) (`Canceled`, `Faulted`, `RanToCompletion`) |
| InstanceQualifiedName  | string | Full qualified name of the class |
| MethodSignature   | string | The complete method signature |
| PropertyName | string | Name of the property modified (if any) |
| EventName | string | Name of the event modified (if any) |
| Arguments  | [argument](#auditinterceptargument) array | The operation arguments (input and output parameters) |
| Success | boolean | Indicates if the operation completed succesfully |
| Exception | string | The exception details when an exception is thrown |
| Result | [argument](#auditinterceptargument) object | The result of the operation |

- <h3>[AuditInterceptArgument](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.DynamicProxy/AuditInterceptArgument.cs)</h3>

Describes an operation argument

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| Index | string | Argument index |
| Name  | string | Argument name |
| Type | string | Argument type |
| Value | object | Input argument value |
| OutputValue | object | Output argument value (Only for `ref` or `out` parameters) |

## Output Samples

#### Successful async method call:
```javascript
{
  "EventType": "MyRepository.InsertUserAsync",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "CallingMethodName": "Audit.DynamicProxy.AuditInterceptor.Intercept()",
    "AssemblyName": "Audit.DynamicProxy, Version=4.5.2.0, Culture=neutral, PublicKeyToken=null",
    "Culture": "en-GB"
  },
  "StartDate": "2016-09-30T12:00:35.7073819-05:00",
  "EndDate": "2016-09-30T12:00:36.7168197-05:00",
  "Duration": 1009,
  "InterceptEvent": {
    "ClassName": "MyRepository",
    "MethodName": "InsertUserAsync",
    "IsAsync": true,
    "AsyncStatus": "RanToCompletion",
    "InstanceQualifiedName": "Audit.DynamicProxy.UnitTest.MyRepository, Audit.DynamicProxy.UnitTest, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null",
    "MethodSignature": "System.Threading.Tasks.Task`1[System.Int32] InsertUserAsync(System.String)",
    "Arguments": [
      {
        "Index": 0,
        "Name": "userName",
        "Type": "String",
        "Value": "thepirat000"
      }
    ],
    "Success": true,
    "Result": {
      "Type": "Task<Int32>",
      "Value": 142857
    }
  }
}
```

#### Failed async method call:
```javascript
{
  "EventType": "MyRepository.InsertUserAsync",
  "Environment": {
    "UserName": "Federico",
    "MachineName": "HP",
    "DomainName": "HP",
    "CallingMethodName": "Audit.DynamicProxy.AuditInterceptor.Intercept()",
    "AssemblyName": "Audit.DynamicProxy, Version=4.5.2.0, Culture=neutral, PublicKeyToken=null",
    "Exception": "COMException: Exception from HRESULT: 0xE0434352",
    "Culture": "en-GB"
  },
  "StartDate": "2016-09-30T12:18:34.5093824-05:00",
  "EndDate": "2016-09-30T12:18:35.5388113-05:00",
  "Duration": 1029,
  "InterceptEvent": {
    "ClassName": "MyRepository",
    "MethodName": "InsertUserAsync",
    "IsAsync": true,
    "AsyncStatus": "Faulted",
    "InstanceQualifiedName": "Audit.DynamicProxy.UnitTest.MyRepository, Audit.DynamicProxy.UnitTest, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null",
    "MethodSignature": "System.Threading.Tasks.Task`1[System.Int32] InsertUserAsync(System.String)",
    "Arguments": [
      {
        "Index": 0,
        "Name": "userName",
        "Type": "String",
        "Value": null
      }
    ],
    "Success": false,
    "Exception": "(ArgumentNullException) UserName cannot be null",
    "Result": null
  }
}
```


