@echo off
cls

del "audit.net\bin\debug\*.symbols.nupkg"
del "Audit.NET.JsonNewtonsoftAdapter\bin\debug\*.symbols.nupkg"
del "Audit.NET.JsonSystemAdapter\bin\debug\*.symbols.nupkg"
del "audit.mvc\bin\debug\*.symbols.nupkg"
del "audit.mvc.core\bin\debug\*.symbols.nupkg"
del "audit.webapi\bin\debug\*.symbols.nupkg"
del "audit.webapi.core\bin\debug\*.symbols.nupkg"
del "audit.net.azurecosmos\bin\debug\*.symbols.nupkg"
del "audit.net.mongodb\bin\debug\*.symbols.nupkg"
del "audit.net.sqlserver\bin\debug\*.symbols.nupkg"
del "audit.net.mysql\bin\debug\*.symbols.nupkg"
del "audit.entityframework\bin\debug\*.symbols.nupkg"
del "audit.entityframework.core\bin\debug\*.symbols.nupkg"
del "audit.entityframework.core.v3\bin\debug\*.symbols.nupkg"
del "audit.entityframework.identity\bin\debug\*.symbols.nupkg"
del "audit.entityframework.identity.core\bin\debug\*.symbols.nupkg"
del "audit.entityframework.identity.core.v3\bin\debug\*.symbols.nupkg"
del "audit.Wcf\bin\debug\*.symbols.nupkg"
del "audit.Wcf.Client\bin\debug\*.symbols.nupkg"
del "audit.net.azurestorage\bin\debug\*.symbols.nupkg"
del "audit.net.azurestorageblobs\bin\debug\*.symbols.nupkg"
del "audit.dynamicproxy\bin\debug\*.symbols.nupkg"
del "audit.net.udp\bin\debug\*.symbols.nupkg"
del "audit.net.redis\bin\debug\*.symbols.nupkg"
del "Audit.NET.PostgreSql\bin\debug\*.symbols.nupkg"
del "Audit.FileSystem\bin\debug\*.symbols.nupkg"
del "Audit.SignalR\bin\debug\*.symbols.nupkg"
del "Audit.NET.log4net\bin\debug\*.symbols.nupkg"
del "Audit.NET.Elasticsearch\bin\debug\*.symbols.nupkg"
del "Audit.NET.EventLog.Core\bin\debug\*.symbols.nupkg"
del "Audit.NET.DynamoDB\bin\debug\*.symbols.nupkg"
del "Audit.HttpClient\bin\debug\*.symbols.nupkg"
del "Audit.NET.NLog\bin\debug\*.symbols.nupkg"
del "Audit.NET.AmazonQLDB\bin\debug\*.symbols.nupkg"
del "Audit.NET.Kafka\bin\debug\*.symbols.nupkg"

nuget push "audit.net\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.JsonNewtonsoftAdapter\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.JsonSystemAdapter\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.mvc\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.Mvc.Core\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.webapi\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.WebApi.Core\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.azurecosmos\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.mongodb\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.sqlserver\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.mysql\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Core\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Core.v3\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Identity\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Identity.Core\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Identity.Core.v3\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "audit.Wcf\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.Wcf.Client\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AzureStorage\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AzureStorageBlobs\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.DynamicProxy\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Udp\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Redis\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.PostgreSql\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.FileSystem\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.SignalR\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.log4net\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Elasticsearch\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.EventLog.Core\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.DynamoDB\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.HttpClient\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.NLog\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AmazonQLDB\bin\debug\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Kafka\bin\debug\*.nupkg" -NoSymbols -source %1
