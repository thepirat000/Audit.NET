function StartDotnetUnitTests ([String]$project, [String]$title, [String]$extraParams='', [int32]$delay=0) {
    start-process powershell -argumentlist ".\_execDotnetTest.ps1 -projects:$project -title:$title -extraParams:'$extraParams' -delay:$delay";
}

[Console]::Title='RUNNER: Build'

# Build solution
& dotnet build ..\audit.net.sln -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed !!!" -foregroundcolor white -BackgroundColor red
    EXIT 1
}
& 'C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe' Audit.EntityFramework.UnitTest -p:Configuration=Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed !!!" -foregroundcolor white -BackgroundColor red
    EXIT 1
}

[Console]::Title='RUNNER: Start parallel tests'
clear

# Run parallel tests
StartDotnetUnitTests 'Audit.Mvc.UnitTest' 'MVC';
StartDotnetUnitTests 'Audit.JsonAdapter.UnitTest' 'JsonAdapter';
StartDotnetUnitTests 'Audit.WebApi.UnitTest' 'WebApi';
StartDotnetUnitTests 'Audit.DynamicProxy.UnitTest' 'DynamicProxy';
StartDotnetUnitTests 'Audit.Redis.UnitTest' 'Redis';
StartDotnetUnitTests 'Audit.Wcf.UnitTest' 'Wcf';
StartDotnetUnitTests 'Audit.RavenDB.UnitTest' 'RavenDB';
StartDotnetUnitTests 'Audit.SqlServer.UnitTest' 'SqlServer';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Integration' '--filter=TestCategory!=AzureDocDb&TestCategory!=AzureBlob&TestCategory!=AzureStorageBlobs&TestCategory!=WCF&TestCategory!=Elasticsearch&TestCategory!=Dynamo&TestCategory!=PostgreSQL&TestCategory!=Kafka&TestCategory!=AmazonQLDB&TestCategory!=Mongo&TestCategory!=MySql';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Mongo' '--filter=TestCategory=Mongo';
StartDotnetUnitTests 'Audit.MongoClient.UnitTest' 'MongoClient';
StartDotnetUnitTests 'Audit.IntegrationTest' 'MySql' '--filter=TestCategory=MySql';
StartDotnetUnitTests 'Audit.IntegrationTest' 'PostgreSQL' '--filter=TestCategory=PostgreSQL';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AzureDocDb' '--filter=TestCategory=AzureDocDb';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AzureStorage' '--filter=TestCategory=AzureBlob|TestCategory=AzureStorageBlobs|TestCategory=AzureTables';
StartDotnetUnitTests 'Audit.AzureStorageTables.UnitTest' 'AzureTables';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Elasticsearch' '--filter=TestCategory=Elasticsearch';
StartDotnetUnitTests 'Audit.Integration.AspNetCore' 'AspNetCore';

StartDotnetUnitTests 'Audit.IntegrationTest' 'Kafka' '--filter=TestCategory=Kafka';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Dynamo' '--filter=TestCategory=Dynamo';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AmazonQLDB' '--filter=TestCategory=AmazonQLDB';

# Run sequential tests
$hasFailed = $false;

[Console]::Title='RUN: 1/9 EF_LocalDb' ; 
& ..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe Audit.EntityFramework.UnitTest\bin\Release\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=LocalDb ;
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
[Console]::Title='RUN: 2/9 EF_Sql' ; 
& ..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe Audit.EntityFramework.UnitTest\bin\Release\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Sql ;
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
[Console]::Title='RUN: 3/9 EF_Stress' ; 
& ..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe Audit.EntityFramework.UnitTest\bin\Release\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Stress ;
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.IntegrationTest -extraParams:'--filter=TestCategory=WCF&TestCategory!=Async' -title:'4/9 WCF_Sync' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.IntegrationTest -extraParams:'--filter=TestCategory=WCF&TestCategory=Async' -title:'5/9 WCF_Async' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Core.UnitTest -title:'6/9 EF_CORE' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Core.v3.UnitTest -title:'7/9 EF_CORE_V3' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Full.UnitTest -title:'8/9 EF_FULL' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:'Audit.UnitTest' -title:'9/9 UnitTest' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}

if ($hasFailed) {
    [Console]::Title='RUNNER: Failed'
    Write-Host "   At least one sequential test has Failed !!!   " -foregroundcolor white -BackgroundColor red
} else {
    [Console]::Title='RUNNER: Completed'
    Write-Host "   Sequential Tests completed Sucessfully !!!   " -foregroundcolor white -BackgroundColor green
}
