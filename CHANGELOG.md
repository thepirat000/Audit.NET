# Changelog
All notable changes to Audit.NET and its extensions will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

## [19.4.0] - 2022-09-02
- Audit.EntityFramework / Audit.EntityFramework.Core: Adding overloads to `Format()` and `Override()` methods for Entity Framework config fluent API, to override entity values on the audit output (#526)

## [19.3.0] - 2022-08-22
- Audit.SqlServer: Fixing compatibility with .NET 6 for Audit.SqlServer library (#522)
- Audit.EntityFramework.Core: Adding DbTransaction interceptor to log transaction events (`AuditTransactionInterceptor`).

## [19.2.2] - 2022-08-08
- Audit.EntityFramework.Core: Allow optional logging for SQL read results. New setting for command interceptor `AuditCommandIntercaptor.IncludeReaderResults` (#515)
- Audit.NET: Changing default IgnoreCycles reference handling for JSON serialization on .NET 6 (#517)

## [19.2.1] - 2022-08-06
- Audit.WCF.Client: Adding .NET Core support to Audit.WCF.Client (#514)
- Audit.NET.log4net: Security upgrade log4net from 2.0.10 to 2.0.15 (#511)

## [19.2.0] - 2022-07-23
- Audit.NET.ElasticSearch: Fix issue when pre-serializing the Target object (#509)
- Audit.NET.AzureStorageBlobs: Bump Azure.Storage.Blobs from 12.9.1 to 12.13.0 (#510)
- Audit.NET: Fix pre-serialization method on base AuditDataProvider

## [19.1.4] - 2022-05-22
- Audit.NET.Redis: Adding Redis Stream provider to allow storing the audit events into Redis Streams.

## [19.1.3] - 2022-05-20
- Audit.NET.Redis: Allow setting redis connection configuration via ConfigurationOptions (#493)

## [19.1.2] - 2022-05-18
- Audit.MVC / Audit.MVC.Core: Adding extension methods on HttpContext to get the Audit Scope `Audit.Mvc.GetCurrentAuditScope()` (#492)

## [19.1.1] - 2022-04-28
- Audit.EntityFramework / Audit.EntityFramework.Core: `Changes.NewValue` property on the EF Audit Event was not being updated with the final value from the database 
when the value changed was a foreign key to a new entity (#488)

## [19.1.0] - 2022-04-10
- Audit.NET.JsonNewtonsoftAdapter: New `AuditContractResolver` configured by default on the Newtonsoft.Json adapter to enhance 
compatibility when targeting newer frameworks (.NET >= 5.0) and still using Newtonsoft.Json as the serializer.
- Audit.NET.RavenDB: New data provider `Audit.NET.RavenDB` for storing Audit Events as documents on Raven DB. (#485)

## [19.0.7] - 2022-03-13
- Audit.NET.Elasticsearch: Fix issue using Auto Generated IDs. Change to use an IndexRequest instead of a CreateRequest (#473)

## [19.0.5] - 2022-03-07
- Audit.NET.PostgreSql: Fix issue when setting null to a custom column (#357)

## [19.0.4] - 2022-01-23
- Audit.NET.AzureStorage: Azure Table Storage InsertEventAsync bug fix. Method was using sync version of EnsureTable. This was causing the thread to be blocked. (#474)

## [19.0.3] - 2021-12-13
- Audit.NET.Redis: Ability to add custom Redis commands to the audit event saving batch (#471)

## [19.0.2] - 2021-12-10
- Audit.NET.Redis: Adding ability to configure the redis database ID to use. Upgrade StackExchange.Redis references to version 2.2.88. (#471)

## [19.0.1] - 2021-11-19
- Audit.EntityFramework / Audit.EntityFrameworkCore: Adding AuditEntityCreator configuration for EF Data Provider to allow custom audit entity creation (#468)

## [19.0.0] - 2021-11-10
- Audit.NET / Audit.EntityFrameworkCore: Adding .NET 6.0 and Entity Framework Core 6.0 support (#400)

## [18.1.6] - 2021-09-26
- Audit.WebApi: Audit Middleware disposing the response body stream if exception was thrown (#459)

## [19.0.0-rc.net60.1] - 2021-09-15
- Audit.NET / Audit.EntityFrameworkCore: Adding .NET 6.0 and Entity Framework Core 6.0 support, release candidates (#400)

## [18.1.5] - 2021-09-07
- Audit.EntityFrameworkCore: Adding `AuditCommandInterceptor` for low-level auditing of commands (reads, writes, non-query) on EF Core 3.0 and 5.0. (#100, #449)

## [18.1.4] - 2021-09-05
- Audit.EntityFrameworkCore: Adding `AuditSaveChangesInterceptor` Save Changes Interceptor as an alternative to configure EF Core 5.0 auditing.

## [18.1.3] - 2021-08-19
- Audit.NET.MongoDB: Added async methods for InsertEvent / ReplaceEvent
- Audit.NET.AzureCosmos: Added EnumerateEvents method to CosmosDB provider
  
## [18.1.2] - 2021-08-07
- Audit.NET.AzureCosmos: Added support for .NET Standard 2.0 and .NET 5.0, using latest client library Microsoft.Azure.Cosmos (fixes #434)

## [18.1.1] - 2021-08-04
- Audit.NET.AzureStorageBlobs: New library to store audit event on Azure Storage blobs, using the latest client [Azure.Storage.Blobs](https://docs.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme).

## [18.1.0] - 2021-08-01
- Audit.NET.AzureStorage: Adding support for configuring the Azure Blob Tier after upload, and to set Metadata associated with the blog (#429)

## [18.0.1] - 2021-07-29
- Audit.WCF.Client: New extension providing client-side audit capabilities for WCF service calls (#427)
- Audit.NET.SqlServer: Adding support for providing the DbConnection instead of the connection string on .NET framework (#428)
- Audit.EntityFrameworkCore: Fix issue with OwnedEntities when using the EF data provider (#430)
- Audit.HttpClient: Removed Newtonsoft.json dependency from Audit.HttpClient

## [18.0.0] - 2021-07-25
- <span style="color:red">Audit.NET and all the extensions</span>: Changing the JSON serialization default library from Newtonsoft.Json to System.Text.Json for .NET >= 5.0. (#356)

## [17.0.8] - 2021-07-07
### Modified
- Audit.EntityFramework.Core: Fix error on SaveChanges() when using EntityFramework Core 5.0 Table-per-Type (#421)

## [17.0.7] - 2021-06-15
### Modified
- Audit.NET.MySql: Adding support for custom columns on MySql data provider (#415)

## [17.0.6] - 2021-06-05
### Modified
- Audit.EntityFramework: Fix "Sequence contains more than one element" exception for EF entities with inheritance, when more than one mapping fragment is defined on an entity (#409)

## [17.0.5] - 2021-05-27
### Modified
- Audit.WebApi: Fix to only read the request body stream when the stream can be seek (CanSeek = true) (#404)
- Audit.EntityFramework: Fix AuditEvent's entitytype not present for explicit mapping (#408)

## [17.0.4] - 2021-05-15
### Modified
- Audit.NET: Adding support for asynchronous global custom actions

## [17.0.3] - 2021-05-01
### Modified
- Audit.NET.MySql: Fix null reference exception for mySQL data provider when using custom transactions (#395)
- Audit.NET.EntityFramework / Audit.NET.EntityFramework.Core: Adding Support for asynchronous AuditEntityAction for the Entity Framework Data Provider (#397)

## [17.0.2] - 2021-04-21
### Modified
- Audit.NET.AzureCosmos: Deprecated `Audit.NET.AzureDocumentDB` package and replaced with new `Audit.NET.AzureCosmos`

## [17.0.1] - 2021-04-17
### Modified
- Audit.NET.AmazonQLDB: Update QLDB Provider to use Async API (#393)

## [17.0.0] - 2021-03-26
### Modified
- Audit.NET: Allow to set the data provider as a factory instead of passing the instance, so it creation can be delayed (#387)

## [16.5.6] - 2021-03-24
### Modified
- Audit.EntityFramework / Audit.EntityFramework.Core: Fix Property Matching error (*An item with the same key has already been added*) when entity objects have hidden base properties (#384)
 
## [16.5.5] - 2021-03-22
### Modified
- Audit.NET.AzureStorage: Call TableOperation.InsertOrMerge to avoid EntityAlreadyExists Conflict (409) exception
- Audit.EntityFramework / Audit.EntityFramework.Core: Explicit mapper enhancements for Entity Framework Data Provider. 
Allow mapping from entries not mapped to a type (i.e. implicitly created join tables) (#381)

## [16.5.4] - 2021-03-09
### Modified
- Audit.EntityFramework / Audit.EntityFramework.Core: Removing type constraint for EF configuration `ForContext<T>` to allow multiple DbContext to be audited without 
inheriting from `AuditDbContext` and allowing fluent configuration for each context separately (#207)

## [16.5.3] - 2021-02-26
### Modified
- Audit.Mvc / Audit.WebApi: Allow the use of AuditIgnore attribute on the return of the MVC actions `[return:AuditIgnore]` (#375)

## [16.5.2] - 2021-02-23
### Modified
- Audit.HttpClient: Allow injection of audited HttpClients through HttpClientFactory (#374)

## [16.5.1] - 2021-02-21
### Modified
- Audit.EntityFramework.Core: Fix EF Core when using multiple owned entities (#373)

## [16.5.0] - 2021-02-17
### Modified
- Audit.NET: Fix AuditScope DisposeAsync return value discrepancy when targeting net472 (#371)

## [16.4.5] - 2021-02-14
### Modified
- Audit.NET: Fix FileDataProvider SaveAsync method which was not respecting the JsonSerializerSetttings (#369)

## [16.4.4] - 2021-02-04
### Modified
- Audit.NET: Fix AuditScope creation when creating from an existing AuditEvent that has pre-assigned CustomFields (#364)
- Audit.EntityFramework: Set the event custom fields before the AuditScope creation so they are available from OnScopeCreated event (#364)
- Audit.NET.SqlServer: Fixed null values on custom columns (#357)

## [16.4.3] - 2021-01-27
### Modified
- Audit.EntityFramework.Identity.Core.v3: Fixed reference to Audit.EntityFramework.Core.v3 (#355)

## [16.4.2] - 2021-01-22
### Modified
- Audit.EntityFramework.Core: Fixed problem with Owned Entities relationships for EntityFramework Core 5 (#360)

## [16.4.1] - 2021-01-21
### Modified
- Audit.EntityFramework.Core: Fixed problem with Many-To-Many relationships without join entity for EntityFramework Core 5 (#360)

## [16.4.0] - 2021-01-10
### Added
- Adding new packages `Audit.EntityFramework.Core.v3` and `Audit.EntityFramework.Identity.Core.v3` for backward compatibility to be used when targeting EntityFramework Core 3 on .NET standard >= 2.0 (#355)

## [16.3.3] - 2021-01-08
### Modified
- Audit.Mvc / Audit.Mvc.Core: Changing body request read to be asynchronous. Avoiding exception when used on asp.net core 3 (System.InvalidOperationException: Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead) (#259)

## [16.3.2] - 2021-01-02
### Modified
- Audit.NET: Adding net461 as base target framework for supporting `IAsyncDisposable` for .NET Framework >= 4.6.1 (#354)

## [16.3.1] - 2020-12-31
### Modified
- Audit.SqlServer: Adding support for Microsoft.EntityFrameworkCore 5.0.1 (#353)

## [16.3.0] - 2020-12-30
### Modified
- Audit.EntityFramework.Core: Adding support for Microsoft.EntityFrameworkCore 5.0.1 (#352)

## [16.2.1] - 2020-12-27
### Modified
- Audit.NET.SqlServer: New optional setting to specify the DbContextOptions for the internal DbContext. (#351)

## [16.2.0] - 2020-10-13
### Modified
- Audit.NET (Core): New optional global cofiguration to include the namespaces on the type names logged (#343)

## [16.1.5] - 2020-10-04
### Modified
- Audit.PostgreSql: Upgrade npgsql driver to latest version and allowing dynamic configuration of the postgreSQL data provider settings (#340)

## [16.1.4] - 2020-09-17
### Modified
- Audit.MongoDB: Adding `SerializeAsBson` setting to allow overriding the serialization method for the target object from JSON to BSON. 

## [16.1.3] - 2020-09-13
### Modified
- Audit.AmazonQLDB: Update Amazon.QLDB.Driver to 1.0.1 as it supports .NET Framework 4.6.1 now (#334)

## [16.1.2] - 2020-09-09
### Added
- Audit.NET.Kafka: New Apache Kafka data provider (#331)
### Modified
- Audit.WebApi: Restore original body for upstream pipelines (#333)

## [16.1.1] - 2020-09-02
### Modified
- Audit.MVC / Audit.MVC.Core: Adding support for auditing Razor Pages (#329)
- Audit.MVC: Including minor fix from PR #327 (`GetCurrentAuditScope` extension method)

## [16.1.0] - 2020-08-19
### Modified
- Audit.WebApi / Audit.WebApi.Core: Fix for respecting creation policy on Audit WebApi Attribute and Middleware (#325).

## [16.0.3] - 2020-08-15
### Modified
- Audit.EntityFramework: Fix bug for ColumnValues and Changes when the entity property is a Complex Type (#321).

## [16.0.2] - 2020-08-09
### Added
- Audit.NET: Adding fluent API for AuditScope creation.

## [16.0.1] - 2020-08-07
### Modified
- Audit.EntityFramework: Enhance CLR entity type resolve in EF Core (#320)
- Audit.EntityFramework: Adding boolean `EarlySavingAudit` setting to AuditDbContext to allow the audit event to be saved **before** the entity saving operation takes place. Related to #316.
- Audit.NET.ElasticSearch: Upgrade NEST reference to version 7.8.2 to  fix exception *Could not load type Elasticsearch.Net.IInternalSerializerWithFormatter from assembly Elasticsearch.Net* (#313)

## [16.0.0] - 2020-08-06
### Modified
- Audit.NET: Moving the AuditScope creation to an `AuditScopeFactory` implementing an interface `IAuditScopeFactory`. 
Also added `IAuditScope` interface for unit testing. (#315, #319)
- Audit.NET: Enable disposable async for netstandard2.0 (#318)

## [15.2.4] - 2020-07-22
### Modified
- Audit.EntityFramework.Core: Changing version for Microsoft.EntityFrameworkCore reference when targeting .NET Standard 2.0 or 2.1 (now referencing Microsoft.EntityFrameworkCore 3.1.0) (#310)

## [15.2.3] - 2020-07-13
### Added
- Audit.NET.AmazonQLDB: Adding new data provider for Amazon QLDB.

## [15.2.2] - 2020-05-19
### Modified
- Audit.Mvc and Audit.WebApi: Remove [obsolete package references](https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-3.1&tabs=visual-studio#remove-obsolete-package-references) for netcoreapp >= 3.0 (Microsofot.AspNetCore.*) (#297)

## [15.2.1] - 2020-05-12
### Modified
- Audit.Mvc and Audit.WebApi: MissingMethodException Method not found: 'AuditScope.DisposeAsync()' when referencing Audit.Mvc from an ASP.NET core 3.1 application. (#296)

## [15.2.0] - 2020-05-09
### Modified
- Audit.NET: Modified `ISystemClock` interface to return a DataTime instead of DateTimeOffset so the DateKind is preserved (#287)

## [15.1.1] - 2020-05-04
### Modified
- Audit.EntityFramework: Fix Stack Overflow problem when entity has validation errors. Only for the .NET framework EF. (#294)

## [15.1.0] - 2020-04-13
### Modified
- Audit.WebApi: Use GetCurrentAuditScope when an HttpContext is missing (#285)
- Audit.WebApi: Package reference `Microsoft.AspNetCore.Mvc` downgrade for to version 2.1.0 for compatibility with older net core 2.1 framework (#284)

## [15.0.5] - 2020-03-20
### Modified
- Audit.EntityFramework: Fix EF events not being audited when calling EF Core's `SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken = default)` overload on the AuditDbContext. (#277)

## [15.0.4] - 2020-02-28
### Modified
- Audit.EntityFramework: Fix audit error when auditing multiple types inheriting from the same entity/table (#273)

## [15.0.3] - 2020-02-26
### Modified
- Audit.Mvc: Fix audit event saving when an exception occurs on the action method being audited (ASP.NET) (#274)

## [15.0.2] - 2020-01-20
### Modified
- Audit.EntityFramework: EntityFrameworkProvider add the possibility to configure the property matching by type, object-wide. (#269)

## [15.0.1] - 2020-01-10
### Modified
- Audit.NET.AzureStorage: Change table mapping cache dictionary to be a ConcurrentDictionary (#268)

## [15.0.0] - 2019-12-16
### Modified
- Audit.NET: Renaming properties `SerializedOld` and `SerializedNew` to be `Old` and `New` respectively on `AuditTarget` class. (#261)
- Audit.EntityFramework / Audit.EntityFramework.Core: Adding net472 as target to allow targeting EF CORE 3 from the full .NET Framework. (#263)

## [14.9.1] - 2019-11-30
### Modified
- Audit.WebApi / Audit.WebApi.Core: Changing body request/response read to be asynchronous. Avoiding exception when used on asp.net core 3 (System.InvalidOperationException: Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead) (#259)

## [14.9.0] - 2019-11-29
### Modified
- Audit.WebApi / Audit.WebApi.Core: Removing unneeded references (Microsoft.AspNetCore.Identity and Microsoft.Extensions.PlatformAbstractions) (#258)

## [14.8.1] - 2019-11-26
### Modified
- Audit.WebApi / Audit.WebApi.Core: Adding execution context getter to AuditApiAction (#257)

## [14.8.0] - 2019-11-20
### Modified
- Audit.EntityFramework: Fix 'Property set method not found.' for get-only properties on audit entities (#256)
- Audit.NET.AzureStorage: Fixed issue with non-concurrent collections (#223, #255)

## [14.7.0] - 2019-10-09
### Modified
- Audit.EntityFramework: Adding support for Net Standard >= 2.1 to point to the EntityFramework 6.3.0 library. (#249)

## [14.6.6] - 2019-10-08
### Modified
- Audit.NET.Elasticsearch: Updating for Elasticsearch 7 support. NEST library updated to latest release 7.3.1. (#248).
- Audit.NET.EntityFrameworkCore: Updating Microsoft.EntityFramework.Core library to 3.0.0.

## [14.6.5] - 2019-09-26
### Modified
- Audit.NET.MongoDB: Updating MongoDB.Driver library to latest release (2.9.1). This fixes problem when using mongo DB data provider in Net Core 3.0 (#246).

## [14.6.4] - 2019-09-21
### Modified
- Audit.EntityFramework: Adding ExcludeValidationResults setting to allow excluding entity validations on the audit output.

## [14.6.3] - 2019-08-12
### Modified
- Audit.SqlServer: Adding boolean configuration value (SetDatabaseInitializerNull) to specify if the initializer should be set to NULL on the constructor of the internal DbContext. Only for .NET Framework (#237)

## [14.6.2] - 2019-08-03
### Modified
- Audit.EntityFramework: EntityFramework Data Provider support to map a single entity to multiple audit entities.

## [14.6.1] - 2019-08-03
### Modified
- Audit.SqlServer: Adding NET Core 3 preview support (#234)

## [14.6.0] - 2019-07-26
### Modified
- Audit.EntityFramework.Core: Adding EF Core 3 support (#231)

## [14.5.7] - 2019-07-18
### Modified
- Audit.WebApi and Audit.WebApi.Core: Changed the default order on `AuditApiAttribute` and `AuditApiGlobalFilter` to be `int.MinValue` instead of `0`. 
This allows using `this.GetCurrentAuditScope()` on Controller overrides `OnActionExecutionAsync` and `OnActionExecuting` (#230)

## [14.5.6] - 2019-07-09
### Modified
- All: Changed nuget package <ProjectUrl> to point to the base Audit.NET repository, so it can be uploaded to GitHub packages
- Audit.WebApi: Fix null reference exception when response is null and response headers are included (#229)

## [14.5.5] - 2019-07-01
### Modified
- Audit.NET: Added `ISystemClock` interface to allow testing code that depends on `DateTime.UtcNow`, such as event start-date, end-date and duration. Added `Audit.Core.Configuration.SystemClock` static configuration  property.

## [14.5.4] - 2019-06-17
### Modified
- Audit.NET.PostgreSql: Fix IdColumnName missing quotes (#226)

## [14.5.3] - 2019-06-05
### Modified
- Audit.NET.SqlServer: Update Microsoft.EntityFrameworkCore reference and remove SqlServer.Design reference for netstandard2.0. (#225)

## [14.5.2] - 2019-05-30
### Added
- Audit.NET.NLog: New extension library to store the audit events using NLog.

## [14.5.1] - 2019-05-28
### Modified
- Audit.EntityFramework: Update Microsoft.EntityFrameworkCore to 2.2.4. Fixed issue with non-concurrent collections in concurrent integration

## [14.5.0] - 2019-05-24
### Modified
- Audit.WebApi / Audit.WebApi.Core: Allowing the use of `[AuditIgnoreAttribute]` on controllers/action methods when using the Middleware or a mixed approach (Middleware+ActionFilter). (#218)

## [14.4.0] - 2019-05-21
### Added
- Audit.HttpClient: New extension library to audit client calls to REST services when using `HttpClient`.

## [14.3.4] - 2019-05-13
### Added
- Audit.Mvc and Audit.Mvc.Core: Added `AuditIngoreAttribute` to allow ignoring controllers, actions and/or parameters on the MVC audit output.

## [14.3.3] - 2019-05-09
### Modified
- Upgrade newtonsoft.json references to latest version (12.0.2)

## [14.3.2] - 2019-04-30
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Adding constructor to the EF Data Provider that allows fluent API configuration.

## [14.3.1] - 2019-04-27
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Adding `ExcludeTransactionId` setting to allow avoiding the retrieval of the contextual transaction id for the audit events.

## [14.3.0] - 2019-04-24
### Modified
- Audit.EntityFramework and Audit.EntityFramework.Core: Fix compatibility issue with EF Core's Proxied Lazy Loading (Microsoft.EntityFrameworkCore.Proxies) (#214).

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


