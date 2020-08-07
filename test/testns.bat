@echo off

title Audit.NET Unit Tests Runner

echo Disabling GitHub nuget source to avoid conflicts
nuget sources disable -name GitHub
echo.


start "REDIS" /D D:\redis "D:\redis\redis-server.exe"
start "ELASTIC" /D D:\Elasticsearch\elasticsearch-7.5.0\bin "D:\Elasticsearch\elasticsearch-7.5.0\bin\elasticsearch.bat"
start "MONGO" "D:\Program Files\MongoDB\Server\3.4\bin\mongod.exe"
net start mysql80
net start mssqlserver
net start postgresql-x64-9.6

cd ..
dotnet restore
cd test

cd Audit.Mvc.UnitTest
echo ---------------------------------------------- RUNNING MVC UNIT TESTS (1/17) ----------------------------------------------
TITLE RUNNING MVC UNIT TESTS (1/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...

cd ..

cd Audit.UnitTest
echo ---------------------------------------------- RUNNING GENERAL UNIT TESTS (2/17) ----------------------------------------------
TITLE RUNNING GENERAL UNIT TESTS (2/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...
cd ..


cd Audit.WebApi.UnitTest
echo ---------------------------------------------- RUNNING WEB API UNIT TESTS (3/17) ----------------------------------------------
TITLE RUNNING WEB API UNIT TESTS (3/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...

cd ..

cd Audit.Integration.AspNetCore
echo ---------------------------------------------- RUNNING ASP NET CORE UNIT TESTS (4/17) ----------------------------------------------
TITLE RUNNING ASP NET CORE UNIT TESTS (4/17)
dotnet run
cd ..

cd Audit.DynamicProxy.UnitTest
echo ---------------------------------------------- RUNNING DYNAMIC PROXY UNIT TESTS (5/17) ----------------------------------------------
TITLE RUNNING DYNAMIC PROXY UNIT TESTS (5/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...
cd ..

cd Audit.EntityFramework.Core.UnitTest
echo ---------------------------------------------- RUNNING EF CORE UNIT TESTS (6/17) ----------------------------------------------
TITLE RUNNING EF CORE UNIT TESTS (6/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...
cd ..

cd Audit.EntityFramework.UnitTest
echo ---------------------------------------------- RUNNING EF FULL UNIT TESTS (7/17) ----------------------------------------------
TITLE RUNNING EF FULL UNIT TESTS (7/17)
IF NOT EXIST "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
	echo The file does not exist
        exit /B 1
)
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
..\..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=LocalDb
..\..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Sql
..\..\packages\NUnit.ConsoleRunner.3.8.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Stress

echo Running...
cd ..

cd Audit.EntityFramework.Full.UnitTest
echo ---------------------------------------------- RUNNING EF FULL UNIT TESTS (8/17) ----------------------------------------------
TITLE RUNNING EF FULL UNIT TESTS (8/17)
dotnet test --logger:"console;verbosity=normal"
echo Running...
cd ..

cd Audit.Redis.UnitTest
echo ---------------------------------------------- RUNNING REDIS UNIT TESTS (9/17) ----------------------------------------------
TITLE RUNNING REDIS UNIT TESTS (9/17)
dotnet test --logger:"console;verbosity=normal"

echo Running...
cd ..

cd Audit.IntegrationTest
echo ---------------------------------------------- RUNNING GENERAL INTEGRATION TEST (10/16) ----------------------------------------------
TITLE RUNNING GENERAL INTEGRATION TEST (10/16)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory!=AzureDocDb&TestCategory!=AzureBlob&TestCategory!=WCF&TestCategory!=Elasticsearch&TestCategory!=Dynamo&TestCategory!=PostgreSQL"
echo ---------------------------------------------- RUNNING PostgreSQL INTEGRATION TEST (11/17) ----------------------------------------------
TITLE RUNNING PostgreSQL INTEGRATION TEST (11/17)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=PostgreSQL"
echo ---------------------------------------------- RUNNING Azure Cosmos DB INTEGRATION TEST (12/17) ----------------------------------------------
TITLE Azure Cosmos DB INTEGRATION TEST (12/17)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=AzureDocDb"
echo ---------------------------------------------- RUNNING Azure BLOB INTEGRATION TEST (13/17) ----------------------------------------------
TITLE RUNNING Azure BLOB INTEGRATION TEST (13/17)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=AzureBlob"
echo ---------------------------------------------- RUNNING WCF SYNC INTEGRATION TEST (14/17) ----------------------------------------------
TITLE RUNNING WCF SYNC INTEGRATION TEST (14/17)
dotnet test --logger:"console;verbosity=normal" -f net452 --filter "TestCategory=WCF&TestCategory!=Async"
echo ---------------------------------------------- RUNNING WCF ASYNC INTEGRATION TEST (15/17) ----------------------------------------------
TITLE RUNNING WCF ASYNC INTEGRATION TEST (15/17)
dotnet test --logger:"console;verbosity=normal" -f net452 --filter "TestCategory=WCF&TestCategory=Async"
echo ---------------------------------------------- RUNNING ELASTICSEARCH INTEGRATION TEST (16/17) ----------------------------------------------
TITLE RUNNING ELASTICSEARCH INTEGRATION TEST (16/17)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=Elasticsearch"
echo ---------------------------------------------- RUNNING DYNAMO DB INTEGRATION TEST (17/17) ----------------------------------------------
TITLE RUNNING DYNAMO DB INTEGRATION TEST (17/17)
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=Dynamo"

cd ..

title Audit.NET Unit Tests Runner

ECHO.
ECHO --- PRESS ENTER TO STOP THE SERVICES (or CTRL+C to cancel)
pause>nul

net stop mssqlserver
net stop mysql80
net stop postgresql-x64-9.6
taskkill /f /im java.exe
taskkill /f /im mongod.exe
taskkill /f /im redis-server.exe

echo Enabling GitHub nuget source
nuget sources enable -name GitHub
echo.
