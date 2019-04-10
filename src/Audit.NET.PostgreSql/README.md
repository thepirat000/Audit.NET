# Audit.NET.PostgreSql
**PostgreSQL Server provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a PostgreSQL Table, in JSON format.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.PostgreSql
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.PostgreSql.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.PostgreSql/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.PostgreSql.svg)](https://www.nuget.org/packages/Audit.NET.PostgreSql/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the PostgreSQL data provider, or call the `UsePostgreSql` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new PostgreSqlDataProvider()
{
    ConnectionString = "Server=127.0.0.1;Port=5432;User Id=admin;Password=admin;Database=postgres;",
    TableName = "event",
    IdColumnName = "id",
    DataColumnName = "data",
    DataType = "JSONB",
    LastUpdatedDateColumnName = "updated_date",
    CustomColumns = new List<CustomColumn>()
    {
        new CustomColumn("event_type", ev => ev.EventType),
        new CustomColumn("user", ev => ev.Environment.UserName),
    }
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UsePostgreSql(config => config
        .ConnectionString("Server=127.0.0.1;Port=5432;User Id=admin;Password=admin;Database=postgres;")
        .TableName("event")
        .IdColumnName("id")
        .DataColumn("data", DataType.JSONB)
        .LastUpdatedColumnName("updated_date")
        .CustomColumn("event_type", ev => ev.EventType)
        .CustomColumn("user", ev => ev.Environment.UserName));
```

### Provider Options

Mandatory:
- **ConnectionString**: The PostgreSQL Server connection string.
- **TableName**: The events table name.
- **DataColumnName**: The column name of the event table where the JSON will be stored.
- **IdColumnName**: The column name of the event identifier (the primary key).

Optional:
- **Schema**: The PostgreSQL schema to use.
- **DataType**: The type of the data column that stores the events. Can be JSON, JSONP or STRING. (Default is JSON).
- **LastUpdatedDateColumnName**: The datetime column name to update when replacing events.
- **CustomColumn**: Additional columns to store information from the audit event. (optional)

## Table constraints

- The table should exists. 
- The table should have a single ID column (Unique or Primary key).
- The type of the ID column should be convertible to `STRING`.

For example:
```SQL
CREATE TABLE public.event
(
    id bigserial NOT NULL,
    inserted_date timestamp without time zone NOT NULL DEFAULT now(),
    updated_date timestamp without time zone NOT NULL DEFAULT now(),
    data jsonb NOT NULL,
    event_type varchar(50),
    user varchar(50) NULL,
    CONSTRAINT event_pkey PRIMARY KEY (id)
)
WITH (
    OIDS = FALSE
)
TABLESPACE pg_default;
```

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = dataProvider.GetEvent(1000);
```

The Postgre SQL data provider also includes basic support for querying the events collection.

Use the `EnumerateEvents()` method on `PostgreSqlDataProvider` class to run SQL-like queries against the audit events. The `EnumerateEvents()` method accepts any valid Postgre WHERE clause as a parameter.

For example:
```c#
IEnumerable<AuditEvent> events = postgreDataProvider.EnumerateEvents(
       "data #> '{Environment,UserName}' = '\"John\"'");
```

Will return the events whose property `Environment.UserName` is equal to 'John'.

This [post](http://schinckel.net/2014/05/25/querying-json-in-postgres/) contains information about the query syntax supported by JSONP data type.
And [here](https://www.postgresql.org/docs/9.4/static/functions-json.html) is the PostgreSQL documentation about JSON operators.

For complex querying capabilities, you should use the [npgsql driver](https://www.nuget.org/packages/Npgsql/) or the [Npgsql EntityFramework provider](http://www.npgsql.org/efcore/) directly.


