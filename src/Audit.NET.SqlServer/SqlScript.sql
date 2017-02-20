CREATE DATABASE [Audit]
GO
USE [Audit]
GO
CREATE TABLE [Event]
(
	Id BIGINT IDENTITY(1,1) NOT NULL,
	InsertedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	LastUpdatedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	[Data] NVARCHAR(MAX) NOT NULL,
	CONSTRAINT PK_Event PRIMARY KEY (Id)
)
GO

CREATE VIEW dbo.[v_Event] WITH SCHEMABINDING
AS
SELECT Id, 
	InsertedDate,
	LastUpdatedDate,
	CAST(JSON_VALUE(Data, '$.EventType') AS NVARCHAR(255)) AS [EventType],
	CAST(JSON_VALUE(Data, '$.ReferenceId') AS NVARCHAR(255)) AS [ReferenceId],
	JSON_VALUE(Data, '$.Environment.Exception') As [Exception],
	JSON_VALUE(Data, '$.Environment.MachineName') AS [MachineName],
	CAST(JSON_VALUE(Data, '$.Environment.UserName') AS NVARCHAR(50)) AS [UserName],
	JSON_VALUE(Data, '$.Environment.CallingMethodName') AS [CallingMethodName],
	CAST(JSON_VALUE(Data, '$.StartDate') AS NVARCHAR(35)) AS [CreatedDate],
	CAST(JSON_VALUE(Data, '$.EndDate') AS NVARCHAR(35)) As [CommitDate],
	CAST(JSON_VALUE(Data, '$.Duration') AS INT) As [Duration],
	JSON_VALUE(Data, '$.Target.Type') As [TargetType],
	COALESCE(JSON_VALUE(Data, '$.Target.Old'), JSON_QUERY(Data, '$.Target.Old')) AS [TargetOld],
	COALESCE(JSON_VALUE(Data, '$.Target.New'), JSON_QUERY(Data, '$.Target.New')) AS [TargetNew],
	JSON_QUERY(Data, '$.Comments') AS [Comments],
	[Data] As [Data]
FROM dbo.[Event]
GO

CREATE UNIQUE CLUSTERED INDEX PK_V_EVENT ON [v_Event] (Id)
CREATE INDEX IX_V_EVENT_EventType_ReferenceId ON [v_Event] (EventType, ReferenceId)
CREATE INDEX IX_V_EVENT_CreatedDate ON [v_Event] (CreatedDate)
CREATE INDEX IX_V_EVENT_CommitDate ON [v_Event] (CommitDate)
GO
