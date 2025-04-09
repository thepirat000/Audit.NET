# Audit.NET.RavenDB
**Raven DB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in Raven DB collections using the [RavenDB.Client](https://www.nuget.org/packages/RavenDB.Client/) library.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.RavenDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.RavenDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.RavenDB/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.RavenDB.svg)](https://www.nuget.org/packages/Audit.NET.RavenDB/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Raven DB data provider, or call the `UseRavenDB` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:

```c#
Audit.Core.Configuration.DataProvider = new RavenDbDataProvider(config => config
    .WithSettings("http://127.0.0.1:8080", "AuditEvents"));
```

Or using the constructor overload that accepts a fluent API:

```c#
Audit.Core.Configuration.DataProvider = new RavenDbDataProvider(config => config
    .WithSettings(settings => settings
        .Urls("http://127.0.0.1:8080")
        .DatabaseDefault("AuditEvents")));
```

Or by using the global setup extension `UseRavenDB()` that also accepts the fluent API:

```c#
Audit.Core.Configuration.Setup()
    .JsonNewtonsoftAdapter()
    .UseRavenDB(config => config
        .WithSettings(settings => settings
            .Urls("http://127.0.0.1:8080")
            .Database(ev => "Audit_" + ev.EventType)
            .Certificate(cert)));
```

> You can provide the database name setting as function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).

You can also provide an instance of a `IDocumentStore` already configured. For example:

```c#
Audit.Core.Configuration.Setup()
    .UseRavenDB(config => config
        .UseDocumentStore(new DocumentStore()
        {
            Urls = new[] {"http://127.0.0.1:8080"},
            Database = "AuditEvents",
            Conventions = new DocumentConventions()
            {
                Serialization = new NewtonsoftJsonSerializationConventions()
                {
                    JsonContractResolver = new AuditContractResolver(),
                    CustomizeJsonSerializer = ser =>
                    {
                        ser.DefaultValueHandling = DefaultValueHandling.Ignore;
                    }
                }
            }
        }));
```

**IMPORTANT NOTES**: 

- The Raven DB C# Client depends on `Newtonsoft.Json` for serialization and deserialization, so if you are 
targeting .NET 5.0 or higher, it is highly recommended to set up the global [Newtonsoft JsonAdapter](https://github.com/thepirat000/Audit.NET#custom-serialization-mechanism) 
by adding the `.JsonNewtonsoftAdapter()` call on your startup. For example:

    ```c#
    Audit.Core.Configuration.Setup()
        .JsonNewtonsoftAdapter()
        .UseRavenDB(...);
    ```

- This data provider uses a specialized [Contract Resolver](https://www.newtonsoft.com/json/help/html/contractresolver.htm) (`AuditContractResolver`) that honors the decorations from `System.Text.Json` but still using 
`Newtonsfot.Json` as required by the Raven DB client.
This `IContractResolver` is added by default to the client conventions when using `.WithSettings(...)` fluent API.

### Provider Settings

- **Urls**: The RavenDB server URLs for the initial connection. 
- **DatabaseDefault**: The name of the default database to use.
- **Certificate**: The certificate for secure connection.
- **DatabaseFunc**: A function of the `AuditEvent` that returns the name of the database to store the audit event.

### Provider Properties
- **DocumentStore**: Gets ot sets the instance of the document store being used.

### Fluent API Methods

The provider options can be set with a fluent API described by the following methods:

##### Connection level
- **WithSettings()**: Use the given RavenDB settings.
- **UseDocumentStore()**: Alternative to use the given RavenDB document store instance (`IDocumentStore`).

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = ravenDataProvider.GetEvent("AuditEvents/3201-A");
```

--------

## Constraints

This provider has the following constraints:

- The table to store the audit events must exists on RavenDB. 

To create a database from a Raven DB Data Provider instance you can use the `DocumentStore` property, for example:

```c#
ravenProvider.DocumentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord("AuditEvents")));
```

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)