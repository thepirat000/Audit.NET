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

& .\_testPublicKey.ps1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Public key validation Failed !!!" -foregroundcolor white -BackgroundColor red
    EXIT 1
}

[Console]::Title='RUNNER: Start parallel tests'
clear

# Run parallel tests
StartDotnetUnitTests 'Audit.FileSystem.UnitTest' 'FileSystem';
StartDotnetUnitTests 'Audit.HttpClient.UnitTest' 'HttpClient';
StartDotnetUnitTests 'Audit.Mvc.UnitTest' 'MVC';
StartDotnetUnitTests 'Audit.JsonAdapter.UnitTest' 'JsonAdapter';
StartDotnetUnitTests 'Audit.WebApi.UnitTest' 'WebApi';
StartDotnetUnitTests 'Audit.DynamicProxy.UnitTest' 'DynamicProxy';
StartDotnetUnitTests 'Audit.Redis.UnitTest' 'Redis';
StartDotnetUnitTests 'Audit.Wcf.UnitTest' 'Wcf.Server';
StartDotnetUnitTests 'Audit.Wcf.Client.UnitTest' 'Wcf.Client';
StartDotnetUnitTests 'Audit.RavenDB.UnitTest' 'RavenDB';
StartDotnetUnitTests 'Audit.SqlServer.UnitTest' 'SqlServer';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Integration' '--filter=TestCategory!=AzureDocDb&TestCategory!=AzureBlob&TestCategory!=AzureStorageBlobs&TestCategory!=WCF&TestCategory!=Elasticsearch&TestCategory!=Dynamo&TestCategory!=PostgreSQL&TestCategory!=Kafka&TestCategory!=AmazonQLDB&TestCategory!=Mongo&TestCategory!=MySql';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Mongo' '--filter=TestCategory=Mongo';
StartDotnetUnitTests 'Audit.MongoClient.UnitTest' 'MongoClient';
StartDotnetUnitTests 'Audit.IntegrationTest' 'MySql' '--filter=TestCategory=MySql';
StartDotnetUnitTests 'Audit.IntegrationTest' 'PostgreSQL' '--filter=TestCategory=PostgreSQL';
StartDotnetUnitTests 'Audit.AzureCosmos.UnitTest' 'AzureCosmos';
StartDotnetUnitTests 'Audit.AzureStorageBlobs.UnitTest' 'AzureStorageBlobs';
StartDotnetUnitTests 'Audit.DynamoDB.UnitTest' 'DynamoDB';
StartDotnetUnitTests 'Audit.Elasticsearch.UnitTest' 'Elasticsearch';
StartDotnetUnitTests 'Audit.Kafka.UnitTest' 'Kafka';
StartDotnetUnitTests 'Audit.MongoDB.UnitTest' 'MongoDB';
StartDotnetUnitTests 'Audit.MySql.UnitTest' 'MySql';
StartDotnetUnitTests 'Audit.PostgreSql.UnitTest' 'PostgreSql';
StartDotnetUnitTests 'Audit.Serilog.UnitTest' 'Serilog';
StartDotnetUnitTests 'Audit.AmazonQLDB.UnitTest' 'AmazonQLDB';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Elasticsearch' '--filter=TestCategory=Elasticsearch';
StartDotnetUnitTests 'Audit.Integration.AspNetCore' 'AspNetCore';

# Run sequential tests
$hasFailed = $false;

& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Core.UnitTest -title:'1/3 EF_CORE' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}

& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Full.UnitTest -title:'2/3 EF_FULL' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}

& .\_execDotnetTest.ps1 -projects:'Audit.UnitTest' -title:'3/3 UnitTest' -nopause
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
