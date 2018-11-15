CREATE DATABASE Demo
GO
USE [Demo]
GO

CREATE TABLE [dbo].[Tenant](
[Id] [int] NOT NULL,
[Name] varchar(100) NOT NULL,
CONSTRAINT [PK_Tenant] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Employee](
[Id] [int] NOT NULL,
[TenantId] [int] NOT NULL,
[Name] varchar(100) NOT NULL,
CONSTRAINT [PK_Employee] PRIMARY KEY CLUSTERED
(
[Id] ASC,
[TenantId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Employee] WITH CHECK ADD CONSTRAINT [FK_Employee_Tenant] FOREIGN KEY([TenantId])
REFERENCES [dbo].[Tenant] ([Id])
GO

ALTER TABLE [dbo].[Employee] CHECK CONSTRAINT [FK_Employee_Tenant]
GO

CREATE TABLE [dbo].[PettyCashTransaction](
[Id] [int] NOT NULL,
[EmployeeId] [int] NULL,
[TrusteeId] [int] NULL,
[TenantId] [int] NULL,
CONSTRAINT [PK_PettyCashTransaction] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PettyCashTransaction] WITH CHECK ADD CONSTRAINT [FK_PettyCashTransaction_Employee] FOREIGN KEY([EmployeeId], [TenantId])
REFERENCES [dbo].[Employee] ([Id], [TenantId])
GO

ALTER TABLE [dbo].[PettyCashTransaction] CHECK CONSTRAINT [FK_PettyCashTransaction_Employee]
GO

ALTER TABLE [dbo].[PettyCashTransaction] WITH CHECK ADD CONSTRAINT [FK_PettyCashTransaction_EmployeeTrustee] FOREIGN KEY([TrusteeId], [TenantId])
REFERENCES [dbo].[Employee] ([Id], [TenantId])
GO

ALTER TABLE [dbo].[PettyCashTransaction] CHECK CONSTRAINT [FK_PettyCashTransaction_EmployeeTrustee]
GO