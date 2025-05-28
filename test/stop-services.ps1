net stop mongodb
net stop mssqlserver
net stop mysql80
net stop postgresql-x64-14
taskkill /f /im Raven.Server.exe
taskkill /f /im java.exe
taskkill /f /im azurite.exe
taskkill /f /im immu.exe
docker stop cosmos-emulator
wsl --shutdown
