# Audit.MongoClient

**MongoDB client audit extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).** 

Generate Audit Logs by adding a [Command Event Subscriber](https://mongodb.github.io/mongo-csharp-driver/2.8/reference/driver_core/events/) 
into the configuration of the MongoDB Driver.

Audit.MongoClient provides the infrastructure to intercept a `MongoClient` instance, enabling the generation of audit logs for operations executed within MongoDB.

> Note: This library is designed to **generate** audit events, not for storing events, If you're aiming to **store** audit events in a Mongo DB collection, you may use the [`Audit.NET.MongoDB`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET.MongoDB/README.md) package.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.MongoClient
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.MongoClient.svg?style=flat)](https://www.nuget.org/packages/Audit.MongoClient/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.MongoClient.svg)](https://www.nuget.org/packages/Audit.MongoClient/)

## Usage

To enable the audit log for a `MongoClient` instance you have to register a `MongoAuditEventSubscriber` instance 
to the ClusterBuilder.

This registration can be done in several ways:

- Registering an instance of the provided `MongoAuditEventSubscriber`:

```c
using Audit.MongoClient;
//...

var mongoSettings = new MongoClientSettings()
{
    Server = new MongoServerAddress("localhost", 27017),
    
    // Register the audit subscriber:
    ClusterConfigurator = clusterBuilder => clusterBuilder.Subscribe(new MongoAuditEventSubscriber()
    {
        IncludeReply = true
    })  
};

// Create the audited client
_client = new MongoDB.Driver.MongoClient(mongoSettings);
```

- Calling the provided `AddAuditSubscriber()` extension method in `ClusterBuilder`:

```c#
using Audit.MongoClient;
//...

var mongoSettings = new MongoClientSettings()
{
    Server = new MongoServerAddress("localhost", 27017),
    ClusterConfigurator = clusterBuilder => clusterBuilder.AddAuditSubscriber(auditConfig => auditConfig
        .IncludeReply())
};

_client = new MongoDB.Driver.MongoClient(mongoSettings);
```

- Reusing an existing `MongoClientSettings` instance by calling the provided `AddAuditSubscriber()` extension method:

```c#
_client = new MongoDB.Driver.MongoClient(mongoSettings.AddAuditSubscriber(cfg => cfg
    .IncludeReply());
```

## Configuration

### Output

The audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. Please refer to the [data providers](https://github.com/thepirat000/Audit.NET#data-providers) section on Audit.NET documentation.

### Settings

The `MongoAuditEventSubscriber` class allows to configure the following settings:

- **EventType**: A string that identifies the event type. Default is "\{command}". It can contain the following placeholders: 
  - \{command}: Replaced by the Command Name (insert, update, delete, find, ...)
- **IncludeReply**: Specifies whether the command audit event should include the server reply. The reply is not included by default.
- **CommandFilter**: Set a filter function to determine which command events to log depending on the command start information. By default all commands are logged.
- **CreationPolicy**: Allows to set a specific event creation policy. By default the globally configured creation policy is used. See [Audit.NET Event Creation Policy](https://github.com/thepirat000/Audit.NET#event-creation-policy) section for more information.
- **AuditDataProvider**: Allows to set a specific audit data provider. By default the globally configured data provider is used. See [Audit.NET Data Providers](https://github.com/thepirat000/Audit.NET/blob/master/README.md#data-providers) section for more information.
- **AuditScopeFactory**: Allows to set a specific audit scope factory. By default the globally configured [`AuditScopeFactory`](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.NET/AuditScopeFactory.cs) is used. 

You can customize these settings using the fluent API provided. Additionally, some settings can be set as functions of the 
executed command, allowing you to adapt the behavior based on the specific command, such as including the reply only in specific cases.

For example, to only audit _insert_ and _delete_ commands, and include the reply only if its length is less than 512 bytes:

```c#
var mongoSettings = new MongoClientSettings()
{
    Server = new MongoServerAddress("localhost", 27017),
    ClusterConfigurator = cc => cc
        .AddAuditSubscriber(auditConfig => auditConfig
            .IncludeReply(cmd => cmd.Reply.ToBson().Length < 512)
            .CommandFilter(cmd => cmd.CommandName is "insert" or "delete"))
};
```

## Output Details

The following table describes the Audit.MongoClient output fields:

### [MongoCommandEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.MongoClient/MongoCommandEvent.cs)

Describes a command call event

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| RequestId | int | The unique request identifier |
| Connection | MongoConnection | Connection information |
| OperationId | long? | The operation identifier |
| CommandName | string | The Mongo command name (insert, update, delete, ...) |
| Body | object | The command body |
| Duration | int | The duration of the Mongo Event in milliseconds |
| Success | bool? | Indicates if the command succeeded |
| Reply | object | The database reply (optional) |
| Error | string | The database error message if an error occurred, otherwise NULL |
| Timestamp | DateTime | The command event Timestamp  |

### [MongoConnection](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.MongoClient/MongoConnection.cs)

Contains the command's connection information

| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| ClusterId | int | The Connection cluster identifier |
| Endpoint | string | The Connection endpoint |
| LocalConnectionId | long | The local connection identifier |
| ServerConnectionId | long? | The server connection identifier |

## Output Sample

Mongo insert command:

```javascript
{
	"Command": {
		"RequestId": 5,
		"Connection": {
			"ClusterId": 1,
			"Endpoint": "Unspecified/localhost:27017",
			"LocalConnectionId": 3,
			"ServerConnectionId": 55
		},
		"OperationId": 1,
		"CommandName": "insert",
		"Body": {
			"insert": "MongoClient",
			"ordered": true,
			"$db": "AuditTest",
			"lsid": {
				"id": "9498dc51-935d-4e3d-9fc0-0031d993059d"
			},
			"documents": [
				{
					"_id": "6574dcbbda3ab8f0437d1c75",
					"test": "this is a test document"
				}
			]
		},
		"Duration": 4,
		"Success": true,
		"Reply": {
			"n": 1,
			"ok": 1.0
		},
		"Timestamp": "2023-12-09T21:31:40.1286166Z"
	},
	"EventType": "3c18aa76-91cb-4c89-b575-342c9158cb44",
	"Environment": {
		"UserName": "Federico Colombo",
		"MachineName": "DESKTOP-ILAR98A",
		"DomainName": "DESKTOP-ILAR98A",
		"CallingMethodName": "Audit.MongoClient.MongoAuditEventSubscriber.Handle()",
		"AssemblyName": "Audit.MongoClient, Version=22.0.2.0, Culture=neutral, PublicKeyToken=null",
		"Culture": "en-US"
	},
	"StartDate": "2023-12-09T21:31:40.1210357Z",
	"EndDate": "2023-12-09T21:31:40.1370355Z",
	"Duration": 16
}
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)