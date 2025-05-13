function StartDotnetUnitTests ([String]$project, [String]$title, [int32]$delay=0) {
    start-process powershell -argumentlist ".\_execDotnetTest.ps1 -projects:$project -title:$title -delay:$delay";
}

[Console]::Title='RUNNER: Build'

If (!(test-path "TestResult/"))
{
    md TestResult/
}

Get-ChildItem -Path "TestResult/" -File -Recurse | Remove-Item -Force

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
StartDotnetUnitTests 'Audit.AmazonQLDB.UnitTest' 'AmazonQLDB';
StartDotnetUnitTests 'Audit.AspNetCore.UnitTest' 'AspNetCore';
StartDotnetUnitTests 'Audit.AzureCosmos.UnitTest' 'AzureCosmos';
StartDotnetUnitTests 'Audit.AzureEventHubs.UnitTest' 'AzureEventHubs';
StartDotnetUnitTests 'Audit.AzureStorageBlobs.UnitTest' 'AzureStorageBlobs';
StartDotnetUnitTests 'Audit.DynamicProxy.UnitTest' 'DynamicProxy';
StartDotnetUnitTests 'Audit.DynamoDB.UnitTest' 'DynamoDB';
StartDotnetUnitTests 'Audit.Elasticsearch.UnitTest' 'Elasticsearch';
StartDotnetUnitTests 'Audit.FileSystem.UnitTest' 'FileSystem';
StartDotnetUnitTests 'Audit.HttpClient.UnitTest' 'HttpClient';
StartDotnetUnitTests 'Audit.JsonAdapter.UnitTest' 'JsonAdapter';
StartDotnetUnitTests 'Audit.Kafka.UnitTest' 'Kafka';
StartDotnetUnitTests 'Audit.MongoClient.UnitTest' 'MongoClient';
StartDotnetUnitTests 'Audit.MongoDB.UnitTest' 'MongoDB';
StartDotnetUnitTests 'Audit.Mvc.UnitTest' 'MVC';
StartDotnetUnitTests 'Audit.MySql.UnitTest' 'MySql';
StartDotnetUnitTests 'Audit.OpenSearch.UnitTest' 'OpenSearch';
StartDotnetUnitTests 'Audit.Polly.UnitTest' 'Polly';
StartDotnetUnitTests 'Audit.PostgreSql.UnitTest' 'PostgreSql';
StartDotnetUnitTests 'Audit.RavenDB.UnitTest' 'RavenDB';
StartDotnetUnitTests 'Audit.Redis.UnitTest' 'Redis';
StartDotnetUnitTests 'Audit.Serilog.UnitTest' 'Serilog';
StartDotnetUnitTests 'Audit.SqlServer.UnitTest' 'SqlServer';
StartDotnetUnitTests 'Audit.WebApi.UnitTest' 'WebApi';
StartDotnetUnitTests 'Audit.Wcf.Client.UnitTest' 'Wcf.Client';
StartDotnetUnitTests 'Audit.Wcf.UnitTest' 'Wcf.Server';


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
