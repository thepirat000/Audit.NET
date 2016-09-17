# Audit.NET.SqlServer
**Sql Server provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET)** (An extensible framework to audit executing operations in .NET).

Store the audit events in a SQL Table, in JSON format.

## Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.SqlServer/)**
```
PM> Install-Package Audit.NET.SqlServer
```

## Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

##Configuration
Set the static `Audit.Core.Configuration.DataProvider` property to set the Sql Server data provider, or call the `UseSqlServer` method on the fluent configuration. This should be done before any `AuditScope` creation, i.e. during application startup.

For example:
```c#
Audit.Core.Configuration.DataProvider = new SqlDataProvider()
{
    ConnectionString =
        "data source=localhost;initial catalog=Audit;integrated security=true;",
    TableName = "Event",
    IdColumnName = "EventId",
    JsonColumnName = "Data",
    LastUpdatedDateColumnName = "LastUpdatedDate"
};
```

Or by using the [fluent configuration API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api):
```c#
Audit.Core.Configuration.Setup()
    .UseSqlServer(config => config
        .ConnectionString("data source=localhost;initial catalog=Audit;integrated security=true;")
        .TableName("Event")
        .IdColumnName("EventId")
        .JsonColumnName("Data")
        .LastUpdatedColumnName("LastUpdatedDate"));
```

### Provider Options

Mandatory:
- **ConnectionString**: The SQL Server connection string.
- **TableName**: The events table name.
- **JsonColumnName**: The column name of the event table where the JSON will be stored.
- **IdColumnName**: The column name of the event identifier (the primary key).

Optional:
- **LastUpdatedDateColumnName**: The datetime column name to update when replacing events.

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
	[Data] NVARCHAR(MAX) NOT NULL,
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
	CAST(JSON_VALUE(Data, '$.EventType') AS NVARCHAR(255)) AS [EventType],
	CAST(JSON_VALUE(Data, '$.ReferenceId') AS NVARCHAR(255)) AS [ReferenceId],
	CAST(JSON_VALUE(Data, '$.Environment.UserName') AS NVARCHAR(50)) AS [UserName],
	JSON_VALUE(Data, '$.Target.Type') As [TargetType],
	COALESCE(JSON_VALUE(Data, '$.Target.Old'), JSON_QUERY(Data, '$.Target.Old')) AS [TargetOld],
	COALESCE(JSON_VALUE(Data, '$.Target.New'), JSON_QUERY(Data, '$.Target.New')) AS [TargetNew],
	JSON_QUERY(Data, '$.Comments') AS [Comments],
	[Data] As [Data]
FROM dbo.[Event]
GO

CREATE UNIQUE CLUSTERED INDEX PK_V_EVENT ON [v_Event] (EventId)
GO
CREATE INDEX IX_V_EVENT_EventType_ReferenceId ON [v_Event] (EventType, ReferenceId)
GO
```
