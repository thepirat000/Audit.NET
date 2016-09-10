# Audit.EntityFramework

**EntityFramework (EF) Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Automatically generates Audit Logs for EntityFramework's CRUD operations. **Supporting EF 6 and EF 7 (EF Core)**.

Audit.EntityFramework provides the infrastructure to log interactions with the EF `DbContext`. It can record detailed information about CRUD operations in your database.

##Install

**[NuGet Package](https://www.nuget.org/packages/Audit.EntityFramework/)**
```
PM> Install-Package Audit.EntityFramework
```

##Usage
Change your EF Context class to inherit from `Audit.EntityFramework.AuditDbContext` instead of `DbContext`. 

For example if you have a context like this:

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

The following settings can be configured per DbContext or globally:

- **Mode**: To indicate the audit operation mode
 - _Opt-Out_: All the entities are tracked by default, except those explicitly ignored. (Default)
 - _Opt-In_: No entity is tracked by default, except those explicitly included.
- **IncludeEntityObjects**: To indicate if the output should contain the complete entity object graphs. (Default is false)
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the context name). Can contain the following placeholders: 
  - {context}: replaced with the Db Context type name.
  - {database}: replaced with the database name.


Change the settings by decorating your DbContext with the `AuditDbContext` attribute, for example:

```c#
[AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = false, AuditEventType = "{database}_{context}" )]
public class MyEntitites : Audit.EntityFramework.AuditDbContext
{
...
```

To exclude specific entities from the audit (OptOut Mode), you can decorate your entity classes with the `AuditIgnore` attribute, for example:
```c#
[AuditIgnore]
public class Blog
{
    public int Id { get; set; }
    ...
}
```

Instead, to include specific entities to the audit (OptIn Mode), you can use the `AuditInclude` attribute:
```c#
[AuditInclude]
public class Post
{
    public int Id { get; set; }
    ...
}
```

You can also change the settings of your DbContext by accessing the properties with the same name as in the attribute. For example:
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

You can also configure settings by using a convenient [Fluent API](http://martinfowler.com/bliki/FluentInterface.html) provided by the method `Audit.EntityFramework.Configuration.Setup()`, this is the most straightforward way to configure the library.

For example, to configure a context called `MyEntities`, that should include the objects on the output, using the OptOut mode, excluding the entities `PostHistory` and `BlogHistory` from the audit:
```c#
Audit.EntityFramework.Configuration.Setup()
    .ForContext<MyEntities>(config => config
        .IncludeEntityObjects()
        .AuditEventType("{context}:{database}"))
    .UseOptOut()
        .Ignore<PostHistory>()
        .Ignore<BlogHistory>;
```

In summary, you have three ways to configure the audit for the contexts:
- By accessing the properties on the `AuditDbContext` base class.
- By decorating your context classes with `AuditDbContext` attribute and your entity classes with `AuditIgnore`/`AuditInclude` attributes.
- By using the fluent API provided by the method `Audit.EntityFramework.Configuration.Setup()`

All three can be used at the same time, and the precedence order is the order exposed in the above list.

To configure the output persistence mechanism please see [Event Output Configuration](https://github.com/thepirat000/Audit.NET/blob/master/README.md#event-output).

##How it works
The library intercepts calls to `SaveChanges` / `SaveChangesAsync` methods on the `DbContext` and generates detailed audit logs. Each call to `SaveChanges` generates a new audit event that includes information of all the entities affected by the save operation.

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

##Output details

The following tables describes the Audit.EntityFramework output fields:

- ###[EntityFrameworkEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EntityFrameworkEvent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Database** | string | Name of the database affected |
| **ConnectionId** | Guid | Unique client connection ID (only available when the connection is open at the beginning of the event) |
| **TransactionId** | string | Unique identifier for the DB transaction used on the audited operation (if any). To group events that are part of the same transaction. |
| **Entries** | Array of [EventEntry](#evententry) | Array with information about the entities affected by the audited operation |
| **Result** | integer | Result of the SaveChanges call. Is the number of objects affected by the operation. |
| **Success** | boolean | Boolean to indicate if the operation was successful |
| **ErrorMessage** | string | The exception thrown details (if any) |

- ###[EventEntry](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntry.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Table** | string | Name of the affected table |
| **Action** | string | Action type (Insert, Update or Delete) |
| **PrimaryKey** | Object | Object with the affected entity's primary key value(s) |
| **ColumnValues** | Object | Object with the affected entity's column values  |
| **Changes** | Array of [ChangeObject](#changeobject) | An array containing the modified columns with the original and new values (only available for Update operations) |
| **Entity** | Object | The object representation of the .NET entity affected (optional) |
| **Valid** | boolean | Bolean indicating if the entity passes the validations |
| **ValidationResults** | Array of string | The validation messages when the entity validation fails |

- ###[ChangeObject](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntryChange.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **ColumnName** | string | The column name that was updated |
| **OriginalValue** | string | The original value before the update |
| **NewValue** | string | The new value after the update |


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

Another way to customize the output is by using global custom actions, please see [custom actions](https://github.com/thepirat000/Audit.NET#custom-actions).

##Output samples
- Output sample for a failed insert operation:
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
		"ConnectionId": "593e082d-b6b5-440b-a048-ba223b247e9f",
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

- Output sample for a successful update+delete operation within a transaction:
```javascript
{
	"EventType": "Blogs_MyEntities",
	"Environment": {
		"UserName": "Federico",
		"MachineName": "HP",
		"DomainName": "HP",
		"CallingMethodName": "Audit.UnitTest.AuditTests.TestEF()",
		"Exception": null,
		"Culture": "en-GB"
	},
	"StartDate": "2016-09-07T11:36:16.2643822-05:00",
	"EndDate": "2016-09-07T11:36:20.410577-05:00",
	"Duration": 4146,
	"EntityFrameworkEvent": {
		"Database": "Blogs",
		"ConnectionId": "d37ddc34-8ecb-4f08-b95b-598807ff3cef",
		"TransactionId": "d37ddc34-8ecb-4f08-b95b-598807ff3cef_1",
		"Entries": [{
			"Table": "Blogs",
			"Action": "Update",
			"PrimaryKey": {
				"Id": 1
			},
			"Changes": [{
				"ColumnName": "BloggerName",
				"OriginalValue": "fede",
				"NewValue": "Federico"
			}],
			"Valid": true
		},
		{
			"Table": "Posts",
			"Action": "Delete",
			"PrimaryKey": {
				"Id": 5
			},
			"ColumnValues": {
				"Id": 5,
				"BlogId": 2,
				"Content": "this is an example",
				"DateCreated": "2016-09-07T11:36:10.973",
				"Title": "my post 5"
			},
			"Valid": true
		}],
		"Result": 2,
		"Success": true
	}
}
```
