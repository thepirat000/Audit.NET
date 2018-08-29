@echo off
cls

del "audit.net\bin\debug\*.symbols.nupkg"
del "audit.mvc\bin\debug\*.symbols.nupkg"
del "audit.mvc.core\bin\debug\*.symbols.nupkg"
del "audit.webapi\bin\debug\*.symbols.nupkg"
del "audit.webapi.core\bin\debug\*.symbols.nupkg"
del "audit.net.azuredocumentdb\bin\debug\*.symbols.nupkg"
del "audit.net.mongodb\bin\debug\*.symbols.nupkg"
del "audit.net.sqlserver\bin\debug\*.symbols.nupkg"
del "audit.net.mysql\bin\debug\*.symbols.nupkg"
del "audit.entityframework\bin\debug\*.symbols.nupkg"
del "audit.entityframework.core\bin\debug\*.symbols.nupkg"
del "audit.entityframework.identity\bin\debug\*.symbols.nupkg"
del "audit.entityframework.identity.core\bin\debug\*.symbols.nupkg"
del "audit.Wcf\bin\debug\*.symbols.nupkg"
del "audit.net.azurestorage\bin\debug\*.symbols.nupkg"
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

nuget push "audit.net\bin\debug\*.nupkg" -source %1
nuget push "audit.mvc\bin\debug\*.nupkg" -source %1
nuget push "Audit.Mvc.Core\bin\debug\*.nupkg" -source %1
nuget push "audit.webapi\bin\debug\*.nupkg" -source %1
nuget push "Audit.WebApi.Core\bin\debug\*.nupkg" -source %1
nuget push "audit.net.azuredocumentdb\bin\debug\*.nupkg" -source %1
nuget push "audit.net.mongodb\bin\debug\*.nupkg" -source %1
nuget push "audit.net.sqlserver\bin\debug\*.nupkg" -source %1
nuget push "audit.net.mysql\bin\debug\*.nupkg" -source %1
nuget push "audit.EntityFramework\bin\debug\*.nupkg" -source %1
nuget push "audit.EntityFramework.Core\bin\debug\*.nupkg" -source %1
nuget push "audit.EntityFramework.Identity\bin\debug\*.nupkg" -source %1
nuget push "audit.EntityFramework.Identity.Core\bin\debug\*.nupkg" -source %1
nuget push "audit.Wcf\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.AzureStorage\bin\debug\*.nupkg" -source %1
nuget push "Audit.DynamicProxy\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.Udp\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.Redis\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.PostgreSql\bin\debug\*.nupkg" -source %1
nuget push "Audit.FileSystem\bin\debug\*.nupkg" -source %1
nuget push "Audit.SignalR\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.log4net\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.Elasticsearch\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.EventLog.Core\bin\debug\*.nupkg" -source %1
nuget push "Audit.NET.DynamoDB\bin\debug\*.nupkg" -source %1

