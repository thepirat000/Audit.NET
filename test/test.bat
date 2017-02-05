@echo off
start "" /D c:\ "c:\Program Files\MongoDB\Server\3.4\bin\mongod.exe"
net start mssqlserver

dotnet restore

cd Audit.IntegrationTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.Mvc.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.WebApi.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.DynamicProxy.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.EntityFramework.UnitTest
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
..\..\packages\NUnit.ConsoleRunner.3.4.1\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult
echo continue...
pause > nul
cd ..

del TestResult.xml /s

