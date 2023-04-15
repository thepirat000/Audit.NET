start "D:\kafka\start-server-reset.cmd" -workingdirectory "D:\Kafka"
start wsl "-d ubuntu redis-server"
start "D:\Elasticsearch\elasticsearch-7.5.0\bin\elasticsearch.bat" -workingdirectory "D:\Elasticsearch\elasticsearch-7.5.0\bin"
start "D:\DynamoDb\Run.bat" -workingdirectory "D:\DynamoDb"
start powershell {"D:\RavenDB\run.ps1"}
net start mongodb
net start mysql80
net start mssqlserver
net start postgresql-x64-14
