@echo off
cls

del "audit.net\bin\release\*.symbols.nupkg"
del "Audit.NET.JsonNewtonsoftAdapter\bin\release\*.symbols.nupkg"
del "audit.mvc\bin\release\*.symbols.nupkg"
del "audit.mvc.core\bin\release\*.symbols.nupkg"
del "audit.webapi\bin\release\*.symbols.nupkg"
del "audit.webapi.core\bin\release\*.symbols.nupkg"
del "audit.net.azurecosmos\bin\release\*.symbols.nupkg"
del "audit.net.mongodb\bin\release\*.symbols.nupkg"
del "audit.net.sqlserver\bin\release\*.symbols.nupkg"
del "audit.net.mysql\bin\release\*.symbols.nupkg"
del "audit.entityframework\bin\release\*.symbols.nupkg"
del "audit.entityframework.core\bin\release\*.symbols.nupkg"
del "audit.entityframework.identity\bin\release\*.symbols.nupkg"
del "audit.entityframework.identity.core\bin\release\*.symbols.nupkg"
del "audit.Wcf\bin\release\*.symbols.nupkg"
del "audit.Wcf.Client\bin\release\*.symbols.nupkg"
del "audit.net.azurestorage\bin\release\*.symbols.nupkg"
del "audit.net.azurestorageblobs\bin\release\*.symbols.nupkg"
del "audit.dynamicproxy\bin\release\*.symbols.nupkg"
del "audit.net.udp\bin\release\*.symbols.nupkg"
del "audit.net.redis\bin\release\*.symbols.nupkg"
del "Audit.NET.PostgreSql\bin\release\*.symbols.nupkg"
del "Audit.NET.RavenDB\bin\release\*.symbols.nupkg"
del "Audit.FileSystem\bin\release\*.symbols.nupkg"
del "Audit.SignalR\bin\release\*.symbols.nupkg"
del "Audit.NET.log4net\bin\release\*.symbols.nupkg"
del "Audit.NET.Elasticsearch\bin\release\*.symbols.nupkg"
del "Audit.NET.EventLog.Core\bin\release\*.symbols.nupkg"
del "Audit.NET.DynamoDB\bin\release\*.symbols.nupkg"
del "Audit.HttpClient\bin\release\*.symbols.nupkg"
del "Audit.NET.NLog\bin\release\*.symbols.nupkg"
del "Audit.NET.AmazonQLDB\bin\release\*.symbols.nupkg"
del "Audit.NET.Kafka\bin\release\*.symbols.nupkg"
del "Audit.NET.AzureStorageTables\bin\release\*.symbols.nupkg"
del "Audit.NET.Serilog\bin\release\*.symbols.nupkg"
del "Audit.MongoClient\bin\release\*.symbols.nupkg"
del "Audit.NET.Polly\bin\release\*.symbols.nupkg"
del "Audit.NET.Channels\bin\release\*.symbols.nupkg"
del "Audit.EntityFramework.Abstractions\bin\release\*.symbols.nupkg"

nuget push "audit.net\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.JsonNewtonsoftAdapter\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.mvc\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.Mvc.Core\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.webapi\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.WebApi.Core\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.azurecosmos\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.mongodb\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.sqlserver\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.net.mysql\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Core\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Identity\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.EntityFramework.Identity.Core\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "audit.Wcf\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.Wcf.Client\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AzureStorage\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AzureStorageBlobs\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.DynamicProxy\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Udp\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Redis\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.PostgreSql\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.RavenDB\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.FileSystem\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.SignalR\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.log4net\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Elasticsearch\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.EventLog.Core\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.DynamoDB\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.HttpClient\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.NLog\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AmazonQLDB\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Kafka\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.AzureStorageTables\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Serilog\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.MongoClient\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Polly\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.NET.Channels\bin\release\*.nupkg" -NoSymbols -source %1
nuget push "Audit.EntityFramework.Abstractions\bin\release\*.nupkg" -NoSymbols -source %1
