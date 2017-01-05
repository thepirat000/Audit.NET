del "audit.net\bin\debug\*.nupkg"
del "audit.mvc\bin\debug\*.nupkg"
del "audit.webapi\bin\debug\*.nupkg"
del "audit.net.azuredocumentdb\bin\debug\*.nupkg"
del "audit.net.mongodb\bin\debug\*.nupkg"
del "audit.net.sqlserver\bin\debug\*.nupkg"
del "audit.entityframework\bin\debug\*.nupkg"
del "audit.WCF\bin\debug\*.nupkg"
del "audit.net.azurestorage\bin\debug\*.nupkg"
del "audit.dynamicproxy\bin\debug\*.nupkg"

del "StrongName\audit.net.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.mvc.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.webapi.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.net.azuredocumentdb.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.net.sqlserver.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.entityframework.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.WCF.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.net.azurestorage.StrongName\bin\debug\*.nupkg"
del "StrongName\audit.dynamicproxy.StrongName\bin\debug\*.nupkg"

copy ..\docs\Audit.NET.snk .\StrongName\Audit.NET.snk /Y

dotnet build "Audit.NET/" 
dotnet build "Audit.Mvc/"
dotnet build "Audit.WebApi/"
dotnet build "Audit.NET.AzureDocumentDB/"
dotnet build "Audit.NET.MongoDB/"
dotnet build "Audit.NET.SqlServer/"
dotnet build "Audit.EntityFramework/"
dotnet build "Audit.Wcf/"
dotnet build "Audit.NET.AzureStorage/"
dotnet build "Audit.DynamicProxy/"

dotnet build "StrongName/Audit.NET.StrongName/" --no-incremental
dotnet build "StrongName/Audit.Mvc.StrongName/" --no-incremental
dotnet build "StrongName/Audit.WebApi.StrongName/" --no-incremental
dotnet build "StrongName/Audit.NET.AzureDocumentDB.StrongName/" --no-incremental
dotnet build "StrongName/Audit.NET.SqlServer.StrongName/" --no-incremental
dotnet build "StrongName/Audit.EntityFramework.StrongName/" --no-incremental
dotnet build "StrongName/Audit.Wcf.StrongName/" --no-incremental
dotnet build "StrongName/Audit.NET.AzureStorage.StrongName/" --no-incremental
dotnet build "StrongName/Audit.DynamicProxy.StrongName/" --no-incremental

dotnet pack "Audit.NET/"
dotnet pack "Audit.Mvc/"
dotnet pack "Audit.WebApi/"
dotnet pack "Audit.NET.AzureDocumentDB/"
dotnet pack "Audit.NET.MongoDB/"
dotnet pack "Audit.NET.SqlServer/"
dotnet pack "Audit.EntityFramework/"
dotnet pack "Audit.Wcf/"
dotnet pack "Audit.NET.AzureStorage/"
dotnet pack "Audit.DynamicProxy/"

dotnet pack "StrongName/Audit.NET.StrongName/"
dotnet pack "StrongName/Audit.Mvc.StrongName/"
dotnet pack "StrongName/Audit.WebApi.StrongName/"
dotnet pack "StrongName/Audit.NET.AzureDocumentDB.StrongName/"
dotnet pack "StrongName/Audit.NET.SqlServer.StrongName/"
dotnet pack "StrongName/Audit.EntityFramework.StrongName/"
dotnet pack "StrongName/Audit.Wcf.StrongName/"
dotnet pack "StrongName/Audit.NET.AzureStorage.StrongName/"
dotnet pack "StrongName/Audit.DynamicProxy.StrongName/"

