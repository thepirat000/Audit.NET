start wsl "-d ubuntu sh -c `"~/kafka_2.13-3.0.0/bin/zookeeper-server-start.sh ~/kafka_2.13-3.0.0/config/zookeeper.properties & sleep 15 & ~/kafka_2.13-3.0.0/bin/kafka-server-start.sh ~/kafka_2.13-3.0.0/config/server.properties`""
start wsl "-d ubuntu redis-server"
start "D:\Elasticsearch\elasticsearch-8.14.3\bin\elasticsearch.bat" -workingdirectory "D:\Elasticsearch\elasticsearch-8.14.3\bin"
start "D:\DynamoDb\Run.bat" -workingdirectory "D:\DynamoDb"
start powershell {"D:\RavenDB\run.ps1"}
start "D:\opensearch-2.19.1-windows-x64\opensearch-2.19.1\bin\opensearch.bat" -workingdirectory "D:\opensearch-2.19.1-windows-x64\opensearch-2.19.1\bin"
start "azurite.exe" -workingdirectory "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator" 
docker run --detach --publish 8081:8081 --publish 1234:1234 --name cosmos-emulator mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview -protocol https

net start mongodb
net start mysql80
net start mssqlserver
net start postgresql-x64-14
