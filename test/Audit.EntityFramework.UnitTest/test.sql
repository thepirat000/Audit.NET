--create database Blogs
--create database ParentChild

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



insert into blogs (Title, BloggerName)
values ('test', 'fdcg')