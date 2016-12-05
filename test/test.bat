@echo off
start "" /D c:\ "c:\Program Files\MongoDB\Server\3.2\bin\mongod.exe"
net start mssqlserver


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

cd Audit.EntityFramework.Edmx.UnitTest
dotnet test
cd ..


del TestResult.xml /s

