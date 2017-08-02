@echo off
start "" /D c:\ "d:\Program Files\MongoDB\Server\3.4\bin\mongod.exe"
start "" /D c:\redis "c:\redis\redis-server.exe"
net start mysql57
net start mssqlserver

dotnet restore

cd Audit.Mvc.UnitTest
dotnet build
dotnet test -f netcoreapp1.0
echo continue...
pause > nul

dotnet test -f net451
echo continue...
pause > nul

..\..\packages\NUnit.ConsoleRunner.3.4.1\tools\nunit3-console.exe bin\Debug\net45\win7-x64\Audit.Mvc.UnitTest.dll
echo continue...
pause > nul

..\..\packages\NUnit.ConsoleRunner.3.4.1\tools\nunit3-console.exe bin\Debug\net40\win7-x64\Audit.Mvc.UnitTest.dll
echo continue...
pause > nul

cd ..

cd Audit.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.WebApi.UnitTest
dotnet build
dotnet test -f netcoreapp1.0
echo continue...
pause > nul

dotnet test -f net451
echo continue...
pause > nul

..\..\packages\NUnit.ConsoleRunner.3.4.1\tools\nunit3-console.exe bin\Debug\net45\win7-x64\Audit.WebApi.UnitTest.dll
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

cd Audit.Redis.UnitTest
dotnet test
echo continue...
pause > nul
cd ..

cd Audit.IntegrationTest
dotnet test
echo continue...
pause > nul
cd ..


del TestResult.xml /s

net stop mssqlserver
net stop mysql57
taskkill /f /im mongod.exe
taskkill /f /im redis-server.exe