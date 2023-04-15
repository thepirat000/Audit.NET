start "KAFKA" /D D:\kafka "D:\kafka\start-server-reset.cmd"
start wsl -d ubuntu redis-server
start "ELASTIC" /D D:\Elasticsearch\elasticsearch-7.5.0\bin "D:\Elasticsearch\elasticsearch-7.5.0\bin\elasticsearch.bat"
start "DYNAMO" /D D:\DynamoDb "D:\DynamoDb\Run.bat"
start "RavenDB" /D D:\RavenDB powershell D:\RavenDB\run.ps1
net start mongodb
net start mysql80
net start mssqlserver
net start postgresql-x64-14