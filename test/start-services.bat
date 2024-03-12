start wsl -d ubuntu sh -c "~/kafka_2.13-3.0.0/bin/zookeeper-server-start.sh ~/kafka_2.13-3.0.0/config/zookeeper.properties & sleep 15 & ~/kafka_2.13-3.0.0/bin/kafka-server-start.sh ~/kafka_2.13-3.0.0/config/server.properties"
start wsl -d ubuntu redis-server
start "ELASTIC" /D D:\Elasticsearch\elasticsearch-7.5.0\bin "D:\Elasticsearch\elasticsearch-7.5.0\bin\elasticsearch.bat"
start "DYNAMO" /D D:\DynamoDb "D:\DynamoDb\Run.bat"
start "RavenDB" /D D:\RavenDB powershell D:\RavenDB\run.ps1
start "Azurite" /D "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator" azurite.exe
net start mongodb
net start mysql80
net start mssqlserver
net start postgresql-x64-14