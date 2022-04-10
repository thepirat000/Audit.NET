net stop mssqlserver
net stop mysql80
net stop postgresql-x64-14
taskkill /f /im Raven.Server.exe
taskkill /f /im java.exe
taskkill /f /im mongod.exe
taskkill /f /im redis-server.exe
