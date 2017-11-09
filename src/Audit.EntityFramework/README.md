# Audit.EntityFramework

**EntityFramework (EF) Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Automatically generates Audit Logs for EntityFramework's CRUD operations. **Supporting EF 6 and EF 7 (EF Core)**.

Audit.EntityFramework provides the infrastructure to log interactions with the EF `DbContext`. It can record detailed information about CRUD operations in your database.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.EntityFramework
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.EntityFramework.svg?style=flat)](https://www.nuget.org/packages/Audit.EntityFramework/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.EntityFramework.svg)](https://www.nuget.org/packages/Audit.EntityFramework/)

## Note 

If you are targeting the full .NET framework but want to use EntityFrameworkCore (EF >= 7), you should install the `Audit.EntityFramework.Core` package instead:

```
PM> Install-Package Audit.EntityFramework.Core
```

## Usage
Change your EF Context class to inherit from `Audit.EntityFramework.AuditDbContext` instead of `DbContext`. 

For example, if you have a context like this:

```c#
public class MyEntities : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

to enable the audit log, you should change it to inherit from `AuditDbContext`:
```c#
public class MyEntities : Audit.EntityFramework.AuditDbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

> If you're using [IdentityDbContext](https://msdn.microsoft.com/en-us/library/microsoft.aspnet.identity.entityframework.identitydbcontext(v=vs.108).aspx) instead of DbContext, 
you should inherit from class `AuditIdentityDbContext`.

## How it works
The library intercepts calls to `SaveChanges` / `SaveChangesAsync` methods on the `DbContext` and generates detailed audit logs.

Each call to `SaveChanges` generates a new audit event that includes information of all the entities affected by the save operation.

## Configuration

### Settings
The following settings can be configured per DbContext or globally:

- **Mode**: To indicate the audit operation mode
  - _Opt-Out_: All the entities are tracked by default, except those explicitly ignored. (Default)
  - _Opt-In_: No entity is tracked by default, except those explicitly included.
- **IncludeEntityObjects**: To indicate if the output should contain the complete entity object graphs. (Default is false)
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the context name). Can contain the following placeholders: 
   - \{context}: replaced with the Db Context type name.
   - \{database}: replaced with the database name.


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

For example, to configure a context called `MyEntities`, that should include the objects on the output, using the OptOut mode, excluding from the audit the entities whose name ends with `History`:
```c#
Audit.EntityFramework.Configuration.Setup()
    .ForContext<MyEntities>(config => config
        .IncludeEntityObjects()
        .AuditEventType("{context}:{database}"))
    .UseOptOut()
        .IgnoreAny(t => t.Name.EndsWith("History"));
```

In summary, you have three ways to configure the audit for the contexts:
- By accessing the properties on the `AuditDbContext` base class.
- By decorating your context classes with `AuditDbContext` attribute and your entity classes with `AuditIgnore`/`AuditInclude` attributes.
- By using the fluent API provided by the method `Audit.EntityFramework.Configuration.Setup()`

All three can be used at the same time, and the precedence order is the order exposed in the above list.

### Event Output 

To configure the output persistence mechanism please see [Configuration](https://github.com/thepirat000/Audit.NET#configuration) and [Data Providers](https://github.com/thepirat000/Audit.NET#data-providers) sections.

### Overrides

The `AuditDbContext` has the following virtual methods that can be overriden to provide your custom logic:
- **OnScopeCreated**: Called before the EF operation execution and after the `AuditScope` creation.
- **OnScopeSaving**: Called after the EF operation execution and before the `AuditScope` saving.

This is useful to, for example, save the audit logs in the same transaction as the CRUD operation being audited, so when the audit logging fails the audited operation is rolled back.

```c#
public class MyDbContext : AuditDbContext
{
	public MyDbContext()
	{
		// Set an empty DynamicDataProvider to avoid saving on the data provider
		AuditDataProvider = new DynamicDataProvider();
	}
	
	protected override void OnScopeCreated(AuditScope auditScope)
	{
		Database.BeginTransaction();
	}

	protected override void OnScopeSaving(AuditScope auditScope)
	{
		try	
		{
			// ... custom log saving ...
		}
		catch
		{
			// Rollback call is not mandatory. If exception thrown, the transaction won't get commited
			Database.CurrentTransaction.Rollback(); 
			throw;
		}
		Database.CurrentTransaction.Commit();
	}
}
```

> Note that in the example above, since we want the event saving to be done on the `OnScopeSaving` method, we need to bypass the [Data Provider](https://github.com/thepirat000/Audit.NET#data-providers) and this can be done specifying an empty dynamic provider.

## Output

Audit.EntityFramework output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Affected SQL database and table names
- Affected column data including primary key, original and new values
- Model validation results
- Exception details
- Transaction identifier (to group logs that are part of the same SQL transaction)
- Entity object graphs (optional with `IncludeEntityObjects` configuration)
- Execution time and duration

With this information, you can measure performance, observe exceptions thrown or get statistics about usage of your database.

## Output details

The following tables describes the Audit.EntityFramework output fields:

- ### [EntityFrameworkEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EntityFrameworkEvent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Database** | string | Name of the database affected |
| **ConnectionId** | Guid | Unique client connection ID (only available when the connection is open at the beginning of the event) |
| **TransactionId** | string | Unique identifier for the DB transaction used on the audited operation (if any). To group events that are part of the same transaction. |
| **Entries** | Array of [EventEntry](#evententry) | Array with information about the entities affected by the audited operation |
| **Result** | integer | Result of the SaveChanges call. Is the number of objects affected by the operation. |
| **Success** | boolean | Boolean to indicate if the operation was successful |
| **ErrorMessage** | string | The exception thrown details (if any) |

- ### [EventEntry](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntry.cs)
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

- ### [ChangeObject](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntryChange.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **ColumnName** | string | The column name that was updated |
| **OriginalValue** | string | The original value before the update |
| **NewValue** | string | The new value after the update |


## Customization

### Custom fields

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

Another way to customize the output is by using global custom actions, please see [custom actions](https://github.com/thepirat000/Audit.NET#custom-actions) for more information.

## Entity Framework Data Provider

If you plan to store the audit logs in the same database as the audited entities, you can use the `EntityFrameworkDataProvider`. Use this if you plan to store the audit trails 
for each entity type in a table with similar structure.

For example, you want to audit the `Order` and `OrderItem` entity types into the `Audit_Order` and `Audit_OrderItem` trail tables respectively, 
and the structure of the `Audit_*` tables mimic the original table plus some fields like the event date, an action and the username.

[IMAGE]

Note the audit trail tables must be mapped on the same model as the audited entities.

### EF Provider configuration

To set the EntityFramework data provider globally, set the static `Audit.Core.Configuration.DataProvider` property to a new `EntityFrameworkDataProvider`:

```c#
Audit.Core.Configuration.DataProvider = new EntityFrameworkDataProvider()
{
    AuditTypeMapper = t => t == typeof(Order) ? typeof(Audit_Order) : t == typeof(OrderItem) ? typeof(Audit_OrderItem) : null,
    AuditEntityAction = (evt, entry, auditEntity) =>
    {
        var a = (dynamic)auditEntity;
        a.AuditDate = DateTime.UtcNow;
        a.UserName = evt.Environment.UserName;
        a.AuditAction = entry.Action; // Insert, Update, Delete
    }
};
```

Or use the [fluent API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api) `UseEntityFramework` method:
```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeExplicitMapper(m => m
            .Map<Order, Audit_Order>()
            .Map<OrderItem, Audit_OrderItem>())
        .AuditEntityAction<IAudit>((evt, entry, auditEntity) =>
        {
            auditEntity.AuditDate = DateTime.UtcNow;
            auditEntity.UserName = evt.Environment.UserName;
            auditEntity.AuditAction = entry.Action; // Insert, Update, Delete
        }));
```

### EF Provider Options

Mandatory:
- **AuditTypeMapper**: A function that maps an entity type to its audited type (i.e. Order -> Audit_Order, etc). 
- **AuditEntityAction**: An action to perform on the audit entity before saving it, for example to update specific audit properties.

Optional:
- **IgnoreMatchedProperties**: Set to true to avoid the property values copy from the entity to the audited entity (default is true).

### EF Provider configuration examples

The `UseEntityFramework` method provides several ways to indicate the Type Mapper and the Audit Action.

You can map the audited entity to its audit trail entity by the entity name using the `AuditTypeNameMapper` method, for example to prepend `Audit_` to the entity name. 
This assumes both entity types are defined on the same assembly and namespace:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeNameMapper(typeName => "Audit_" + typeName)
        .AuditEntityAction((ev, ent, auditEntity) =>
        {
        // auditEntity is object
	    ((dynamic)auditEntity).AuditDate = DateTime.UtcNow;
        }));
```

If your audit trail entities implements a common interface or base class, you can use the generic version of the `AuditEntityAction` method 
to configure the action to be performed to each audit trail entity before saving:
```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeNameMapper(typeName => "Audit_" + typeName)
        .AuditEntityAction<IAudit>((ev, ent, auditEntity) =>
        {
            // auditEntity is of IAudit type
            auditEntity.AuditDate = DateTime.UtcNow;
        }));
```

Use the explicit mapper to provide granular configuration per audit type:
```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeExplicitMapper(m => m
            .Map<Order, Audit_Order>((order, auditOrder) => 
            { 
                // This action is executed only for Audit_Order entities
                auditOrder.Status = "Order-" + order.Status; 
            })
            .Map<OrderItem, Audit_OrderItem>()
            .AuditEntityAction<IAudit>((ev, ent, auditEntity) =>
            {
                // This common action executes for every audited entity
                auditEntity.AuditDate = DateTime.UtcNow;
            })));
```