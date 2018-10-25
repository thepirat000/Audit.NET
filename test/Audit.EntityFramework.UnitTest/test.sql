--create database Blogs
--create database ParentChild
Use [Audit]

drop table [OrderLineAudit]
drop table [OrderAudit]
drop table [OrderLine]
drop table [Order]
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

use ParentChild

drop table child; drop table parent;

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

use Blogs

Drop Table Posts 

Drop Table Blogs

Create table Blogs
(
	Id int identity(1,1) not null primary key,
	Title nvarchar(25),
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

Drop Proc Insert_Blog
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

Drop Proc Update_Blog
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



insert into blogs (Title, BloggerName)
values ('test', 'fdcg')