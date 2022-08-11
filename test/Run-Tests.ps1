function StartDotnetUnitTests ([String]$project, [String]$title, [String]$extraParams='') {
    start-process powershell -argumentlist ".\_execDotnetTest.ps1 -projects:$project -title:$title -extraParams:'$extraParams'";
}

function StartEfUnitTests ([String]$category) {
    start-process powershell -argumentlist "
        [Console]::Title='RUN: EF_$category' ; 
        ..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe Audit.EntityFramework.UnitTest\bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=$category ;
        [Console]::Title='END: EF_$category' ;
        pause
    ";

}

[Console]::Title='RUNNER: Build'

#Build solution
& dotnet build ..\audit.net.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed !!!" -foregroundcolor white -BackgroundColor red
    EXIT 1
}
& 'C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe' Audit.EntityFramework.UnitTest
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed !!!" -foregroundcolor white -BackgroundColor red
    EXIT 1
}

[Console]::Title='RUNNER: Start parallel tests'
clear

#Run parallel tests
StartDotnetUnitTests 'Audit.Mvc.UnitTest' 'MVC';
StartDotnetUnitTests 'Audit.JsonAdapter.UnitTest' 'JsonAdapter';
StartDotnetUnitTests 'Audit.WebApi.UnitTest' 'WebApi';
StartDotnetUnitTests 'Audit.DynamicProxy.UnitTest' 'DynamicProxy';
StartDotnetUnitTests 'Audit.Redis.UnitTest' 'Redis';
StartDotnetUnitTests 'Audit.Wcf.UnitTest' 'Wcf';
StartDotnetUnitTests 'Audit.RavenDB.UnitTest' 'RavenDB';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Integration' '--filter=TestCategory!=AzureDocDb&TestCategory!=AzureBlob&TestCategory!=WCF&TestCategory!=Elasticsearch&TestCategory!=Dynamo&TestCategory!=PostgreSQL&TestCategory!=Kafka&TestCategory!=AmazonQLDB';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Kafka' '--filter=TestCategory=Kafka';
StartDotnetUnitTests 'Audit.IntegrationTest' 'PostgreSQL' '--filter=TestCategory=PostgreSQL';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AzureDocDb' '--filter=TestCategory=AzureDocDb';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AzureBlob' '--filter=TestCategory=AzureBlob';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Elasticsearch' '--filter=TestCategory=Elasticsearch';
StartDotnetUnitTests 'Audit.IntegrationTest' 'Dynamo' '--filter=TestCategory=Dynamo';
StartDotnetUnitTests 'Audit.IntegrationTest' 'AmazonQLDB' '--filter=TestCategory=AmazonQLDB';

start-process powershell -argumentlist "[Console]::Title='RUN: AspNetCore' ; dotnet run --project Audit.Integration.AspNetCore ; pause";

StartEfUnitTests 'LocalDb';
StartEfUnitTests 'Sql';
StartEfUnitTests 'Stress';

#Run sequential tests
$hasFailed = $false;
[Console]::Title='RUNNER: Start sequential tests'
& .\_execDotnetTest.ps1 -projects:Audit.IntegrationTest -extraParams:'--filter=TestCategory=WCF&TestCategory!=Async' -title:'1/6 WCF_Sync' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.IntegrationTest -extraParams:'--filter=TestCategory=WCF&TestCategory=Async' -title:'2/6 WCF_Async' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Core.UnitTest -title:'3/6 EF_CORE' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Core.v3.UnitTest -title:'4/6 EF_CORE_V3' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:Audit.EntityFramework.Full.UnitTest -title:'5/6 EF_FULL' -nopause
if ($LASTEXITCODE -ne 0) {
    $hasFailed = $true;
}
& .\_execDotnetTest.ps1 -projects:'Audit.UnitTest' -title:'6/6 UnitTest' -nopause
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