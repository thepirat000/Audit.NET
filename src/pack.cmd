@echo off
cls

del "audit.net\bin\release\*.nupkg"
del "Audit.NET.JsonNewtonsoftAdapter\bin\release\*.nupkg"
del "Audit.NET.JsonSystemAdapter\bin\release\*.nupkg"
del "audit.mvc\bin\release\*.nupkg"
del "audit.mvc.core\bin\release\*.nupkg"
del "audit.webapi\bin\release\*.nupkg"
del "audit.webapi.core\bin\release\*.nupkg"
del "audit.net.azurecosmos\bin\release\*.nupkg"
del "audit.net.mongodb\bin\release\*.nupkg"
del "audit.net.sqlserver\bin\release\*.nupkg"
del "audit.net.mysql\bin\release\*.nupkg"
del "audit.entityframework\bin\release\*.nupkg"
del "audit.entityframework.core\bin\release\*.nupkg"
del "audit.entityframework.core.v3\bin\release\*.nupkg"
del "audit.entityframework.Identity\bin\release\*.nupkg"
del "audit.entityframework.Identity.core\bin\release\*.nupkg"
del "audit.entityframework.Identity.core.v3\bin\release\*.nupkg"
del "audit.WCF\bin\release\*.nupkg"
del "audit.WCF.Client\bin\release\*.nupkg"
del "audit.net.azurestorage\bin\release\*.nupkg"
del "audit.net.azurestorageblobs\bin\release\*.nupkg"
del "audit.dynamicproxy\bin\release\*.nupkg"
del "audit.net.udp\bin\release\*.nupkg"
del "audit.net.redis\bin\release\*.nupkg"
del "Audit.NET.PostgreSql\bin\release\*.nupkg"
del "Audit.NET.RavenDB\bin\release\*.nupkg"
del "Audit.FileSystem\bin\release\*.nupkg"
del "Audit.SignalR\bin\release\*.nupkg"
del "Audit.NET.log4net\bin\release\*.nupkg"
del "Audit.NET.Elasticsearch\bin\release\*.nupkg"
del "Audit.NET.EventLog.Core\bin\release\*.nupkg"
del "Audit.NET.DynamoDB\bin\release\*.nupkg"
del "Audit.HttpClient\bin\release\*.nupkg"
del "Audit.NET.NLog\bin\release\*.nupkg"
del "Audit.NET.AmazonQLDB\bin\release\*.nupkg"
del "Audit.NET.Kafka\bin\release\*.nupkg"
del "Audit.NET.AzureStorageTables\bin\release\*.nupkg"
del "Audit.NET.Serilog\bin\release\*.nupkg"

copy ..\docs\Audit.NET.snk .\StrongName\Audit.NET.snk /Y

dotnet build "..\Audit.NET.sln" -c Release

IF NOT ERRORLEVEL 0 GOTO error

dotnet pack "Audit.NET/" -c Release
dotnet pack "Audit.NET.JsonNewtonsoftAdapter/" -c Release
dotnet pack "Audit.NET.JsonSystemAdapter/" -c Release
dotnet pack "Audit.Mvc/" -c Release
dotnet pack "Audit.Mvc.Core/" -c Release
dotnet pack "Audit.WebApi/" -c Release
dotnet pack "Audit.WebApi.Core/" -c Release
dotnet pack "Audit.NET.AzureCosmos/" -c Release
dotnet pack "Audit.NET.MongoDB/" -c Release
dotnet pack "Audit.NET.SqlServer/" -c Release
dotnet pack "Audit.NET.MySql/" -c Release
dotnet pack "Audit.EntityFramework/" -c Release
dotnet pack "Audit.EntityFramework.Core/" -c Release
dotnet pack "Audit.EntityFramework.Core.v3/" -c Release
dotnet pack "Audit.EntityFramework.Identity/" -c Release
dotnet pack "Audit.EntityFramework.Identity.Core/" -c Release
dotnet pack "Audit.EntityFramework.Identity.Core.v3/" -c Release
dotnet pack "Audit.Wcf/" -c Release
dotnet pack "Audit.Wcf.Client/" -c Release
dotnet pack "Audit.NET.AzureStorage/" -c Release
dotnet pack "Audit.NET.AzureStorageBlobs/" -c Release
dotnet pack "Audit.DynamicProxy/" -c Release
dotnet pack "Audit.NET.Udp/" -c Release
dotnet pack "Audit.NET.Redis/" -c Release
dotnet pack "Audit.NET.PostgreSql/" -c Release
dotnet pack "Audit.NET.RavenDB/" -c Release
dotnet pack "Audit.FileSystem/" -c Release
dotnet pack "Audit.SignalR/" -c Release
dotnet pack "Audit.NET.log4net/" -c Release
dotnet pack "Audit.NET.Elasticsearch/" -c Release
dotnet pack "Audit.NET.EventLog.Core/" -c Release
dotnet pack "Audit.NET.DynamoDB/" -c Release
dotnet pack "Audit.HttpClient/" -c Release
dotnet pack "Audit.NET.NLog/" -c Release
dotnet pack "Audit.NET.AmazonQLDB/" -c Release
dotnet pack "Audit.NET.Kafka/" -c Release
dotnet pack "Audit.NET.AzureStorageTables/" -c Release
dotnet pack "Audit.NET.Serilog/" -c Release

ECHO.
ECHO ADD TAG NOW !
ECHO git tag -a x.x.x -m x.x.x
ECHO git push --tags

exit /b 0
:error
echo ERROR BUILDING
exit /b 1