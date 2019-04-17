# Changelog
All notable changes to Audit.NET and its extensions will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## [14.2.3] - 2019-04-17
### Modified
- Audit.WebApi and Audit.WebApi.Core: Ignoring action parameters marked with `[FromServicesAttribute]` (#213).

## [14.2.2] - 2019-04-10
### Added
- Audit.NET.PostgreSql: Adding ability to configure extra columns on the audit SQL table with `CustomColumn` fluent API.

## [14.2.1] - 2019-04-05
### Added
- Audit.NET: Adding ability to re-assign the AuditEvent's Target object after the scope is created.

## [14.2.0] - 2019-03-16
### Added
- Audit.NET.SqlServer: Adding ability to configure extra columns on the audit SQL table with `CustomColumn` fluent API, and making JsonColumn optional.

## [14.1.1] - 2019-03-07
### Modified
- Audit.EntityFramework: Fix race condition on EntityKeyHelper when obtaining the column names (#208)

## [14.1.0] - 2019-02-11
### Added
- Audit.NET.AzureStorage: Added Azure Active Directory Authentication support via Access Token for the BLOB storage Data Provider.

## [14.0.4] - 2019-01-31
### Modified
- Audit.EntityFramework: Fixed #198, adding validation to avoid exception when multiple mapping fragments are found.

## [14.0.3] - 2019-01-21
### Added
- Audit.AzureStorage: Added constructor overloads to `AzureTableDataProvider` and `AzureBlobDataProvider` that accepts a fluent configuration. Useful for custom providers inheriting from those classes.

## [14.0.2] - 2018-12-15
### Added
- Audit.WebApi and Audit.WebApi.Core: Added Response Headers to the event output (optional by `IncludeResponseHeaders` configuration, default is _false_). 
- Audit.Core: Added optional Custom Fields to `AuditEvent.Environment`.

## [14.0.1] - 2018-11-28
### Added
- Audit.EntityFramework and Audit.EntityFramework.Core: Added Schema property on entries to complement table name (#182).
Added optional Custom Fields to Entity Framework Event and Event.Entries.
- Audit.NET.MySql: Changed target frameworks to match those on MySqlConnector nuget version 0.47.1.

## [14.0.0] - 2018-11-19
### Modified
- Audit.Core: UTC standarization for dates: changing missing dates to be UTC (Event.StartDate, Event.EndDate).
- Audit.Core: Added `Configuration.Setup().Use()` shortcut method for `UseCustomProvider()` and `UseDynamicProvider()`.

## [13.3.0] - 2018-11-16
### Modified
- Audit.Core: Adding support to Xamarin/Mono. Fix incompatible calls to `System.Runtime.InteropServices.Marshal`. (#180)

## [13.2.2] - 2018-11-15
### Modified
- Audit.EntityFramework.Core: Fix bug when auditing tables with composite multiple froeign keys related to the same column (#178).

## [13.2.1] - 2018-11-12
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Fix parameter default for `IgnoreMatchedProperties(bool)` on configuration API to be _true_.


## [13.2.0] - 2018-10-31
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Allow mapping multiple entity types to the same audit type with independent actions. (#175)

## [13.1.5] - 2018-10-30
### Modified
- Audit.PostgreSQL: Fix casing for schema name, allowing case-sensitive schemas. (#174)

## [13.1.4] - 2018-10-25
### Modified
- Audit.MongoDB: Fix bug when deserializing custom fields (#173)

## [13.1.3] - 2018-10-17
### Modified
- Audit.WebApi and Audit.Mvc: Upgrading AspNetCore libraries from GitHub suggestion because of security vulnerability.
- Audit.AzureDocumentDB: Adding netstandard2.0 as target

## [13.1.2] - 2018-09-12
### Modified
- Audit.WebApi.Core: (#158) Moving `GetAuditScope` extension method to `ControllerBase` instead of `Controller`.
- Audit.Template.WebApi and Audit.Template Mvc: Using the built-in extension for registering a IHttpContextAccessor (#160)
- Audit.WebApi.Core: (Fix bug #161) Empty http response when using IncludeResponseBody on both the middleware and the audit filter.

## [13.1.1] - 2018-09-10
### Modified
- Audit.WebApi / Audit.WebApi.Core: Have request/response bodies retrieval mutually exclusive between ActionFilter and MiddleWare.
- Audit.WebApi.Template: Adding middleware to webapi default template. 

## [13.1.0] - 2018-09-10
### Added
- Audit.WebApi / Audit.WebApi.Core: Added new middleware to complement the action filter audits and be able to log any request regardless
if an action is reached or not.


## [13.0.0] - 2018-08-30
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Created new nuget packages `Audit.EntityFramework.Identity` and `Audit.EntityFramework.Identity.Core` 
to remove the AspNet.Identity dependency.


## [12.3.6] - 2018-08-29
### Added
- Audit.NET.DynamoDB: Adding new extension **Audit.NET.DynamoDB** to save audit events into Amazon DynamoDB using the `AWSSDK.DynamoDBv2` document model.

### Modified
- Audit.Core: Adding GetXxxxxEvent() extensions to AuditScope in addition to the AuditEvent.
- Audit.EntityFramework: Added validation on EF Data Provider to ignore non EF events.

## [12.3.5] - 2018-08-22
### Modified
- Audit.EntityFramework: Fix #150: Race condition on Audit.EF .NET Framework version, when multiple threads are calling EF SaveChanges. 


## [12.3.4] - 2018-08-21
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Allow setting up a different DbContext for storing the audit events on the EF Data Provider. Related to #148.


## [12.3.3] - 2018-08-21
### Modified
- Audit.WebApi: Fix bug #147 for Microsoft.AspNet.WebApi.Versioning package compatibility.

## [12.3.2] - 2018-08-20
### Modified
- Audit.EntityFramework.Core: Replace Microsoft.EntityFrameworkCore.SqlServer dependency in Audit.EntityFramework.Core with Microsoft.EntityFrameworkCore.Relational. (thanks to https://github.com/Deilan).


## [12.3.1] - 2018-08-20
### Modified
- Audit.WebApi: Fix bug #146 on IsActionIgnored for .NET framework WebApi.


## [12.3.0] - 2018-08-20
### Modified
- Audit.Core: Changing the exception serialization to include the stacktrace and inner exceptions.

## [12.2.2] - 2018-08-15
### Modified
- Audit.WebApi, Audit.WebApi.Core: Adding AuditIgnoreAttribute for controller, actions and arguments.
- Audit.EntityFramework: Adding DefaultAuditContext and documentation on readme.md about using the library without inheritance.

## [12.2.1] - 2018-08-09
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Removing SqlServer.Design dependency

## [12.2.0] - 2018-08-08
### Added
- Audit.NET: Added FileDataProvider constructor overload with fluent API.
- Audit.NET.SqlServer: Added SqlDataProvider constructor overload with fluent API.
- Audit.NET.PostgreSql: Added PostgreSqlDataProvider constructor overload with fluent API.
- Audit.NET.MySql: Added MySqlDataProvider constructor overload with fluent API.
- Audit.NET.MongoDB: Added MongoDataProvider constructor overload with fluent API.
- Audit.NET.log4net: Added Log4netDataProvider constructor overload with fluent API.
- Audit.NET.ElasticSearch: Added ElasticSearchDataProvider constructor overload with fluent API.


## [12.1.11] - 2018-07-30
### Added
- Audit.WebApi, Audit.WebApi.Core and Audit.Mvc.Core: Added `TraceId` field on the output, with the internal ASP.NET correlation id per request.
- Added Audit.WebApi.Template dotnet new template.

## [12.1.10] - 2018-07-20
### Added
- Audit.EntityFramework and Audit.EntityFramework.Core: Adding support for ambient transactions (i.e. TransactionScope) on EF Core 2.1. Added AmbientTransactionId field to EF event output.

## [12.1.9] - 2018-07-10
### Added
- Audit.NET.AzureStorage: Adding new data provider for storing events on **Azure Tables** `AzureTableDataProvider`.

## [12.1.8] - 2018-07-01
### Modified
- Audit.EntityFramework: Fix #127: (For EF 6) Foreign keys are set to NULL when deleting a relation
entity (many-to-many), this was making NULL the column values holding the foreign key. Adding
a workaround to avoid updating the foreign column values from the foreign key values that are set to NULL by EF.

## [12.1.7] - 2018-06-06
### Modified
- Audit.WebApi and Audit.WebApi.Core: Fix #131. Swallow `InvalidDataException` when accessing the Request.Form getter and return NULL is case of that type of exception.
Only for the asp.net core version

## [12.1.6] - 2018-06-04
### Modified
- Audit.NET.Udp: Fix #129. Allow specifying host name on the Udp Configuration as an alternative to the IP address. 

## [12.1.5] - 2018-06-02
### Added
- Audit.NET.EventLog.Core: Created this new assembly/package to output events to the windows eventlog when targeting net core 2.0.
 
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Fix #128 to be compatible with new EF Core 2.1 lazy loading feature.
- Audit.NET: Removing Microsoft.Windows.Compatibility dependency from Audit.NET package

## [12.1.4] - 2018-05-25
### Added
- Audit.Mvc and Audit.Mvc.Core: Adding request and response bodies to the logs, optional via IncludeRequestBody and IncludeResponseBody properties on the action filter attribute.

## [12.1.3] - 2018-05-15
### Modified
- Audit.NET.AzureDocumentDB: Fix #126: AzureDocumentDb not respecting the global JsonSettings.

## [12.1.2] - 2018-05-15
### Modified
- Audit.Core: Fix bug #126: FileDataProvider not respecting JsonSettings. Exposed JsonSettings as a property of the FileDataProvider and fixed async methods.

## [12.1.1] - 2018-05-14
### Modified
- Audit.EntityFramework: Fix bug on SaveAsync for EntityFrameworkDataProvide. Related to #122.

## [12.1.0] - 2018-05-08
### Added
- Audit.WebApi: Adding `AuditApiGlobalFilter` a configurable global filter as an alternative
to decorate the controllers with `AuditApiAttribute`.

## [12.0.7] - 2018-05-05
### Modified
- Audit.WebApi: Adding `IncludeResponseBodyFor` and `ExcludeResponseBodyFor` property on `AuditApiAttribute` 
to allow conditionally Including/Excluding the Response Body from the log, only when certain Http Status codes are returned.


## [12.0.6] - 2018-05-04
### Modified
- Audit.Core: Adding `AuditDisabled()` method to fluent configuration API.
- Audit.WebApi: Bypassing the filter when audit is globally disabled.
- Audit.Mvc: Bypassing the filter when audit is globally disabled.

## [12.0.5] - 2018-05-02
### Modified
- Audit.WebApi: Adding context wrapper interface IContextWrapper and injection mechanism for Audit.WebApi on full framework. Related to #124.

## [12.0.4] - 2018-04-30
### Modified
- Audit.NET.AzureDocumentDB: Allowing builders to pass the connection string, database and collection.
- Audit.NET.Elasticsearch: Fixing project URL reference on nuget package.

## [12.0.3] - 2018-04-30
### Added
- Audit.NET.Elasticsearch: New Elasticsearch data provider

### Modified
- Audit.NET.AzureDocumentDB: Cosmos DB provider enhancements by _ovidiu [AT] ovidiudiaconescu.com_. Caching azure client and allow passing the connection policy.
- Audit.WebApi: (#124) Making GetRequestBody protected virtual

## [12.0.2] - 2018-04-27
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
- Audit.MySql: refactor by _bgrainger [AT] gmail.com_ to use MySqlConnector instead of MySql.Data to support real async calls.

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


