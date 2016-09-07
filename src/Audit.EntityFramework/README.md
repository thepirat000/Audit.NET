# Audit.EntityFramework

**EntityFramework (EF) Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Automatically generates Audit Trails for EntityFramework's CRUD operations. **Supporting EF 6 and EF Core**.

Audit.EntityFramework provides the infrastructure to log interactions with the EF `DbContext`. It can record operations in the database with detailed information.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.EntityFramework/)**
```
PM> Install-Package Audit.EntityFramework
```

##Usage
Change your EF Context class to inherits from `Audit.EntityFramework.AuditDbContext` instead of `DbContext`. For example if you have a context like this:

```c#
public class MyEntities : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

to enable the audit log, you should change it to this:
```c#
public class MyEntities : Audit.EntityFramework.AuditDbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

##Configuration
You can change the default behavior by decorating your DbContext with the `AuditDbContext` attribute, indicating the setting values:

- **Mode**: To indicate the audit operation mode
 - _Opt-Out_: All the entities are tracked by default, except those decorated with the `AuditIgnore` attribute. (Default)
 - _Opt-In_: No entity is tracked by default, except those decorated with the `AuditInclude` attribute.
- **IncludeEntityObjects**: To indicate if the output should contain the complete entity object graphs. (Default is false)
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the context name). Can contain the following placeholders: 
  - {context}: replaced with the Db Context type name.
  - {database}: replaced with the database name.

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output-configuration).

For example:
```c#
[AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = false, AuditEventType = "{database}_{context}" )]
public class MyEntitites : Audit.EntityFramework.AuditDbContext
{
...
```

You can also change the settings by accessing the properties with the same name as in the attribute. For example:
```c#
public class MyEntities : Audit.EntityFramework.AuditDbContext
{
    public MyEntities()
    {
        AuditEventType = "{database}_{context}";
        Mode = AuditOptionMode.OptOut;
        IncludeEntityObjects = false;
    }
}
```

##How it works
The library intercepts the calls to `SaveChanges` / `SaveChangesAsync` methods on the `DbContext` to generate detailed audit logs. Each call to `SaveChanges` generates a new audit event that includes information of all the entities affected by the save operation.

##Output
Audit.EntityFramework output includes:
- Affected SQL database and table names
- Affected column data including primary key, original and new values
- Model validation results
- Exception details
- Transaction identifier (to group logs that are part of the same SQL transaction)
- Entity object graphs (optional with `IncludeEntityObjects` configuration)
- Execution time and duration
- Environment information such as user, machine, domain, locale, etc.

With this information, you can measure performance, observe exceptions thrown or get statistics about usage of your database.

##Customization
You can add extra information to the events by calling the method `AddAuditCustomField` on the `DbContext`. For example:

```c#
using(var context = new MyEntitites())
{
	...
	context.AddAuditCustomField("UserName", userName);
	...
	context.SaveChanges();
	
}
```

##Output samples
This is an example of the output for a failed insert operation:
```javascript
{
	"EventType": "Blogs_MyEntities",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.UnitTest.AuditTests.TestEF()",
		"Exception": "Exception: Exception from HRESULT: 0xE0434352",
		"Culture": "en-GB"
	},
	"StartDate": "2016-09-06T21:11:57.7562152-05:00",
	"EndDate": "2016-09-06T21:11:58.1039904-05:00",
	"Duration": 348,
	"EntityFrameworkEvent": {
		"Database": "Blogs",
		"TransactionId": "593e082d-b6b5-440b-a048-ba223b247e9f_1",
		"Entries": [{
			"Table": "Posts",
			"Action": "Insert",
			"PrimaryKey": {
				"Id": -2147482647
			},
			"ColumnValues": {
				"Id": -2147482647,
				"BlogId": 1,
				"Content": "content",
				"DateCreated": "2016-09-07T01:05:51.1972469-05:00",
				"Title": "title VERY LONG_________________"
			},
			"Valid": false,
			"ValidationResults": ["The field Title must be a string or array type with a maximum length of '20'."]
		}],
		"Result": 0,
		"Success": false,
		"ErrorMessage": "(DbUpdateException) An error occurred while updating the entries. See the inner exception for details. -> String or binary data would be truncated."
	}
}
```

