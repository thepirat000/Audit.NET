# Summary

|||
|:---|:---|
| Generated on: | 10/24/2025 - 18:30:01 |
| Coverage date: | 10/24/2025 - 18:06:04 - 10/24/2025 - 18:29:24 |
| Parser: | MultiReport (96x Cobertura) |
| Assemblies: | 40 |
| Classes: | 321 |
| Files: | 275 |
| **Line coverage:** | 89.3% (9351 of 10471) |
| Covered lines: | 9351 |
| Uncovered lines: | 1120 |
| Coverable lines: | 10471 |
| Total lines: | 27171 |
| **Branch coverage:** | 80.2% (3459 of 4311) |
| Covered branches: | 3459 |
| Total branches: | 4311 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| Audit.Wcf.Client | Audit.Wcf.Client.AuditMessageInspector | CreateWcfClientAction(...) | 272 | 16 || Audit.Wcf.Client | Audit.Wcf.Client.AuditMessageInspector | AfterReceiveReply(...) | 210 | 14 || Audit.Mvc.Core | Audit.Mvc.AuditAttribute | GetResponseBody(...) | 147 | 36 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | SetConfig(...) | 100 | 100 || Audit.EntityFramework.Core | Audit.EntityFramework.DbContextHelper | SetConfig(...) | 90 | 90 || Audit.NET | Audit.Core.Providers.EventLogDataProvider | InsertEvent(...) | 72 | 8 || Audit.NET.EventLog.Core | Audit.Core.Providers.EventLogDataProvider | InsertEvent(...) | 72 | 8 || Audit.NET.AzureStorageTables | Audit.AzureStorageTables.Providers.AzureTableDataProvider | CreateTableclient(...) | 65 | 10 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | BeginSaveChanges(...) | 42 | 6 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | BeginSaveChangesAsync() | 42 | 6 || Audit.WebApi | Audit.WebApi.AuditApiGlobalFilter | OnActionExecutedAsync() | 42 | 6 || Audit.WebApi | Audit.WebApi.AuditApiGlobalFilter | OnActionExecutingAsync() | 42 | 6 || Audit.Mvc.Core | Audit.Mvc.AuditPageFilter | GetResponseBody(...) | 40 | 40 || Audit.Mvc.Core | Audit.Mvc.AuditPageFilter | BeforeExecutingAsync() | 40 | 40 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | CreateAuditEvent(...) | 38 | 38 || Audit.WebApi | Audit.WebApi.AuditApiAdapter | BeforeExecutingAsync() | 38 | 38 || Audit.EntityFramework | Audit.EntityFramework.Providers.EntityFrameworkDataProvider | InsertEvent(...) | 36 | 34 || Audit.WebApi.Core | Audit.WebApi.AuditApiAdapter | GetResponseBody(...) | 36 | 36 || Audit.EntityFramework | Audit.EntityFramework.Providers.EntityFrameworkDataProvider | InsertEventAsync() | 35 | 34 || Audit.EntityFramework.Core | Audit.EntityFramework.DbContextHelper | CreateAuditEvent(...) | 34 | 34 || Audit.EntityFramework.Core | Audit.EntityFramework.Providers.EntityFrameworkDataProvider | InsertEvent(...) | 34 | 34 || Audit.EntityFramework.Core | Audit.EntityFramework.Providers.EntityFrameworkDataProvider | InsertEventAsync() | 34 | 34 || Audit.Mvc.Core | Audit.Mvc.AuditAttribute | BeforeExecutingAsync() | 34 | 34 || Audit.Mvc.Core | Audit.Mvc.AuditPageFilter | AfterExecutedAsync() | 34 | 34 || Audit.NET | Audit.Core.AuditScope | .ctor(...) | 30 | 30 || Audit.DynamicProxy | Audit.DynamicProxy.AuditInterceptor | CreateAuditInterceptEvent(...) | 29 | 28 || Audit.NET.JsonNewtonsoftAdapter | Audit.JsonNewtonsoftAdapter.AuditContractResolver | CreateObjectContract(...) | 28 | 28 || Audit.SignalR | Audit.SignalR.AuditHubFilter | CreateAuditScopeAsync() | 28 | 28 || Audit.WebApi | Audit.WebApi.AuditApiAdapter | AfterExecutedAsync() | 28 | 28 || Audit.Mvc.Core | Audit.Mvc.AuditAttribute | AfterExecutedAsync() | 26 | 26 || Audit.WebApi.Core | Audit.WebApi.AuditApiAdapter | AfterExecutedAsync() | 26 | 26 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | MergeEntitySettings(...) | 26 | 24 || Audit.EntityFramework.Core | Audit.EntityFramework.DbContextHelper | MergeEntitySettings(...) | 24 | 24 || Audit.Mvc | Audit.Mvc.AuditAttribute | OnActionExecuting(...) | 24 | 24 || Audit.Mvc | Audit.Mvc.AuditAttribute | OnActionExecuted(...) | 24 | 24 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | AuditEventEnabled(...) | 25 | 24 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | UpdateAuditEvent(...) | 22 | 22 || Audit.Mvc | Audit.Mvc.AuditAttribute | GetResponseBody(...) | 22 | 22 || Audit.NET | Audit.Core.AuditScope | GetActivityTraceData() | 24 | 22 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | OnBeforeIncoming(...) | 23 | 22 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | OnIncomingError(...) | 22 | 22 || Audit.WebApi.Core | Audit.WebApi.AuditMiddleware | BeforeInvoke() | 22 | 22 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | IncludeProperty(...) | 25 | 20 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | GetAssociationEntries(...) | 20 | 20 || Audit.EntityFramework.Core | Audit.EntityFramework.DbContextHelper | IncludeProperty(...) | 20 | 20 || Audit.Mvc.Core | Audit.Mvc.AuditAttribute | AfterResultAsync() | 20 | 20 || Audit.NET.MongoDB | Audit.MongoDB.Providers.MongoDataProvider | FixDocumentElementNames(...) | 20 | 20 || Audit.WebApi.Core | Audit.WebApi.AuditApiAdapter | GetActionParameters(...) | 20 | 20 || Audit.WebApi.Core | Audit.WebApi.AuditApiAdapter | CreateOrUpdateAction() | 20 | 20 || Audit.FileSystem | Audit.FileSystem.FileSystemMonitor | Start() | 18 | 18 || Audit.WebApi.Core | Audit.WebApi.AuditMiddleware | AfterInvoke() | 18 | 18 || Audit.WebApi.Core | Audit.WebApi.AuditMiddleware | InvokeAsync() | 18 | 18 || Audit.DynamicProxy | Audit.DynamicProxy.AuditInterceptor | Intercept(...) | 16 | 16 || Audit.EntityFramework | Audit.EntityFramework.DbContextHelper | HasPropertyValue(...) | 18 | 16 || Audit.EntityFramework.Core | Audit.EntityFramework.DbContextHelper | UpdateAuditEvent(...) | 16 | 16 || Audit.HttpClient | Audit.Http.AuditHttpClientHandler | SendAsync() | 16 | 16 || Audit.Mvc | Audit.Mvc.AuditAttribute | OnResultExecuted(...) | 16 | 16 || Audit.NET | Audit.Core.AuditScope | GetEnvironmentInfo(...) | 16 | 16 || Audit.NET.Redis | Audit.Redis.Providers.RedisProviderSortedSet | ExecSortedSetAdd(...) | 16 | 16 || Audit.SignalR | Audit.SignalR.AuditHubFilter | OnDisconnectedAsync() | 17 | 16 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | OnBeforeConnect(...) | 17 | 16 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | OnBeforeDisconnect(...) | 17 | 16 || Audit.SignalR | Audit.SignalR.AuditPipelineModule | OnBeforeReconnect(...) | 17 | 16 || Audit.WebApi.Core | Audit.WebApi.AuditApiAdapter | ActionIgnored(...) | 16 | 16 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Audit.DynamicProxy** | **154** | **7** | **161** | **523** | **95.6%** | **64** | **74** | **86.4%** |
| Audit.DynamicProxy.AuditEventExtensions | 5 | 1 | 6 | 32 | 83.3% | 3 | 6 | 50% |
| Audit.DynamicProxy.AuditEventIntercept | 1 | 0 | 1 | 19 | 100% | 0 | 0 |  |
| Audit.DynamicProxy.AuditInterceptArgument | 17 | 0 | 17 | 53 | 100% | 0 | 0 |  |
| Audit.DynamicProxy.AuditInterceptor | 101 | 6 | 107 | 253 | 94.3% | 57 | 64 | 89% |
| Audit.DynamicProxy.AuditProxy | 11 | 0 | 11 | 59 | 100% | 4 | 4 | 100% |
| Audit.DynamicProxy.InterceptEvent | 12 | 0 | 12 | 59 | 100% | 0 | 0 |  |
| Audit.DynamicProxy.InterceptionSettings | 7 | 0 | 7 | 48 | 100% | 0 | 0 |  |
| **Audit.EntityFramework** | **1084** | **263** | **1347** | **3615** | **80.4%** | **512** | **640** | **80%** |
| Audit.Core.EntityFrameworkConfiguratorExtensions | 15 | 15 | 30 | 72 | 50% | 0 | 2 | 0% |
| Audit.EntityFramework.AssociationEntry | 3 | 0 | 3 | 18 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AssociationEntryRecord | 5 | 0 | 5 | 19 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditDbContext | 27 | 23 | 50 | 253 | 54% | 0 | 0 |  |
| Audit.EntityFramework.AuditDbContextAttribute | 2 | 15 | 17 | 92 | 11.7% | 0 | 10 | 0% |
| Audit.EntityFramework.AuditEventEntityFramework | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditEventExtensions | 2 | 4 | 6 | 82 | 33.3% | 1 | 6 | 16.6% |
| Audit.EntityFramework.ColumnValueChange | 2 | 0 | 2 | 7 | 100% | 0 | 0 |  |
| Audit.EntityFramework.Configuration | 44 | 9 | 53 | 142 | 83% | 5 | 8 | 62.5% |
| Audit.EntityFramework.ConfigurationApi.AuditEntityMapping | 186 | 53 | 239 | 363 | 77.8% | 2 | 8 | 25% |
| Audit.EntityFramework.ConfigurationApi.ContextConfigurator | 8 | 0 | 8 | 35 | 100% | 4 | 4 | 100% |
| Audit.EntityFramework.ConfigurationApi.ContextEntitySetting<T> | 20 | 8 | 28 | 85 | 71.4% | 4 | 8 | 50% |
| Audit.EntityFramework.ConfigurationApi.ContextSettingsConfigurator<T> | 14 | 2 | 16 | 57 | 87.5% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EfEntitySettings | 3 | 0 | 3 | 30 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EfSettings | 14 | 0 | 14 | 31 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EntityFrameworkProviderConfigurator | 62 | 25 | 87 | 184 | 71.2% | 2 | 6 | 33.3% |
| Audit.EntityFramework.ConfigurationApi.ExcludeConfigurator<T> | 6 | 0 | 6 | 26 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.IncludeEntityConfigurator<T> | 2 | 8 | 10 | 33 | 20% | 0 | 2 | 0% |
| Audit.EntityFramework.ConfigurationApi.IncludePropertyConfigurator<T> | 0 | 11 | 11 | 36 | 0% | 0 | 4 | 0% |
| Audit.EntityFramework.ConfigurationApi.MappingInfo | 4 | 0 | 4 | 31 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.ModeConfigurator<T> | 6 | 0 | 6 | 21 | 100% | 0 | 0 |  |
| Audit.EntityFramework.DbContextHelper | 380 | 63 | 443 | 1023 | 85.7% | 349 | 414 | 84.2% |
| Audit.EntityFramework.DefaultAuditContext | 19 | 4 | 23 | 48 | 82.6% | 0 | 0 |  |
| Audit.EntityFramework.EntityFrameworkEvent | 11 | 3 | 14 | 81 | 78.5% | 0 | 0 |  |
| Audit.EntityFramework.EntityKeyHelper | 114 | 0 | 114 | 295 | 100% | 65 | 68 | 95.5% |
| Audit.EntityFramework.EntityName | 2 | 0 | 2 | 11 | 100% | 0 | 0 |  |
| Audit.EntityFramework.EventEntry | 14 | 3 | 17 | 120 | 82.3% | 0 | 0 |  |
| Audit.EntityFramework.EventEntryChange | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.EntityFramework.Providers.EntityFrameworkDataProvider | 115 | 17 | 132 | 396 | 87.1% | 80 | 100 | 80% |
| **Audit.EntityFramework.Abstractions** | **4** | **2** | **6** | **20** | **66.6%** | **0** | **0** | **** |
| Audit.EntityFramework.AuditOverrideAttribute | 4 | 2 | 6 | 20 | 66.6% | 0 | 0 |  |
| **Audit.EntityFramework.Core** | **1738** | **110** | **1848** | **5236** | **94%** | **768** | **878** | **87.4%** |
| Audit.Core.DbContextConfiguratorExtensions | 2 | 2 | 4 | 37 | 50% | 0 | 0 |  |
| Audit.Core.EntityFrameworkConfiguratorExtensions | 28 | 2 | 30 | 72 | 93.3% | 2 | 2 | 100% |
| Audit.EntityFramework.AuditDbContext | 34 | 0 | 34 | 253 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditDbContextAttribute | 15 | 0 | 15 | 92 | 100% | 8 | 8 | 100% |
| Audit.EntityFramework.AuditEventCommandEntityFramework | 1 | 0 | 1 | 14 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditEventEntityFramework | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditEventExtensions | 18 | 0 | 18 | 82 | 100% | 18 | 18 | 100% |
| Audit.EntityFramework.AuditEventTransactionEntityFramework | 1 | 0 | 1 | 14 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditSaveChangesInterceptor | 21 | 0 | 21 | 61 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ColumnValueChange | 2 | 0 | 2 | 7 | 100% | 0 | 0 |  |
| Audit.EntityFramework.CommandEvent | 7 | 1 | 8 | 58 | 87.5% | 0 | 0 |  |
| Audit.EntityFramework.Configuration | 45 | 6 | 51 | 142 | 88.2% | 8 | 8 | 100% |
| Audit.EntityFramework.ConfigurationApi.AuditEntityMapping | 238 | 1 | 239 | 363 | 99.5% | 3 | 8 | 37.5% |
| Audit.EntityFramework.ConfigurationApi.ContextConfigurator | 8 | 0 | 8 | 35 | 100% | 4 | 4 | 100% |
| Audit.EntityFramework.ConfigurationApi.ContextEntitySetting<T> | 20 | 8 | 28 | 85 | 71.4% | 3 | 8 | 37.5% |
| Audit.EntityFramework.ConfigurationApi.ContextSettingsConfigurator<T> | 14 | 0 | 14 | 57 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.DbContextProviderConfigurator | 17 | 0 | 17 | 86 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.DbContextProviderConfigurator<T1, T2> | 13 | 0 | 13 | 86 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.DbContextProviderEntityConfigurator | 6 | 0 | 6 | 50 | 100% | 1 | 2 | 50% |
| Audit.EntityFramework.ConfigurationApi.DbContextProviderEntityConfigurator<T> | 4 | 0 | 4 | 50 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EfEntitySettings | 3 | 0 | 3 | 30 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EfSettings | 13 | 0 | 13 | 31 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.EntityFrameworkProviderConfigurator | 69 | 18 | 87 | 184 | 79.3% | 4 | 6 | 66.6% |
| Audit.EntityFramework.ConfigurationApi.ExcludeConfigurator<T> | 0 | 6 | 6 | 26 | 0% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.IncludeEntityConfigurator<T> | 6 | 4 | 10 | 33 | 60% | 2 | 2 | 100% |
| Audit.EntityFramework.ConfigurationApi.IncludePropertyConfigurator<T> | 9 | 2 | 11 | 36 | 81.8% | 2 | 4 | 50% |
| Audit.EntityFramework.ConfigurationApi.MappingInfo | 4 | 0 | 4 | 31 | 100% | 0 | 0 |  |
| Audit.EntityFramework.ConfigurationApi.ModeConfigurator<T> | 6 | 0 | 6 | 21 | 100% | 0 | 0 |  |
| Audit.EntityFramework.DbContextExtensions | 3 | 0 | 3 | 24 | 100% | 0 | 0 |  |
| Audit.EntityFramework.DbContextHelper | 454 | 18 | 472 | 1134 | 96.1% | 411 | 452 | 90.9% |
| Audit.EntityFramework.DefaultAuditContext | 18 | 4 | 22 | 48 | 81.8% | 0 | 0 |  |
| Audit.EntityFramework.EntityFrameworkEvent | 13 | 1 | 14 | 81 | 92.8% | 0 | 0 |  |
| Audit.EntityFramework.EntityName | 2 | 0 | 2 | 11 | 100% | 0 | 0 |  |
| Audit.EntityFramework.EventEntry | 16 | 1 | 17 | 120 | 94.1% | 0 | 0 |  |
| Audit.EntityFramework.EventEntryChange | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.EntityFramework.InterceptorEventBase | 8 | 0 | 8 | 56 | 100% | 0 | 0 |  |
| Audit.EntityFramework.Interceptors.AuditCommandInterceptor | 192 | 11 | 203 | 464 | 94.5% | 95 | 114 | 83.3% |
| Audit.EntityFramework.Interceptors.AuditTransactionInterceptor | 173 | 6 | 179 | 391 | 96.6% | 53 | 66 | 80.3% |
| Audit.EntityFramework.Providers.DbContextDataProvider | 62 | 0 | 62 | 200 | 100% | 40 | 46 | 86.9% |
| Audit.EntityFramework.Providers.DbContextDataProvider<T1, T2> | 62 | 8 | 70 | 219 | 88.5% | 18 | 24 | 75% |
| Audit.EntityFramework.Providers.EntityFrameworkDataProvider | 123 | 10 | 133 | 396 | 92.4% | 96 | 106 | 90.5% |
| Audit.EntityFramework.TransactionEvent | 4 | 1 | 5 | 32 | 80% | 0 | 0 |  |
| **Audit.EntityFramework.Identity** | **111** | **0** | **111** | **950** | **100%** | **0** | **0** | **** |
| Audit.EntityFramework.AuditIdentityDbContext | 12 | 0 | 12 | 303 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditIdentityDbContext<T> | 51 | 0 | 51 | 303 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditIdentityDbContext<T1, T2, T3, T4, T5, T6> | 48 | 0 | 48 | 344 | 100% | 0 | 0 |  |
| **Audit.EntityFramework.Identity.Core** | **42** | **5** | **47** | **572** | **89.3%** | **0** | **0** | **** |
| Audit.EntityFramework.AuditIdentityDbContext | 4 | 0 | 4 | 76 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditIdentityDbContext<T> | 0 | 4 | 4 | 76 | 0% | 0 | 0 |  |
| Audit.EntityFramework.AuditIdentityDbContext<T1, T2, T3> | 4 | 0 | 4 | 76 | 100% | 0 | 0 |  |
| Audit.EntityFramework.AuditIdentityDbContext<T1, T2, T3, T4, T5, T6, T7, T8> | 34 | 1 | 35 | 344 | 97.1% | 0 | 0 |  |
| **Audit.FileSystem** | **166** | **0** | **166** | **408** | **100%** | **52** | **52** | **100%** |
| Audit.FileSystem.AuditEventFileSystem | 1 | 0 | 1 | 19 | 100% | 0 | 0 |  |
| Audit.FileSystem.FileBinaryContent | 2 | 0 | 2 | 12 | 100% | 0 | 0 |  |
| Audit.FileSystem.FileSystemEvent | 15 | 0 | 15 | 27 | 100% | 0 | 0 |  |
| Audit.FileSystem.FileSystemMonitor | 128 | 0 | 128 | 254 | 100% | 52 | 52 | 100% |
| Audit.FileSystem.FileSystemMonitorOptions | 18 | 0 | 18 | 85 | 100% | 0 | 0 |  |
| Audit.FileSystem.FileTextualContent | 2 | 0 | 2 | 11 | 100% | 0 | 0 |  |
| **Audit.HttpClient** | **202** | **5** | **207** | **604** | **97.5%** | **78** | **86** | **90.6%** |
| Audit.Http.AuditEventExtensions | 3 | 3 | 6 | 32 | 50% | 2 | 6 | 33.3% |
| Audit.Http.AuditEventHttpClient | 1 | 0 | 1 | 12 | 100% | 0 | 0 |  |
| Audit.Http.AuditHttpClientHandler | 130 | 1 | 131 | 282 | 99.2% | 73 | 76 | 96% |
| Audit.Http.ClientFactory | 1 | 1 | 2 | 28 | 50% | 0 | 0 |  |
| Audit.Http.ConfigurationApi.AuditClientHandlerConfigurator | 38 | 0 | 38 | 136 | 100% | 0 | 0 |  |
| Audit.Http.Content | 2 | 0 | 2 | 10 | 100% | 0 | 0 |  |
| Audit.Http.HttpAction | 14 | 0 | 14 | 55 | 100% | 3 | 4 | 75% |
| Audit.Http.HttpClientBuilderAuditExtensions | 1 | 0 | 1 | 19 | 100% | 0 | 0 |  |
| Audit.Http.Request | 6 | 0 | 6 | 16 | 100% | 0 | 0 |  |
| Audit.Http.Response | 6 | 0 | 6 | 14 | 100% | 0 | 0 |  |
| **Audit.MongoClient** | **113** | **4** | **117** | **448** | **96.5%** | **29** | **30** | **96.6%** |
| Audit.MongoClient.AuditEventMongoCommand | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Audit.MongoClient.ClusterBuilderExtensions | 2 | 0 | 2 | 20 | 100% | 0 | 0 |  |
| Audit.MongoClient.ConfigurationApi.AuditMongoConfigurator | 16 | 0 | 16 | 72 | 100% | 0 | 0 |  |
| Audit.MongoClient.MongoAuditEventSubscriber | 73 | 2 | 75 | 193 | 97.3% | 27 | 28 | 96.4% |
| Audit.MongoClient.MongoClientSettingsExtensions | 4 | 0 | 4 | 28 | 100% | 2 | 2 | 100% |
| Audit.MongoClient.MongoCommandEvent | 13 | 2 | 15 | 95 | 86.6% | 0 | 0 |  |
| Audit.MongoClient.MongoConnection | 4 | 0 | 4 | 25 | 100% | 0 | 0 |  |
| **Audit.Mvc** | **182** | **5** | **187** | **484** | **97.3%** | **110** | **128** | **85.9%** |
| Audit.Mvc.AuditAction | 22 | 2 | 24 | 65 | 91.6% | 0 | 0 |  |
| Audit.Mvc.AuditAttribute | 139 | 0 | 139 | 289 | 100% | 97 | 110 | 88.1% |
| Audit.Mvc.AuditEventExtensions | 6 | 0 | 6 | 32 | 100% | 6 | 6 | 100% |
| Audit.Mvc.AuditEventMvcAction | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| Audit.Mvc.AuditHelper | 9 | 3 | 12 | 42 | 75% | 7 | 12 | 58.3% |
| Audit.Mvc.BodyContent | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.Mvc.ControllerExtensions | 2 | 0 | 2 | 31 | 100% | 0 | 0 |  |
| **Audit.Mvc.Core** | **358** | **26** | **384** | **902** | **93.2%** | **229** | **320** | **71.5%** |
| Audit.Mvc.AuditAction | 23 | 3 | 26 | 65 | 88.4% | 0 | 0 |  |
| Audit.Mvc.AuditAttribute | 166 | 18 | 184 | 381 | 90.2% | 103 | 154 | 66.8% |
| Audit.Mvc.AuditEventExtensions | 6 | 0 | 6 | 32 | 100% | 6 | 6 | 100% |
| Audit.Mvc.AuditEventMvcAction | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| Audit.Mvc.AuditHelper | 14 | 4 | 18 | 57 | 77.7% | 10 | 16 | 62.5% |
| Audit.Mvc.AuditPageFilter | 142 | 1 | 143 | 302 | 99.3% | 110 | 144 | 76.3% |
| Audit.Mvc.BodyContent | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.Mvc.ControllerExtensions | 3 | 0 | 3 | 40 | 100% | 0 | 0 |  |
| **Audit.NET** | **1265** | **57** | **1322** | **4470** | **95.6%** | **309** | **346** | **89.3%** |
| Audit.Core.AuditActivityEvent | 2 | 0 | 2 | 78 | 100% | 0 | 0 |  |
| Audit.Core.AuditActivityTag | 2 | 0 | 2 | 78 | 100% | 0 | 0 |  |
| Audit.Core.AuditActivityTrace | 10 | 0 | 10 | 78 | 100% | 0 | 0 |  |
| Audit.Core.AuditDataProvider | 20 | 0 | 20 | 128 | 100% | 8 | 8 | 100% |
| Audit.Core.AuditEvent | 16 | 0 | 16 | 107 | 100% | 0 | 0 |  |
| Audit.Core.AuditEventEnvironment | 11 | 0 | 11 | 74 | 100% | 0 | 0 |  |
| Audit.Core.AuditScope | 255 | 7 | 262 | 661 | 97.3% | 154 | 168 | 91.6% |
| Audit.Core.AuditScopeFactory | 67 | 0 | 67 | 209 | 100% | 2 | 2 | 100% |
| Audit.Core.AuditScopeOptions | 38 | 0 | 38 | 129 | 100% | 2 | 2 | 100% |
| Audit.Core.AuditScopeOptionsConfigurator | 28 | 5 | 33 | 107 | 84.8% | 1 | 2 | 50% |
| Audit.Core.AuditTarget | 3 | 0 | 3 | 26 | 100% | 0 | 0 |  |
| Audit.Core.Configuration | 170 | 12 | 182 | 492 | 93.4% | 9 | 10 | 90% |
| Audit.Core.ConfigurationApi.ActionConfigurator | 5 | 0 | 5 | 19 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.ActionEventSelector | 14 | 2 | 16 | 48 | 87.5% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.ActivityProviderConfigurator | 38 | 0 | 38 | 121 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.BlockingCollectionProviderConfigurator | 10 | 0 | 10 | 32 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.BlockingCollectionProviderExtraConfigurator | 2 | 0 | 2 | 13 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.ConditionalDataProviderConfigurator | 37 | 0 | 37 | 77 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.Configurator | 67 | 0 | 67 | 195 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.CreationPolicyConfigurator | 10 | 0 | 10 | 35 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.DynamicAsyncDataProviderConfigurator | 21 | 2 | 23 | 79 | 91.3% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.DynamicDataProviderConfigurator | 11 | 2 | 13 | 47 | 84.6% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.EventLogProviderConfigurator | 17 | 0 | 17 | 54 | 100% | 0 | 0 |  |
| Audit.Core.ConfigurationApi.FileLogProviderConfigurator | 10 | 0 | 10 | 35 | 100% | 0 | 0 |  |
| Audit.Core.DefaultSystemClock | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| Audit.Core.Extensions.ExceptionExtensions | 3 | 0 | 3 | 25 | 100% | 2 | 2 | 100% |
| Audit.Core.Extensions.TypeExtensions | 13 | 1 | 14 | 43 | 92.8% | 7 | 8 | 87.5% |
| Audit.Core.JsonAdapter | 11 | 3 | 14 | 53 | 78.5% | 4 | 8 | 50% |
| Audit.Core.PlatformHelper | 5 | 0 | 5 | 16 | 100% | 0 | 0 |  |
| Audit.Core.Providers.ActivityDataProvider | 98 | 1 | 99 | 333 | 98.9% | 50 | 52 | 96.1% |
| Audit.Core.Providers.BlockingCollectionDataProvider | 37 | 2 | 39 | 169 | 94.8% | 18 | 22 | 81.8% |
| Audit.Core.Providers.DynamicAsyncDataProvider | 45 | 2 | 47 | 169 | 95.7% | 6 | 6 | 100% |
| Audit.Core.Providers.DynamicDataProvider | 28 | 3 | 31 | 106 | 90.3% | 6 | 6 | 100% |
| Audit.Core.Providers.EventLogDataProvider | 15 | 12 | 27 | 82 | 55.5% | 2 | 10 | 20% |
| Audit.Core.Providers.FileDataProvider | 56 | 0 | 56 | 153 | 100% | 12 | 12 | 100% |
| Audit.Core.Providers.InMemoryDataProvider | 26 | 0 | 26 | 76 | 100% | 0 | 0 |  |
| Audit.Core.Providers.NullDataProvider | 6 | 0 | 6 | 36 | 100% | 0 | 0 |  |
| Audit.Core.Providers.Wrappers.ConditionalDataProvider | 14 | 3 | 17 | 57 | 82.3% | 6 | 8 | 75% |
| Audit.Core.Providers.Wrappers.DeferredDataProvider | 7 | 0 | 7 | 34 | 100% | 0 | 0 |  |
| Audit.Core.Providers.Wrappers.LazyDataProvider | 7 | 0 | 7 | 31 | 100% | 0 | 0 |  |
| Audit.Core.Providers.Wrappers.WrapperDataProvider | 15 | 0 | 15 | 68 | 100% | 18 | 18 | 100% |
| Audit.Core.Setting<T> | 14 | 0 | 14 | 81 | 100% | 2 | 2 | 100% |
| **Audit.NET.AzureCosmos** | **113** | **10** | **123** | **406** | **91.8%** | **35** | **46** | **76%** |
| Audit.AzureCosmos.ConfigurationApi.AzureCosmosProviderConfigurator | 17 | 8 | 25 | 83 | 68% | 0 | 0 |  |
| Audit.AzureCosmos.Providers.AuditCosmosSerializer | 9 | 2 | 11 | 36 | 81.8% | 4 | 6 | 66.6% |
| Audit.AzureCosmos.Providers.AzureCosmosDataProvider | 85 | 0 | 85 | 266 | 100% | 31 | 40 | 77.5% |
| Audit.Core.AzureCosmosConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| **Audit.NET.AzureEventHubs** | **53** | **14** | **67** | **234** | **79.1%** | **7** | **14** | **50%** |
| Audit.AzureEventHubs.ConfigurationApi.AzureEventHubsConnectionConfigurator | 9 | 4 | 13 | 39 | 69.2% | 0 | 0 |  |
| Audit.AzureEventHubs.ConfigurationApi.AzureEventHubsCustomConfigurator | 2 | 2 | 4 | 20 | 50% | 0 | 0 |  |
| Audit.AzureEventHubs.Providers.AzureEventHubsDataProvider | 40 | 8 | 48 | 156 | 83.3% | 7 | 14 | 50% |
| Audit.Core.AzureEventHubsConfiguratorExtensions | 2 | 0 | 2 | 19 | 100% | 0 | 0 |  |
| **Audit.NET.AzureStorageBlobs** | **137** | **12** | **149** | **450** | **91.9%** | **27** | **30** | **90%** |
| Audit.AzureStorageBlobs.ConfigurationApi.AzureBlobConnectionConfigurator | 9 | 0 | 9 | 32 | 100% | 0 | 0 |  |
| Audit.AzureStorageBlobs.ConfigurationApi.AzureBlobContainerConfigurator | 8 | 8 | 16 | 66 | 50% | 0 | 0 |  |
| Audit.AzureStorageBlobs.ConfigurationApi.AzureBlobCredentialConfiguration | 4 | 4 | 8 | 39 | 50% | 0 | 0 |  |
| Audit.AzureStorageBlobs.Providers.AzureStorageBlobDataProvider | 114 | 0 | 114 | 292 | 100% | 27 | 30 | 90% |
| Audit.Core.AzureConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| **Audit.NET.AzureStorageTables** | **135** | **26** | **161** | **481** | **83.8%** | **26** | **48** | **54.1%** |
| Audit.AzureStorageTables.ConfigurationApi.AuditEventTableEntity | 17 | 4 | 21 | 62 | 80.9% | 0 | 0 |  |
| Audit.AzureStorageTables.ConfigurationApi.AzureTableColumnsConfigurator | 7 | 1 | 8 | 32 | 87.5% | 3 | 6 | 50% |
| Audit.AzureStorageTables.ConfigurationApi.AzureTableConnectionConfigurator | 16 | 0 | 16 | 61 | 100% | 0 | 0 |  |
| Audit.AzureStorageTables.ConfigurationApi.AzureTableEntityConfigurator | 14 | 2 | 16 | 49 | 87.5% | 5 | 10 | 50% |
| Audit.AzureStorageTables.ConfigurationApi.AzureTableRowConfigurator | 8 | 2 | 10 | 38 | 80% | 0 | 0 |  |
| Audit.AzureStorageTables.Providers.AzureTableDataProvider | 71 | 17 | 88 | 218 | 80.6% | 18 | 32 | 56.2% |
| Audit.Core.AzureTableConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| **Audit.NET.Channels** | **53** | **7** | **60** | **227** | **88.3%** | **9** | **12** | **75%** |
| Audit.Channels.Configuration.ChannelProviderConfigurator | 6 | 6 | 12 | 34 | 50% | 0 | 0 |  |
| Audit.Channels.Providers.ChannelDataProvider | 35 | 1 | 36 | 131 | 97.2% | 9 | 12 | 75% |
| Audit.Core.ChannelConfiguratorExtensions | 12 | 0 | 12 | 62 | 100% | 0 | 0 |  |
| **Audit.NET.DynamoDB** | **115** | **10** | **125** | **428** | **92%** | **49** | **68** | **72%** |
| Audit.Core.DynamoConfiguratorExtensions | 8 | 0 | 8 | 38 | 100% | 0 | 0 |  |
| Audit.DynamoDB.Configuration.DynamoProviderAttributeConfigurator | 3 | 0 | 3 | 17 | 100% | 0 | 0 |  |
| Audit.DynamoDB.Configuration.DynamoProviderConfigurator | 7 | 8 | 15 | 54 | 46.6% | 0 | 0 |  |
| Audit.DynamoDB.Configuration.DynamoProviderTableConfigurator | 7 | 0 | 7 | 28 | 100% | 0 | 0 |  |
| Audit.DynamoDB.Providers.DynamoDataProvider | 90 | 2 | 92 | 291 | 97.8% | 49 | 68 | 72% |
| **Audit.NET.Elasticsearch** | **72** | **18** | **90** | **285** | **80%** | **26** | **50** | **52%** |
| Audit.Core.ElasticsearchConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| Audit.Elasticsearch.Configuration.ElasticsearchProviderConfigurator | 13 | 2 | 15 | 54 | 86.6% | 0 | 0 |  |
| Audit.Elasticsearch.Providers.ElasticsearchAuditEventId | 2 | 0 | 2 | 13 | 100% | 0 | 0 |  |
| Audit.Elasticsearch.Providers.ElasticsearchDataProvider | 55 | 16 | 71 | 197 | 77.4% | 26 | 50 | 52% |
| **Audit.NET.EventLog.Core** | **25** | **12** | **37** | **109** | **67.5%** | **2** | **10** | **20%** |
| Audit.Core.EvenLogProviderConfigurator | 10 | 0 | 10 | 27 | 100% | 0 | 0 |  |
| Audit.Core.Providers.EventLogDataProvider | 15 | 12 | 27 | 82 | 55.5% | 2 | 10 | 20% |
| **Audit.NET.Firestore** | **211** | **4** | **215** | **592** | **98.1%** | **61** | **69** | **88.4%** |
| Audit.Core.FirestoreConfiguratorExtensions | 21 | 0 | 21 | 55 | 100% | 0 | 0 |  |
| Audit.Firestore.ConfigurationApi.FirestoreProviderConfigurator | 24 | 2 | 26 | 92 | 92.3% | 0 | 0 |  |
| Audit.Firestore.Providers.FirestoreDataProvider | 166 | 2 | 168 | 445 | 98.8% | 61 | 69 | 88.4% |
| **Audit.NET.ImmuDB** | **103** | **14** | **117** | **369** | **88%** | **24** | **28** | **85.7%** |
| Audit.Core.ImmuDbConfiguratorExtensions | 2 | 0 | 2 | 22 | 100% | 0 | 0 |  |
| Audit.ImmuDB.ConfigurationApi.ImmuDbProviderConfigurator | 18 | 10 | 28 | 98 | 64.2% | 0 | 0 |  |
| Audit.ImmuDB.Providers.ImmuDbDataProvider | 83 | 4 | 87 | 249 | 95.4% | 24 | 28 | 85.7% |
| **Audit.NET.JsonNewtonsoftAdapter** | **95** | **0** | **95** | **216** | **100%** | **39** | **40** | **97.5%** |
| Audit.Core.ConfiguratorExtensions | 4 | 0 | 4 | 32 | 100% | 0 | 0 |  |
| Audit.Core.JsonNewtonsoftAdapter | 33 | 0 | 33 | 82 | 100% | 8 | 8 | 100% |
| Audit.JsonNewtonsoftAdapter.AuditContractResolver | 58 | 0 | 58 | 102 | 100% | 31 | 32 | 96.8% |
| **Audit.NET.Kafka** | **74** | **23** | **97** | **532** | **76.2%** | **20** | **26** | **76.9%** |
| Audit.Core.KafkaConfiguratorExtensions | 4 | 0 | 4 | 34 | 100% | 0 | 0 |  |
| Audit.Kafka.Configuration.KafkaProviderConfigurator<T> | 12 | 10 | 22 | 85 | 54.5% | 0 | 0 |  |
| Audit.Kafka.Providers.DefaultJsonSerializer<T> | 2 | 0 | 2 | 19 | 100% | 1 | 2 | 50% |
| Audit.Kafka.Providers.KafkaDataProvider | 1 | 1 | 2 | 197 | 50% | 0 | 0 |  |
| Audit.Kafka.Providers.KafkaDataProvider<T> | 55 | 12 | 67 | 197 | 82% | 19 | 24 | 79.1% |
| **Audit.NET.log4net** | **39** | **15** | **54** | **195** | **72.2%** | **12** | **20** | **60%** |
| Audit.Core.Log4netConfiguratorExtensions | 2 | 2 | 4 | 32 | 50% | 0 | 0 |  |
| Audit.log4net.Configuration.Log4netConfigurator | 6 | 6 | 12 | 47 | 50% | 0 | 0 |  |
| Audit.log4net.Providers.Log4netDataProvider | 31 | 7 | 38 | 116 | 81.5% | 12 | 20 | 60% |
| **Audit.NET.MongoDB** | **171** | **9** | **180** | **446** | **95%** | **51** | **54** | **94.4%** |
| Audit.Core.MongoConfiguratorExtensions | 12 | 8 | 20 | 55 | 60% | 0 | 0 |  |
| Audit.MongoDB.ConfigurationApi.MongoProviderConfigurator | 15 | 0 | 15 | 50 | 100% | 0 | 0 |  |
| Audit.MongoDB.Providers.MongoDataProvider | 144 | 1 | 145 | 341 | 99.3% | 51 | 54 | 94.4% |
| **Audit.NET.MySql** | **148** | **2** | **150** | **365** | **98.6%** | **35** | **36** | **97.2%** |
| Audit.Core.MySqlServerConfiguratorExtensions | 14 | 0 | 14 | 49 | 100% | 0 | 0 |  |
| Audit.MySql.Configuration.MySqlServerProviderConfigurator | 15 | 0 | 15 | 45 | 100% | 0 | 0 |  |
| Audit.MySql.CustomColumn | 6 | 2 | 8 | 24 | 75% | 0 | 0 |  |
| Audit.MySql.Providers.MySqlDataProvider | 113 | 0 | 113 | 247 | 100% | 35 | 36 | 97.2% |
| **Audit.NET.NLog** | **39** | **15** | **54** | **193** | **72.2%** | **12** | **20** | **60%** |
| Audit.Core.NLogConfiguratorExtensions | 2 | 2 | 4 | 30 | 50% | 0 | 0 |  |
| Audit.NLog.Configuration.NLogConfigurator | 6 | 6 | 12 | 47 | 50% | 0 | 0 |  |
| Audit.NLog.Providers.NLogDataProvider | 31 | 7 | 38 | 116 | 81.5% | 12 | 20 | 60% |
| **Audit.NET.OpenSearch** | **74** | **13** | **87** | **277** | **85%** | **25** | **38** | **65.7%** |
| Audit.Core.OpenSearchConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| Audit.OpenSearch.Configuration.OpenSearchProviderConfigurator | 13 | 2 | 15 | 54 | 86.6% | 0 | 0 |  |
| Audit.OpenSearch.Providers.OpenSearchAuditEventId | 2 | 0 | 2 | 13 | 100% | 0 | 0 |  |
| Audit.OpenSearch.Providers.OpenSearchDataProvider | 57 | 11 | 68 | 189 | 83.8% | 25 | 38 | 65.7% |
| **Audit.NET.Polly** | **74** | **13** | **87** | **325** | **85%** | **20** | **22** | **90.9%** |
| Audit.Core.PollyConfiguratorExtensions | 2 | 0 | 2 | 19 | 100% | 0 | 0 |  |
| Audit.Polly.Configuration.PollyProviderConfigurator | 5 | 0 | 5 | 19 | 100% | 0 | 0 |  |
| Audit.Polly.Configuration.PollyResilienceConfigurator | 4 | 0 | 4 | 19 | 100% | 0 | 0 |  |
| Audit.Polly.FallbackActionArgumentsExtensions | 14 | 2 | 16 | 63 | 87.5% | 8 | 10 | 80% |
| Audit.Polly.HedgingActionGeneratorArgumentsExtensions | 17 | 0 | 17 | 69 | 100% | 10 | 10 | 100% |
| Audit.Polly.Providers.PollyDataProvider | 31 | 11 | 42 | 120 | 73.8% | 2 | 2 | 100% |
| Audit.Polly.ResilienceContextExtensions | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| **Audit.NET.PostgreSql** | **210** | **0** | **210** | **568** | **100%** | **60** | **64** | **93.7%** |
| Audit.Core.PostgreSqlConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| Audit.PostgreSql.Configuration.PostgreSqlProviderConfigurator | 34 | 0 | 34 | 102 | 100% | 0 | 0 |  |
| Audit.PostgreSql.CustomColumn | 8 | 0 | 8 | 24 | 100% | 0 | 0 |  |
| Audit.PostgreSql.Providers.PostgreSqlDataProvider | 166 | 0 | 166 | 421 | 100% | 60 | 64 | 93.7% |
| **Audit.NET.RavenDB** | **76** | **12** | **88** | **288** | **86.3%** | **6** | **8** | **75%** |
| Audit.RavenDB.ConfigurationApi.RavenDbConfiguratorExtensions | 2 | 0 | 2 | 21 | 100% | 0 | 0 |  |
| Audit.RavenDB.ConfigurationApi.RavenDbProviderConfigurator | 15 | 0 | 15 | 39 | 100% | 0 | 0 |  |
| Audit.RavenDB.ConfigurationApi.RavenDbProviderStoreConfigurator | 8 | 0 | 8 | 38 | 100% | 0 | 0 |  |
| Audit.RavenDB.Providers.RavenDbDataProvider | 51 | 12 | 63 | 190 | 80.9% | 6 | 8 | 75% |
| **Audit.NET.Redis** | **455** | **90** | **545** | **1713** | **83.4%** | **88** | **106** | **83%** |
| Audit.Core.RedisConfiguratorExtensions | 3 | 0 | 3 | 21 | 100% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisConfigurator | 26 | 0 | 26 | 74 | 100% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisHashConfigurator | 13 | 4 | 17 | 65 | 76.4% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisListConfigurator | 11 | 6 | 17 | 65 | 64.7% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisPubSubConfigurator | 4 | 0 | 4 | 22 | 100% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisSortedSetConfigurator | 21 | 12 | 33 | 113 | 63.6% | 0 | 0 |  |
| Audit.Redis.Configuration.RedisStreamConfigurator | 15 | 8 | 23 | 79 | 65.2% | 2 | 2 | 100% |
| Audit.Redis.Configuration.RedisStringConfigurator | 9 | 6 | 15 | 58 | 60% | 0 | 0 |  |
| Audit.Redis.Providers.RedisDataProvider | 17 | 0 | 17 | 86 | 100% | 0 | 0 |  |
| Audit.Redis.Providers.RedisDataProviderHelper | 39 | 0 | 39 | 120 | 100% | 0 | 0 |  |
| Audit.Redis.Providers.RedisProviderHandler | 28 | 5 | 33 | 100 | 84.8% | 12 | 12 | 100% |
| Audit.Redis.Providers.RedisProviderHash | 44 | 6 | 50 | 145 | 88% | 7 | 10 | 70% |
| Audit.Redis.Providers.RedisProviderList | 47 | 5 | 52 | 156 | 90.3% | 20 | 24 | 83.3% |
| Audit.Redis.Providers.RedisProviderPubSub | 26 | 8 | 34 | 95 | 76.4% | 3 | 4 | 75% |
| Audit.Redis.Providers.RedisProviderSortedSet | 71 | 10 | 81 | 213 | 87.6% | 30 | 36 | 83.3% |
| Audit.Redis.Providers.RedisProviderStream | 44 | 18 | 62 | 183 | 70.9% | 14 | 18 | 77.7% |
| Audit.Redis.Providers.RedisProviderString | 37 | 2 | 39 | 118 | 94.8% | 0 | 0 |  |
| **Audit.NET.Serilog** | **36** | **20** | **56** | **223** | **64.2%** | **10** | **20** | **50%** |
| Audit.Core.SerilogConfiguratorExtensions | 2 | 2 | 4 | 37 | 50% | 0 | 0 |  |
| Audit.Serilog.Configuration.SerilogConfigurator | 6 | 6 | 12 | 53 | 50% | 0 | 0 |  |
| Audit.Serilog.Providers.SerilogDataProvider | 28 | 12 | 40 | 133 | 70% | 10 | 20 | 50% |
| **Audit.NET.SqlServer** | **240** | **23** | **263** | **817** | **91.2%** | **102** | **114** | **89.4%** |
| Audit.Core.SqlServerConfiguratorExtensions | 2 | 0 | 2 | 23 | 100% | 0 | 0 |  |
| Audit.SqlServer.AuditEventValueModel | 1 | 0 | 1 | 88 | 100% | 0 | 0 |  |
| Audit.SqlServer.Configuration.SqlServerProviderConfigurator | 37 | 12 | 49 | 136 | 75.5% | 0 | 0 |  |
| Audit.SqlServer.CustomColumn | 12 | 2 | 14 | 32 | 85.7% | 0 | 0 |  |
| Audit.SqlServer.DefaultAuditDbContext | 16 | 3 | 19 | 88 | 84.2% | 4 | 4 | 100% |
| Audit.SqlServer.Providers.SqlDataProvider | 172 | 6 | 178 | 450 | 96.6% | 98 | 110 | 89% |
| **Audit.NET.Udp** | **81** | **10** | **91** | **308** | **89%** | **16** | **18** | **88.8%** |
| Audit.Core.UdpProviderConfiguratorExtensions | 13 | 0 | 13 | 48 | 100% | 0 | 0 |  |
| Audit.Udp.Configuration.UdpProviderConfigurator | 8 | 8 | 16 | 64 | 50% | 2 | 2 | 100% |
| Audit.Udp.Providers.UdpDataProvider | 60 | 2 | 62 | 196 | 96.7% | 14 | 16 | 87.5% |
| **Audit.SignalR** | **398** | **44** | **442** | **1121** | **90%** | **179** | **256** | **69.9%** |
| Audit.SignalR.AuditEventSignalr | 1 | 0 | 1 | 15 | 100% | 0 | 0 |  |
| Audit.SignalR.AuditHubFilter | 98 | 10 | 108 | 196 | 90.7% | 48 | 86 | 55.8% |
| Audit.SignalR.AuditPipelineModule | 195 | 11 | 206 | 344 | 94.6% | 115 | 146 | 78.7% |
| Audit.SignalR.Configuration.AuditHubConfigurator | 13 | 2 | 15 | 58 | 86.6% | 0 | 0 |  |
| Audit.SignalR.Configuration.AuditHubFilterConfigurator | 12 | 12 | 24 | 90 | 50% | 0 | 0 |  |
| Audit.SignalR.SignalrEventBase | 1 | 2 | 3 | 33 | 33.3% | 0 | 0 |  |
| Audit.SignalR.SignalrEventConnect | 7 | 0 | 7 | 41 | 100% | 0 | 0 |  |
| Audit.SignalR.SignalrEventDisconnect | 9 | 0 | 9 | 43 | 100% | 0 | 0 |  |
| Audit.SignalR.SignalrEventError | 13 | 0 | 13 | 33 | 100% | 0 | 0 |  |
| Audit.SignalR.SignalrEventIncoming | 13 | 1 | 14 | 49 | 92.8% | 0 | 0 |  |
| Audit.SignalR.SignalrEventOutgoing | 7 | 0 | 7 | 25 | 100% | 0 | 0 |  |
| Audit.SignalR.SignalrEventReconnect | 7 | 0 | 7 | 25 | 100% | 0 | 0 |  |
| Audit.SignalR.SignalrExtensions | 22 | 6 | 28 | 169 | 78.5% | 16 | 24 | 66.6% |
| **Audit.Wcf.Client** | **0** | **105** | **105** | **341** | **0%** | **0** | **44** | **0%** |
| Audit.Wcf.Client.AuditBehavior | 0 | 11 | 11 | 74 | 0% | 0 | 0 |  |
| Audit.Wcf.Client.AuditEndpointBehavior | 0 | 17 | 17 | 59 | 0% | 0 | 0 |  |
| Audit.Wcf.Client.AuditEventExtensions | 0 | 6 | 6 | 31 | 0% | 0 | 6 | 0% |
| Audit.Wcf.Client.AuditEventWcfClient | 0 | 1 | 1 | 12 | 0% | 0 | 0 |  |
| Audit.Wcf.Client.AuditMessageInspector | 0 | 60 | 60 | 115 | 0% | 0 | 38 | 0% |
| Audit.Wcf.Client.WcfClientAction | 0 | 10 | 10 | 50 | 0% | 0 | 0 |  |
| **Audit.WebApi** | **249** | **83** | **332** | **1192** | **75%** | **135** | **212** | **63.6%** |
| Audit.WebApi.ApiControllerExtensions | 4 | 0 | 4 | 92 | 100% | 0 | 0 |  |
| Audit.WebApi.AuditApiAction | 21 | 2 | 23 | 74 | 91.3% | 0 | 0 |  |
| Audit.WebApi.AuditApiAdapter | 126 | 2 | 128 | 250 | 98.4% | 85 | 106 | 80.1% |
| Audit.WebApi.AuditApiAttribute | 30 | 3 | 33 | 133 | 90.9% | 14 | 18 | 77.7% |
| Audit.WebApi.AuditApiGlobalFilter | 17 | 22 | 39 | 140 | 43.5% | 1 | 28 | 3.5% |
| Audit.WebApi.AuditApiHelper | 8 | 4 | 12 | 139 | 66.6% | 6 | 12 | 50% |
| Audit.WebApi.AuditEventExtensions | 6 | 0 | 6 | 32 | 100% | 6 | 6 | 100% |
| Audit.WebApi.AuditEventWebApi | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| Audit.WebApi.BodyContent | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.WebApi.ConfigurationApi.AuditApiGlobalActionsSelector | 3 | 18 | 21 | 63 | 14.2% | 0 | 4 | 0% |
| Audit.WebApi.ConfigurationApi.AuditApiGlobalConfigurator | 2 | 24 | 26 | 108 | 7.6% | 0 | 0 |  |
| Audit.WebApi.ContextWrapper | 28 | 8 | 36 | 136 | 77.7% | 23 | 38 | 60.5% |
| **Audit.WebApi.Core** | **456** | **32** | **488** | **1443** | **93.4%** | **232** | **284** | **81.6%** |
| Audit.WebApi.ApiControllerExtensions | 6 | 0 | 6 | 92 | 100% | 0 | 0 |  |
| Audit.WebApi.AuditApiAction | 22 | 2 | 24 | 74 | 91.6% | 0 | 0 |  |
| Audit.WebApi.AuditApiAdapter | 141 | 2 | 143 | 308 | 98.6% | 117 | 136 | 86% |
| Audit.WebApi.AuditApiAttribute | 35 | 2 | 37 | 130 | 94.5% | 15 | 16 | 93.7% |
| Audit.WebApi.AuditApiGlobalFilter | 32 | 1 | 33 | 140 | 96.9% | 19 | 20 | 95% |
| Audit.WebApi.AuditApiHelper | 47 | 5 | 52 | 139 | 90.3% | 23 | 36 | 63.8% |
| Audit.WebApi.AuditEventExtensions | 6 | 0 | 6 | 32 | 100% | 6 | 6 | 100% |
| Audit.WebApi.AuditEventWebApi | 1 | 0 | 1 | 16 | 100% | 0 | 0 |  |
| Audit.WebApi.AuditIgnoreActionFilter | 4 | 0 | 4 | 23 | 100% | 0 | 0 |  |
| Audit.WebApi.AuditMiddleware | 97 | 2 | 99 | 181 | 97.9% | 47 | 60 | 78.3% |
| Audit.WebApi.AuditMiddlewareExtensions | 3 | 0 | 3 | 24 | 100% | 0 | 0 |  |
| Audit.WebApi.BodyContent | 3 | 0 | 3 | 9 | 100% | 0 | 0 |  |
| Audit.WebApi.ConfigurationApi.AuditApiGlobalActionsSelector | 11 | 10 | 21 | 63 | 52.3% | 1 | 6 | 16.6% |
| Audit.WebApi.ConfigurationApi.AuditApiGlobalConfigurator | 26 | 0 | 26 | 108 | 100% | 0 | 0 |  |
| Audit.WebApi.ConfigurationApi.AuditMiddlewareConfigurator | 22 | 8 | 30 | 104 | 73.3% | 4 | 4 | 100% |

