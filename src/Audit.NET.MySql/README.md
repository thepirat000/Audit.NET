# Audit.NET.MySql
**MySQL provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a MySQL Table.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.MySql
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.MySql.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.MySql/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.MySql.svg)](https://www.nuget.org/packages/Audit.NET.MySql/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the MySQL data provider, or call the `UseMySql` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new MySqlDataProvider()
{
    ConnectionString = "Server=localhost; Database=events; Uid=admin; Pwd=admin;",
    TableName = "events",
    IdColumnName = "event_id",
    JsonColumnName = "data",
    CustomColumns = new List<CustomColumn>()
    {
        new CustomColumn("user", ev => ev.Environment.UserName)
    }
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseMySql(config => config
        .ConnectionString("Server=localhost; Database=events; Uid=admin; Pwd=admin;")
        .TableName("events")
        .IdColumnName("event_id")
        .JsonColumnName("data")
        .CustomColumn("user", ev => ev.Environment.UserName);
```

### Provider Options

- **ConnectionString**: The MySQL connection string.
- **TableName**: The table name that stores the audit events.
- **IdColumnName**: The column name of the event identifier (the primary key column name).
- **JsonColumnName**: The column name of the event table where the event JSON will be stored. (optional)
- **CustomColumn**: Additional columns to store information from the audit event. (optional)

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id 

```c#
var event = mySqlDataProvider.GetEvent(1000);
```

## Table constraints

- The table should exists. 
- The table should have a single ID column (Primary key).

For example:
```SQL
CREATE TABLE event
(
	id INT unsigned NOT NULL AUTO_INCREMENT,
	inserted_date DATETIME DEFAULT CURRENT_TIMESTAMP,
	last_updated_date DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	data JSON NOT NULL,
    	user VARCHAR(20) NULL,
	PRIMARY KEY (id)
);
```
