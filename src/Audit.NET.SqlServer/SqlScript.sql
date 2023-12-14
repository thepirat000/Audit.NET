CREATE DATABASE [Audit]
GO
USE [Audit]
GO

CREATE TABLE [Event]
(
	EventId BIGINT IDENTITY(1,1) NOT NULL,
	InsertedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	LastUpdatedDate datetimeoffset NOT NULL DEFAULT(GETDATE()),
	EventType NVARCHAR(MAX),
	[Data] NVARCHAR(MAX) NOT NULL,
	CONSTRAINT PK_Event PRIMARY KEY (EventId)
)
GO

SET NUMERIC_ROUNDABORT OFF;
SET ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON;
GO

CREATE VIEW dbo.[v_Event] WITH SCHEMABINDING
AS
SELECT EventId, 
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

CREATE UNIQUE CLUSTERED INDEX PK_V_EVENT ON [v_Event] (EventId)
CREATE INDEX IX_V_EVENT_EventType_ReferenceId ON [v_Event] (EventType, ReferenceId)
CREATE INDEX IX_V_EVENT_CreatedDate ON [v_Event] (CreatedDate)
CREATE INDEX IX_V_EVENT_CommitDate ON [v_Event] (CommitDate)
GO

Create table [Order]
(
	Id BIGINT IDENTITY(1,1) NOT NULL,
	[Number] nvarchar(Max) NULL,
	[Status] nvarchar(Max) NULL,
	CONSTRAINT PK_Order PRIMARY KEY (Id)
)

Create table [OrderLine]
(
	Id BIGINT IDENTITY(1,1) NOT NULL,
	[Product] nvarchar(Max) NULL,
	[Quantity] int NOT NULL,
	[OrderId] BIGINT NOT NULL,
	CONSTRAINT PK_OrderLine PRIMARY KEY (Id),
	Constraint FK_OrderLine_Order Foreign Key ([OrderId]) References [Order](Id)
)

Create table [OrderAudit]
(
	Id BIGINT NOT NULL,
	[Number] nvarchar(Max) NULL,
	[Status] nvarchar(Max) NULL,
	AuditDate datetime NOT NULL,
    AuditStatus nvarchar(Max) NULL,
    UserName nvarchar(Max) NULL,
	CONSTRAINT PK_OrderAudit PRIMARY KEY (AuditDate, Id)
)

Create table [OrderLineAudit]
(
	Id BIGINT NOT NULL,
	[Product] nvarchar(Max) NULL,
	[Quantity] int NOT NULL,
	[OrderId] BIGINT NOT NULL,
	AuditDate datetime NOT NULL,
    AuditStatus nvarchar(Max) NULL,
    UserName nvarchar(Max) NULL,
	CONSTRAINT PK_OrderLineAudit PRIMARY KEY (AuditDate, Id)
)


create database ParentChild
GO
use ParentChild
GO

CREATE TABLE [Parent]
(
	Id BIGINT IDENTITY(1,1) NOT NULL,
	[Name] nvarchar(Max) NOT NULL,
	CONSTRAINT PK_Parent PRIMARY KEY (Id)
)
CREATE TABLE [Child]
(
	Id BIGINT IDENTITY(1,1) NOT NULL,
	[Name] nvarchar(Max) NOT NULL,
	[Period_Start] datetime NOT NULL,
	[Period_End] datetime NOT NULL,
	[ParentId] bigint NOT NULL,
	CONSTRAINT PK_Child PRIMARY KEY (Id),
	Constraint FK_Child_Parent Foreign Key ([ParentId]) References Parent(Id)
)

create database blogs
GO
use Blogs
GO

Create table Blogs
(
	Id int identity(1,1) not null primary key,
	Title nvarchar(max),
	BloggerName nvarchar(max)
)


Create table Posts
(
	Id int identity(1,1) not null primary key,
	Title nvarchar(max),
	DateCreated datetime not null,
	Content nvarchar(max),
	BlogId int not null,
	Constraint FK_Posts_Blog Foreign Key (BlogId) References Blogs(Id)
)
GO

Create Proc Insert_Blog
(
	@Title nvarchar(max),
	@Name nvarchar(max)
)
As
Begin
	insert into blogs (Title, BloggerName)
	values (@Title, @Name)
End
GO

Create Proc Update_Blog
(
	@Id int,
	@Title nvarchar(max),
	@Name nvarchar(max)
)
As
Begin
	Update blogs 
	Set Title = @Title, BloggerName = @Name
	Where id = @Id
End
GO

Create table dbo.BlogsAudits
(
	BlogAuditId int identity(1,1) not null primary key,
	BlogId int not null,
	Title nvarchar(max),
	BloggerName nvarchar(max),
	AuditAction nvarchar(max),
	AuditDate datetime,
	AuditUser nvarchar(max),
	Exception nvarchar(max)
)

Create table dbo.PostsAudits
(
	PostAuditId int identity(1,1) not null primary key,
	Title nvarchar(max),
	DateCreated datetime not null,
	Content nvarchar(max),
	PostId int not null,
	BlogId int not null,
	AuditAction nvarchar(max),
	AuditDate datetime,
	AuditUser nvarchar(max),
	Exception nvarchar(max)
)

insert into blogs (Title, BloggerName)
values ('test', 'fdcg')