# Changelog
All notable changes to Audit.NET and its extensions will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## [12.0.2] - 2018-04-????????????
### Modified
- Audit.EntityFramework: Fix #120 exposing internal properties EventEntry.Entry (GetEntry) and EntityFrameworkEvent.DbContext (GetDbContext) on model objects.
- Audit.EntityFramework: Fix #122 allow exlude entities via the audit entity action. Now the AuditEntityAction can be a Func that return a boolean indicating whether to include the entity.


## [12.0.1] - 2018-04-24
### Added
- Audit.Core: Exposing the global JSON serializer settings as a Configuration option to allow changing the serialization behavior for audit events.
 
## [12.0.0] - 2018-04-22
### Added
- Audit.Core: Added a global audit switch off `Configuration.AuditDisabled`.
- Audit.Core: Added `NullDataProvider` as an anternative to disable the audit logging.


## [11.2.0] - 2018-04-11
### Changed
- Audit.NET.MongoDB: Fix #114 - MongoDB Dataprovider Date serialization. Changing serialization mechanism to store the .NET DateTime as mongo datetime.

## [11.1.0] - 2018-04-08
### Added
- Audit.EntityFramework: Added built-in mechanism to Ignore columns and Override column values on the audit logs.

### Changed
- Audit.EntityFramework: (Core) fix PrimaryKeys, ForeignKeys and ColumnValues to log the column name instead of the property name.


## [11.0.8] - 2018-03-25
### Changed
- Audit.EntityFramework - Fix [#106]: DbEntityValidationException causes AuditEntityAction StackOverflowException.

## [11.0.7] - 2018-03-19
### Changed
- Audit.NET.AzureDocumentDB - Fix [#103]: Added FeedOptions argument to DocumentDb QueryEvents.
- Audit.EntityFramework - Fix [#104]: Multiple foreing key using the same field as key, causing audit to fail.

## [11.0.6] - 2018-03-07
### Changed
- Audit.WebApi and Audit.Mvc - Fix [#102]: NULL validation on HttpContext.Connection.RemoteIpAddress 


## [11.0.5] - 2018-02-18
### Changed
- Audit.WebApi. Fix [#99]: Output not including body value when type was Microsoft.AspNetCore.Mvc.JsonResult

## [11.0.4] - 2018-02-14
### Added
- Audit.WebApi: Added GetCurrentAuditScope(this HttpContext httpContext) extension to get the web api audit scope directly from an HttpContext

### Changed
- Audit.NET.Postgres: Fix the insert command for the Postgres provider (#98)

## [11.0.3] - 2018-02-12
### Added
- Added request body for AspNet Core Web API 2 via IncludeRequestBody property.

## [11.0.2] - 2018-02-09
### Added
- Adding NETSTANDARD2.0 support to Audit.NET 

### Changed
- EventLog data provider available on NETCORE 2.0
- EventLog new MessageBuilder property to allow customizing the logged message
- Audit.DynamicProxy: allow setting the creation policy.
- Fixed [#97](https://github.com/thepirat000/Audit.NET/issues/97): WebAPI missing response body when the response was a type inherited from ObjectResult, etc. 

## [11.0.1] - 2018-01-28
### Changed
- Audit.MySql: refactor by bgrainger@gmail.com to use MySqlConnector instead of MySql.Data to support real async calls.

## [11.0.0] - 2018-01-14
### Added
- Async support to the AuditScope and all the data providers.
- Async support for Audit.EntityFramework, Audit.WCF, Audit.WebAPI and Audit.MVC extensions.
- Data retrieval methods for most of the data providers.
- New Custom Action OnEventSaved triggered right after the event is saved.

## [10.0.3] - 2017-12-28
### Changed
- Audit.EntityFramework: bug fixing [#92](https://github.com/thepirat000/Audit.NET/issues/92)

## [10.0.2] - 2017-12-25
### Added
- New Audit.NET.log4net extension to log events with Apache log4net.


