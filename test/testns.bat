@echo off
start "" /D D:\DynamoDB "D:\DynamoDB\run.bat"
start "" "D:\Program Files\MongoDB\Server\3.4\bin\mongod.exe"
start "" /D c:\redis "c:\redis\redis-server.exe"
net start mysql57
net start mssqlserver
net start postgresql-x64-9.6
net start elasticsearch

cd ..
dotnet restore
cd test

cd Audit.Mvc.UnitTest
echo ------ RUNNING MVC UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"
echo continue...

echo Running...

cd ..

cd Audit.UnitTest
echo ------ RUNNING GENERAL UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"
echo continue...

echo Running...
cd ..

cd Audit.WebApi.UnitTest
echo ------ RUNNING WEB API UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"
echo continue...

echo Running...

cd ..

cd Audit.Integration.AspNetCore
echo ------ RUNNING ASP NET CORE UNIT TESTS ------
dotnet run
echo continue...
cd ..

cd Audit.DynamicProxy.UnitTest
echo ------ RUNNING DYNAMIC PROXY UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"
echo continue...

echo Running...
cd ..

cd Audit.EntityFramework.Core.UnitTest
echo ------ RUNNING EF CORE UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"

echo Running...
cd ..

cd Audit.EntityFramework.UnitTest
echo ------ RUNNING EF FULL UNIT TESTS ------
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
..\..\packages\NUnit.ConsoleRunner.3.7.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Sql
..\..\packages\NUnit.ConsoleRunner.3.7.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=LocalDb
..\..\packages\NUnit.ConsoleRunner.3.7.0\tools\nunit3-console.exe bin\Debug\Audit.EntityFramework.UnitTest.dll --noresult --where=cat=Stress
echo continue...

echo Running...
cd ..

cd Audit.Redis.UnitTest
echo ------ RUNNING REDIS UNIT TESTS ------
dotnet test --logger:"console;verbosity=normal"
echo continue...

echo Running...
cd ..

cd Audit.IntegrationTest
echo ------ RUNNING GENERAL INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory!=AzureDocDb&TestCategory!=AzureBlob&TestCategory!=WCF&TestCategory!=Elasticsearch&TestCategory!=Dynamo"
echo ------ RUNNING Azure Cosmos DB INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=AzureDocDb"
echo ------ RUNNING Azure BLOB INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=AzureBlob"
echo ------ RUNNING WCF SYNC INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" -f net451 --filter "TestCategory=WCF&TestCategory!=Async"
echo ------ RUNNING WCF ASYNC INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" -f net451 --filter "TestCategory=WCF&TestCategory=Async"
echo ------ RUNNING ELASTICSEARCH INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=Elasticsearch"
echo ------ RUNNING DYNAMO DB INTEGRATION TEST ------
dotnet test --logger:"console;verbosity=normal" --filter "TestCategory=Dynamo"
echo continue...

cd ..

net stop mssqlserver
net stop mysql57
net stop postgresql-x64-9.6
net stop elasticsearch
taskkill /f /im mongod.exe
taskkill /f /im redis-server.exe