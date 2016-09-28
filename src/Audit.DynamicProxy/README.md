# Audit.DynamicProxy

**Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs by intercepting operations on any class.

Audit.DynamicProxy provides the infrastructure to create audit logs for a class without changing its code.
It relies on [Castle DynamicProxy](http://www.castleproject.org/projects/dynamicproxy/) library to intercept and record the operation calls (methods, properties, fields and events) including caller info and arguments.

## Install

**[NuGet Package](https://www.nuget.org/packages/Audit.DynamicProxy/)**
```
PM> Install-Package Audit.DynamicProxy
```

## Usage

To enable the audit log for an instance of a class, create a proxy for the class by calling the `AuditProxy.Create<>()` method.

This will return a proxied _audit-enabled_ instance of the class that you should use in order to generate logs.

Suppose you have a `MyRepository` class that you want to audit, for example:
```c#
public class MyDataAccess
{
    IMyRepository _repository = new MyRepository();
    
    public void InsertUser(string userName)
    {
        _repository.InsertUser(userName);
    }
        
    //...
}
```

To enable the audit on the `_repository` object, intercept its assignation by calling `AuditProxy.Create<>()`: 
```c#
public class MyDataAccess
{
    IMyRepository _repository = AuditProxy.Create<IMyRepository>(new MyRepository());

    public void InsertUser(string userName)
    {
        _repository.InsertUser(userName);
    }

    //...
}
```

Or you can optionally intercept the assignation, for example to optionally audit when no debugger is attached:
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
- **A class type**: Will generate a _class proxy_ to log virtual member calls. Non-virtual methods or fields can't be automatically audited. 

The `instance` argument is an instance of the object to be audited.

The `settings` argument allows you to change the default settings. See [settings](#settings) section for more information.

## Settings

The class `InterceptionSettings` provides access to the following settings:

- **EventType**: A string that identifies the event type. Default is "\{class}.\{method}". Can contain the following placeholders: 
  - \{class}: Replaced by the class name
  - \{method}: Replaced by the method name
- **IgnoreProperties**: A boolean indicating whether the audit should ignore the property getters and setters.
 If _true_, the property accesses will not be logged. Default is _false_
- **IgnoreEvents**: A boolean indicating whether the audit should ignore the event attach and detach operations.
 If _true_, the event accesses will not be logged. Default is _false_
- **MethodFilter**: A function that takes a `MethodInfo` and returns a boolean indicating whether the method should be taken into account for the logging. Use this setting to have fine grained control over the methods that should be audited. By default all methods are included.
- **AuditDataProvider**: Allows to set a specific audit data provider for this instance. By default the globally configured data provider is used. See [Audit.NET configuration](https://github.com/thepirat000/Audit.NET#data-provider) section for more information.

## AuditIgnore attribute

You can ignore specific members from the audit, by decorating them with the `AuditIgnore` attribute. For example:

```c#
public class MyRepository : IMyRepository
{
    [AuditIgnore]
    public User GetUser(string userName)
    {
        //...
    }
}
``` 

## Customization

You can access the current audit scope from an audited member by getting the static `AuditProxy.CurrentScope` property. 

> This property returns the scope for the **current running thread** and should be accessed from the same thread as the executing audited operation.
Calling this property from a different thread will return NULL or an unexpected value.

For example:
```c#
public class MyRepository : IMyRepository
{
    public void InsertUser(string userName)
    {
        var auditScope = AuditProxy.CurrentScope;   // Get the current scope
        auditScope.SetCustomField("TestField", Guid.NewGuid()); // Set a custom field
        
        //... add the user ...
        
        if (pleaseDoNotLog)
        {
            auditScope.Discard(); // Discard the event
        }
    }
}
``` 

## Output

Audit.DynamicProxy output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Method parameters (input and output)
- Return object
- Exception details
- [Comments and Custom Fields](#custom-fields-and-comments) provided

With this information you can know who did the operation, and also measure performance, observe exceptions thrown and get statistics about usage of your classes.

## Output Details

The following table describes the Audit.DynamicProxy output fields:

- <h3>[AuditInterceptEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.DynamicProxy/AuditInterceptEvent.cs)</h3>

Describes an operation call event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| ClassName  | string | Name of class where the operation is defined |
| MethodName | string | Name of the audited method |
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
| Name  | string | The argument name |
| Type | string | The argument type |
| Value | object | The input argument value |
| OutputValue | object | The output argument value (Only for ref or out parameters) |

## Output Samples

- Successful method call:
```javascript
{
	"EventType": "MyRepository.InsertUser",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.DynamicProxy.AuditInterceptor.Intercept()",
		"AssemblyName": "Audit.DynamicProxy, Version=4.5.0.0, Culture=neutral, PublicKeyToken=null",
		"Culture": "en-GB"
	},
	"StartDate": "2016-09-28T09:53:17.7555677-05:00",
	"EndDate": "2016-09-28T09:53:99.7565477-05:00",
	"Duration": 82,
	"InterceptEvent": {
		"ClassName": "MyRepository",
		"MethodName": "InsertUser",
		"InstanceQualifiedName": "Audit.DynamicProxy.UnitTest.MyRepository, Audit.DynamicProxy.UnitTest, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null",
		"MethodSignature": "Void InsertUser(System.String)",
		"Arguments": [{
			"Name": "userName",
			"Type": "String",
			"Value": "thepirat000"
		}],
		"Success": true,
		"Result": {
			"Type": "Void",
			"Value": null
		}
	}
}
```

- Exception on method call:
```javascript
{
	"EventType": "MyRepository.InsertUser",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.DynamicProxy.AuditInterceptor.Intercept()",
		"AssemblyName": "Audit.DynamicProxy, Version=4.5.0.0, Culture=neutral, PublicKeyToken=null",
		"Exception": "COMException: Exception from HRESULT: 0xE0434352",
		"Culture": "en-GB"
	},
	"StartDate": "2016-09-28T09:56:32.8875018-05:00",
	"EndDate": "2016-09-28T09:58:32.8880027-05:00",
	"Duration": 3,
	"InterceptEvent": {
		"ClassName": "MyRepository",
		"MethodName": "InsertUser",
		"InstanceQualifiedName": "Audit.DynamicProxy.UnitTest.MyRepository, Audit.DynamicProxy.UnitTest, Version=3.0.0.0, Culture=neutral, PublicKeyToken=null",
		"MethodSignature": "Void InsertUser(System.String)",
		"Arguments": [{
			"Name": "userName",
			"Type": "String",
			"Value": null
		}],
		"Success": false,
		"Exception": "(ArgumentNullException) userName cannot be NULL\r\nParameter name: userName",
		"Result": null
	}
}
```


