start wsl "-d ubuntu sh -c `"~/kafka_2.13-3.0.0/bin/zookeeper-server-start.sh ~/kafka_2.13-3.0.0/config/zookeeper.properties & sleep 15 & ~/kafka_2.13-3.0.0/bin/kafka-server-start.sh ~/kafka_2.13-3.0.0/config/server.properties`""
start wsl "-d ubuntu redis-server"
start "D:\Elasticsearch\elasticsearch-7.5.0\bin\elasticsearch.bat" -workingdirectory "D:\Elasticsearch\elasticsearch-7.5.0\bin"
start "D:\DynamoDb\Run.bat" -workingdirectory "D:\DynamoDb"
start powershell {"D:\RavenDB\run.ps1"}
net start mongodb
net start mysql80
net start mssqlserver
net start postgresql-x64-14
