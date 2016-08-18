# Audit.NET.SqlServer
Sql Server provider for [Audit.NET library](https://github.com/thepirat000/Audit.NET) (A small framework to audit executing operations in .NET).

Store the events as rows in a SQL Table, in JSON format.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.NET.SqlServer/)**
```
PM> Install-Package Audit.NET.SqlServer
```

##Usage
Please see the [Audit.NET Readme](https://github.com/thepirat000/Audit.NET#usage)

##Configuration
Call the static `AuditConfiguration.SetDataProvider` method to set the Sql Server data provider. This should be before to any `AuditScope` creation, i.e. during application startup.

```c#
AuditConfiguration.SetDataProvider(new Audit.SqlServer.Providers.SqlDataProvider()
{
    ConnectionString =
        "data source=localhost;initial catalog=AuditDb;user id=user;password=pass",
    TableName = "Audit"
});
```

##Table constraint

The table should have a column named `[Data]` of type `NVARCHAR(MAX)`. The JSON representation of the events will be saved on that column.

For example:
```SQL
CREATE TABLE [Event]
(
	[EventId] BIGINT IDENTITY(1,1) NOT NULL,
	[InsertedDate] DATETIME NOT NULL DEFAULT(GETUTCDATE()),
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
