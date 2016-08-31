del "audit.net\bin\debug\*.symbols.nupkg"
del "audit.mvc\bin\debug\*.symbols.nupkg"
del "audit.webapi\bin\debug\*.symbols.nupkg"
del "audit.net.azuredocumentdb\bin\debug\*.symbols.nupkg"
del "audit.net.mongodb\bin\debug\*.symbols.nupkg"
del "audit.net.sqlserver\bin\debug\*.symbols.nupkg"

nuget push "audit.net\bin\debug\*.nupkg" -source %1
nuget push "audit.mvc\bin\debug\*.nupkg" -source %1
nuget push "audit.webapi\bin\debug\*.nupkg" -source %1
nuget push "audit.net.azuredocumentdb\bin\debug\*.nupkg" -source %1
nuget push "audit.net.mongodb\bin\debug\*.nupkg" -source %1
nuget push "audit.net.sqlserver\bin\debug\*.nupkg" -source %1
