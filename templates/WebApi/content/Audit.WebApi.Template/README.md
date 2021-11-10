### Further steps

1. Modify `Startup.cs` to fit your audit output configuration needs.
2. Modify `AuditConfiguration.cs` to customize the audit data provider.

Search for the string `// TODO` on code to see the places where you can customize the audit.

#### API samples

Description | Command
------------ | -------------- 
**Get all records** | ```curl -X GET http://localhost:50732/api/values -H "content-type: application/json"```
**Get record** | ```curl -X GET http://localhost:50732/api/values/1 -H "content-type: application/json"```
**Insert record** | ```curl -X POST http://localhost:50732/api/values -H "content-type: application/json" -d '"Some description"'```
**Update record** | ```curl -X PUT http://localhost:50732/api/values/1 -H "content-type: application/json" -d '"New description"'```
**Delete record** | ```curl -X DELETE http://localhost:50732/api/values/1 -H "content-type: application/json"```
**Delete multiple records** | ```curl -X DELETE http://localhost:50732/api/values/delete -H "content-type: application/json" -d '"2,3,4"'```

