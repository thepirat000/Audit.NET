# Audit.NET.AmazonQLDB
**Amazon Amazon QLDB provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in Amazon QLDB tables using the [Amazon.QLDB.Driver](https://www.nuget.org/packages/amazon.qldb.driver) library.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.AmazonQLDB
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.AmazonQLDB.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.AmazonQLDB/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.AmazonQLDB.svg)](https://www.nuget.org/packages/Audit.NET.AmazonQLDB/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Amazon QLDB data provider, or call the `UseAmazonQLDB` 
method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.


For example:
```c#
Audit.Core.Configuration.DataProvider = new AmazonQldbDataProvider()
{
    QldbDriver = Lazy<IQldbDriver>(() => QldbDriver.Builder()
                    .WithLedger("AuditEvents")
                    .Build()),
    TableNameBuilder = ev => "MyTable"
};
```

Or even shorter using the constructor overload that accepts a fluent API:

```c#
Audit.Core.Configuration.DataProvider = new AmazonQldbDataProvider(config => config
    .WithQldbDriver(QldbDriver.Builder()
                    .WithLedger("AuditEvents")
                    .Build())
    .Table("MyTable"));
```


Or by using the global setup extension `UseAmazonQldb()`:
```c#
Audit.Core.Configuration.Setup()
    .UseAmazonQldb(config => config
        .WithQldbDriver(QldbDriver.Builder()
            .WithLedger("AuditEvents")
            .Build())
        .Table("MyTable"));
```

> You can provide the table name setting as a string or as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage).


### Provider Options

- **QldbDriver**: The Amazon Qldb driver creator `QldbDriver`. 
- **TableNameBuilder**: A function of the audit event that returns the Table Name to use.
- **CustomAttributes**: A dictionary with additional fields to be included in the document and as custom fields on the audit event.

### Fluent API Methods

The provider options can be set with a fluent API described by the following methods:

##### Connection level
- **WithQldbDriver()**: Use the given AmazonQLDB driver instance (`QldbDriver`).
- chaining **UseXXX()** as an alternative to `WithQldbDriver()` for having the possibility to build a QldbDriver with one or more options
    - **UseLedger()** - use a driver with the given ledger name
    - **UseAwsCredentials()** - use a driver with the given AWS credentials
    - **UseLogger()** - use a driver with the given logger
    - **UseQldbSessionConfig()** - use a driver with the given session config
    - **UseRetryLogging()** - use a driver with logging on retry

##### Table level
- **Table()**: To specify the table name (as a string or a function of the audit event).

##### Attributes level
- **SetAttribute()**: To specify additional top-level attributes on the document before saving.

```c#
 Audit.Core.Configuration.Setup()
    .UseAmazonQldb(config =>
    {
        config
            .UseLedger("AuditEvents")
            .UseQldbSessionConfig(new AmazonQLDBSessionConfig
            {
                AllowAutoRedirect = true
            })
            .UseMaxConcurrentTransactions(5)
            .And
            .Table(ev => ev.EventType)
            .SetAttribute("Source", ev => "Production");
    });
```

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = AmazonQldbDataProvider.GetEvent();
```

> The `eventId` parameter on the generic `GetEvent(object eventId)` must be of type (ValueTuple`<`string, string`>`), 
> The first element must be the documentId, and the second element should be the table name.

```c#
// Get event with the given documentId and table name
var event = AmazonQldbDataProvider.GetEvent(("GpIvgdXq44f7QRuLLgldYJ", "AuditEvents"));
```

--------

## Constraints

This provider has the following constraints:

- The ledger to store the audit events must exists on AmazonQLDB. 
(Note you can add properties to the AuditEvent as [Custom Fields](https://github.com/thepirat000/Audit.NET#custom-fields-and-comments) with the `SetAttribute()` method on the provider configuration)