# Audit.NET.SqlServer
**Sql Server provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a SQL Table, in JSON format.

## Install

**NuGet Package** 
To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.NET.SqlServer
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.NET.SqlServer.svg?style=flat)](https://www.nuget.org/packages/Audit.NET.SqlServer/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.NET.SqlServer.svg)](https://www.nuget.org/packages/Audit.NET.SqlServer/)

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

## Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Sql Server data provider, or call the `UseSqlServer` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new SqlDataProvider()
{
    ConnectionString = "data source=localhost;initial catalog=Audit;integrated security=true;",
    Schema = "dbo",
    TableName = "Event",
    IdColumnName = "EventId",
    JsonColumnName = "JsonData",
    LastUpdatedDateColumnName = "LastUpdatedDate",
    CustomColumns = new List<CustomColumn>()
    {
        new CustomColumn("EventType", ev => ev.EventType)
    }
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseSqlServer(config => config
        .ConnectionString("data source=localhost;initial catalog=Audit;integrated security=true;")
        .Schema("dbo")
        .TableName("Event")
        .IdColumnName("EventId")
        .JsonColumnName("JsonData")
        .LastUpdatedColumnName("LastUpdatedDate")
        .CustomColumn("EventType", ev => ev.EventType)        
        .CustomColumn("User", ev => ev.Environment.UserName));
```

You can provide any of the settings as a function of the [Audit Event](https://github.com/thepirat000/Audit.NET#usage), 
for example to use a connection string per machine, and different table names:

```c#
Audit.Core.Configuration.Setup()
    .UseSqlServer(config => config
        .ConnectionString(ev => GetCnnString(ev.Environment.MachineName))
        .TableName(ev => ev.EventType == "Order" ? "OrderAudits" : "Audits"));
```


### Provider Options

- **ConnectionString**: The SQL Server connection string.
- **DbConnection**: The `DbConnection` to use, alternative to ConnectionString when you need more control or configuration over the DB connection. (optional, only for .NET Framework) 
- **Schema**: The SQL schema for the table. (optional)
- **TableName**: The audit events table name.
- **IdColumnName**: The column name of the event identifier (the primary key).
- **JsonColumnName**: The column name of the event table where the audit event JSON will be stored. (optional)
- **LastUpdatedDateColumnName**: The datetime column name to update when replacing events. (optional)
- **CustomColumn**: Additional columns to store information from the audit event. (optional)
- **SetDatabaseInitializerNull**: To set the database initializer to NULL on the internal DbContext. (optional, only for .NET Framework)
- **DbContextOptions**: To set custom options for the internal EntityFramework DbContext (optional, only for .NET Core)

## Query events

This provider implements `GetEvent` and `GetEventAsync` methods to obtain an audit event by id:

```c#
var event = sqlDataProvider.GetEvent(1000);
```

## Table constraints

- The table should exists. 
- The table should have a single ID column (Unique or Primary key).
- The type of the ID column should be convertible to `NVARCHAR`.

For example:
```SQL
CREATE TABLE [Event]
(
    [EventId] BIGINT IDENTITY(1,1) NOT NULL,
    [InsertedDate] DATETIME NOT NULL DEFAULT(GETUTCDATE()),
    [LastUpdatedDate] DATETIME NULL,
    [JsonData] NVARCHAR(MAX) NOT NULL,
    [EventType] NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Event PRIMARY KEY (EventId)
)
GO
```

If you use Azure SQL Server or Sql Server 2016, you can create indexes on the JSON fields, for example creating a schemabinded view:

```SQL
CREATE VIEW dbo.[v_Event] WITH SCHEMABINDING
AS
SELECT EventId, 
    InsertedDate,
    CAST(JSON_VALUE(JsonData, '$.EventType') AS NVARCHAR(255)) AS [EventType],
    CAST(JSON_VALUE(JsonData, '$.ReferenceId') AS NVARCHAR(255)) AS [ReferenceId],
    JSON_VALUE(JsonData, '$.Target.Type') As [TargetType],
    COALESCE(JSON_VALUE(JsonData, '$.Target.Old'), JSON_QUERY(JsonData, '$.Target.Old')) AS [TargetOld],
    COALESCE(JSON_VALUE(JsonData, '$.Target.New'), JSON_QUERY(JsonData, '$.Target.New')) AS [TargetNew],
    JSON_QUERY(JsonData, '$.Comments') AS [Comments],
    [JsonData]
FROM dbo.[Event]
GO

CREATE UNIQUE CLUSTERED INDEX PK_V_EVENT ON [v_Event] (EventId)
GO
CREATE INDEX IX_V_EVENT_EventType_ReferenceId ON [v_Event] (EventType, ReferenceId)
GO
```
