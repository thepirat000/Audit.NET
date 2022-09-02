# Audit.EntityFramework

**Entity Framework Audit Extension for [Audit.NET library](https://github.com/thepirat000/Audit.NET).**

Automatically generates Audit Logs for EntityFramework's operations. **Supporting EntityFramework and EntityFramework Core**

This library provides the infrastructure to log interactions with the EF `DbContext`. 
It can record detailed information about CRUD operations in your database.

## Install

**NuGet Package** 

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Audit.EntityFramework
```

Or, if you use EntityFramework core: 

```
PM> Install-Package Audit.EntityFramework.Core
```

Or, if you want to audit [ASP.NET Identity entities](https://msdn.microsoft.com/en-us/library/microsoft.aspnet.identity.entityframework.identitydbcontext(v=vs.108).aspx), you must also install the `Audit.EntityFramework.Identity` library:

```
PM> Install-Package Audit.EntityFramework.Identity
```

[![NuGet Status](https://img.shields.io/nuget/v/Audit.EntityFramework.svg?style=flat)](https://www.nuget.org/packages/Audit.EntityFramework/)
[![NuGet Count](https://img.shields.io/nuget/dt/Audit.EntityFramework.svg)](https://www.nuget.org/packages/Audit.EntityFramework/)

## EF library version 

The following table shows the entity framework package version used for each .NET framework and audit library:

| <sub>Target</sub> \ <sup>Library</sup> | `Audit.EntityFramework` / `Audit.EntityFramework.Identity` | `Audit.EntityFramework.Core` / `Audit.EntityFramework.Identity.Core` | `Audit.EntityFramework.Core.v3` / `Audit.EntityFramework.Identity.Core.v3` | 
| ------------ | ---------------- |  -------------- | -------------- |
| **.NET 4.5** | EntityFramework 6.1.3 | N/C | N/C |
| **.NET 4.6.1** | EntityFramework 6.1.3 | Microsoft.EntityFrameworkCore 2.2.4 | N/C |
| **.NET 4.7.2** | EntityFramework 6.3.0 | Microsoft.EntityFrameworkCore 3.1.0 | N/C |
| **.NET Standard 1.5** | Microsoft.EntityFrameworkCore 1.1.2 | Microsoft.EntityFrameworkCore 1.1.2 | N/C |
| **.NET Standard 2.0** | Microsoft.EntityFrameworkCore 2.2.4 | Microsoft.EntityFrameworkCore 3.1.0 | Microsoft.EntityFrameworkCore 3.1.0 |
| **.NET Standard 2.1** | EntityFramework 6.3.0 | Microsoft.EntityFrameworkCore 5.0.1 | Microsoft.EntityFrameworkCore 3.1.0 |
| **.NET 5.0** | EntityFramework 6.4.4 | Microsoft.EntityFrameworkCore 5.0.8 | Microsoft.EntityFrameworkCore 3.1.0 |
| **.NET 6.0** | EntityFramework 6.4.4 | Microsoft.EntityFrameworkCore 6.0.0 | N/C |

> N/C: Not Compatible

Examples:

- Your app targets the full .NET framework but want to use EntityFramework Core, you must install the `Audit.EntityFramework.Core` package.
- Your app targets .NET standard 2.1 and want to use EntityFramework 6, you must install `Audit.EntityFramework` package.
- Your app targets .NET 5.0 and want to use EntityFramework Core 3, you must install `Audit.EntityFramework.Core.v3` package.

## Usage

### High-Level SaveChanges Interception

In order to audit *Insert*, *Delete* and *Update* operations, you can use any of the three `SaveChanges` interception mechanisms provided:

#### 1. Inheriting from `AuditDbContext`

Change your EF context class to inherit from `Audit.EntityFramework.AuditDbContext` instead of `DbContext`. 

For example, if you have a context like this:

```c#
public class MyContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

In order to enable the audit log, you should change it to inherit from `AuditDbContext`:
```c#
public class MyContext : AuditDbContext // <-- inherit from Audit.EntityFramework.AuditDbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

> **Note**
> 
> If you're using [IdentityDbContext](https://msdn.microsoft.com/en-us/library/microsoft.aspnet.identity.entityframework.identitydbcontext(v=vs.108).aspx) instead of DbContext, 
you can install the package `Audit.EntityFramework.Identity` or `Audit.EntityFramework.Identity.Core` and inherit from the class `AuditIdentityDbContext` instead of `AuditDbContext`.

#### 2. Without inheritance, overriding `SaveChanges`

You can use the library without changing the inheritance of your `DbContext`.
In order to to that, you can define your `DbContext` in the following way, overriding `SaveChanges` and `SaveChangesAsync`:

```c#
public class MyContext : DbContext
{
    private readonly DbContextHelper _helper = new DbContextHelper();
    private readonly IAuditDbContext _auditContext;

    public MyContext(DbContextOptions<MyContext> options) : base(options)
    {
        _auditContext = new DefaultAuditContext(this);
        _helper.SetConfig(_auditContext);
    }

    public override int SaveChanges()
    {
        return _helper.SaveChanges(_auditContext, () => base.SaveChanges());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _helper.SaveChangesAsync(_auditContext, () => base.SaveChangesAsync(cancellationToken));
    }
}
```

#### 3. With the provided save changes interceptor

[Save Changes Interceptors](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.savechangesinterceptor?view=efcore-5.0) were introduced in EF Core 5.0. 

If you can't change the inheritance of your `DbContext`, and/or can't override the `SaveChanges`, you can attach an instance of `AuditSaveChangesInterceptor` to your DbContext configuration. 

For example:

```c#
public class MyContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor());
    }
    // ...
}
```

Or alternatively, when creating your DbContext:

```c#
var options = new DbContextOptionsBuilder()
    .AddInterceptors(new AuditSaveChangesInterceptor())
    .Options;
using (var ctx = new MyContext(options))
{
    // ...
}
```

> **Note**
> 
> Notice that a new instance of the interceptor is registered for each DbContext instance. This is because the auditing interceptor contains state linked to the current context instance.
 
#### Considerations

- All the Save Changes interception methods produces the same output. You should use **only one** of these methods, otherwise you could get duplicated audit logs.

### Low-Level Command Interception

A low-level command interceptor is also provided for EF Core ≥ 3.0. 

In order to audit low-level operations like *reads*, *stored procedure calls* and *non-query commands*, you can attach the provided `AuditCommandInterceptor` to 
your `DbContext` configuration. 

#### 1. On DbContext instantiation:

```c#
var options = new DbContextOptionsBuilder()
    .AddInterceptors(new AuditCommandInterceptor())
    .Options;
using (var ctx = new MyContext(options))
{
    // ...
}
```

#### 2. Or inside DbContext configuration:

```c#
public class MyDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditCommandInterceptor());
    }

    // ...
}
```

> **Note**
> 
> The **Command Interceptor** generates a different type of [audit output](#command-interceptor-audit-output) than the **Save Changes Interceptor**.
> Nevertheless, you can combine the Command Interceptor with any of the Save Changes interception mechanisms.



## Configuration 

### Output

The EF audit events are stored using a _Data Provider_. You can use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own. This can be set per `DbContext` instance or globally. If you plan to store the audit logs with EF, you can use the [Entity Framework Data Provider](#entity-framework-data-provider). 

### Settings (low-Level interceptor)
The low-level command interceptor can be configured by setting the `AuditCommandInterceptor` properties, for example:

```c#
optionsBuilder.AddInterceptors(new AuditCommandInterceptor()
{
    ExcludeNonQueryEvents = true,
    AuditEventType = "{database}",
    IncludeReaderResults = true
});
```

- **LogParameterValues**: Boolean value to indicate whether to log the command parameter values. By default (when null) it will depend on EnableSensitiveDataLogging setting on the DbContext.
- **ExcludeReaderEvents**: Boolean value to indicate whether to exclude the events handled by ReaderExecuting. Default is false to include the ReaderExecuting events.
- **ExcludeNonQueryEvents**: Boolean value to indicate whether to exclude the events handled by NonQueryExecuting. Default is false to include the NonQueryExecuting events.
- **ExcludeScalarEvents**: Boolean value to indicate whether to exclude the events handled by ScalarExecuting. Default is false to include the ScalarExecuting events.
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the execute method name). Can contain the following placeholders: 
    - \{database}: Replaced with the database name 
    - \{method}: Replaced with the execute method name (ExecuteReader, ExecuteNonQuery or ExecuteScalar) 
- **IncludeReaderResults**: Boolean value to indicate whether to include the query results to the audit output. Default is false.

### Settings (High-Level interceptor)
The following settings for the high-level interceptor can be configured per DbContext or globally:

- **Mode**: To indicate the audit operation mode
  - _Opt-Out_: All the entities are tracked by default, except those explicitly ignored. (Default)
  - _Opt-In_: No entity is tracked by default, except those explicitly included.
- **IncludeEntityObjects**: To indicate if the output should contain the complete entity object graphs. (Default is false)
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the context name). Can contain the following placeholders: 
   - \{context}: replaced with the Db Context type name.
   - \{database}: replaced with the database name.
- **IncludeIndependantAssociations**: Value to indicate if the Independant Associations should be included. Default is false. (Only for EF <= 6.2)
 > **Note**
 > 
 > Note: EF Core <= 3 does not support many-to-many relations without a join entity, and for EF Core 5 the many-to-many relations are normally included on the audit event entries. 
- **ExcludeTransactionId**: Value to indicate if the Transaction IDs should be excluded from the output and not be retrieved (default is false to include the Transaction IDs).
- **ExcludeValidationResults**: Value to indicate if the entity validations should be avoided and excluded from the audit output. (Default is false)
- **EarlySavingAudit**: Value to indicate if the audit event should be saved *before* the entity saving operation takes place. (Default is false to save the audit event after the entity saving operation completes or fails)


The following settings can be configured per entity type:

- **IgnoredProperties**: To indicate the entity's properties (columns) to be ignored on the audit logs.
- **OverrideProperties**: To override property values on the audit logs.
- **FormatProperties**: To indicate replacement functions for the property's values on the audit logs.

> The Ignore, Override and Format settings are only applied to the **Changes** and **ColumnValues** collections on the [EventEntry](#evententry). The `Entity` object (if included) will not be affected by these settings.

Change the settings for a DbContext by decorating it with the `AuditDbContext` attribute, for example:

```c#
[AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = false, AuditEventType = "{database}_{context}" )]
public class MyEntitites : Audit.EntityFramework.AuditDbContext
{
...
```

You can also use the [Fluent API](#fluent-api) to configure the high-level interceptor settings globally.


#### Include/Ignore entities (tables)

To ignore specific entities on the audit (when using OptOut Mode), you can decorate your entity classes with the `AuditIgnore` attribute, for example:
```c#
[AuditIgnore]
public class Blog
{
    public int Id { get; set; }
    ...
}
```

Instead, to include specific entities to the audit (when using OptIn Mode), you can use the `AuditInclude` attribute:
```c#
[AuditInclude]
public class Post
{
    public int Id { get; set; }
    ...
}
```

#### Exclude properties (columns)

The `AuditIgnore` attribute can be used on the entity's properties to indicate that its value should
*not* be included on the audit logs. For example to prevent storing passwords on the logs:

```c#
public class User
{
    public int Id { get; set; }
    [AuditIgnore]
    public string Password { get; set; }
    ...
}
```

#### Override properties (columns)

The `AuditOverride` attribute can be used to override a column value with a constant value.
For example to override the password values with a NULL value:

```c#
public class User
{
    [AuditOverride(null)]
    public string Password { get; set; }
    ...
}
```

Note you can also provide a replacement function of the value, please see next section.

### Fluent API

You can configure the settings via a convenient Fluent API provided by the method `Audit.EntityFramework.Configuration.Setup()`, this is the most straightforward way to configure the library.

For example, to configure a context called `MyContext`, that should include the objects on the output, using the OptOut mode, excluding from the audit the entities whose name ends with `History`:
```c#
Audit.EntityFramework.Configuration.Setup()
    .ForContext<MyContext>(config => config
        .IncludeEntityObjects()
        .AuditEventType("{context}:{database}"))
    .UseOptOut()
        .IgnoreAny(t => t.Name.EndsWith("History"));
```

Another example configuring ignored, overriden and formatted column values. In this example, the Photo column 
is ignored, the OldPassword will be always null and the Password will be set to a number of stars equal to the number 
of password characters.

```c#
Audit.EntityFramework.Configuration.Setup()
    .ForContext<MyContext>(config => config
        .ForEntity<User>(_ => _
            .Ignore(user => user.Photo)
            .Override(user => user.OldPassword, null)
            .Format(user => user.Password, pass => new String('*', pass.Length))));
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

This is useful to, for example, save the audit logs in the same transaction as the operation being audited, so when the audit logging fails the audited operation is rolled back.

```c#
public class MyDbContext : AuditDbContext
{
    public MyDbContext()
    {
        // Set a NULL data provider, since log saving is done in this class 
        AuditDataProvider = new NullDataProvider();
    }
    
    public override void OnScopeCreated(IAuditScope auditScope)
    {
        Database.BeginTransaction();
    }

    public override void OnScopeSaving(IAuditScope auditScope)
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

> **Note**
> 
> In this example we want the event saving to be done on the `OnScopeSaving` method, so we must bypass the [Data Provider](https://github.com/thepirat000/Audit.NET#data-providers) and this can be done by setting a `NullDataProvider`.

## Output

Audit.EntityFramework output includes:

- Execution time and duration
- Environment information such as user, machine, domain and locale.
- Affected SQL database and table names
- Affected column data including primary key, original and new values
- Model validation results
- Exception details
- Transaction identifiers (to group logs that are part of the same SQL or ambient transaction)
- Entity object graphs (optional with `IncludeEntityObjects` configuration)

With this information, you can measure performance, observe exceptions thrown or get statistics about usage of your database.

## Output details

### SaveChanges audit output

The following tables describes the output fields for the SaveChanges interception:

- #### [EntityFrameworkEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EntityFrameworkEvent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Database** | string | Name of the database affected |
| **ConnectionId** | Guid | A unique identifier for the database connection. |
| **ContextId** | string | A unique identifier for the context instance and pool lease. |
| **TransactionId** | string | Unique identifier for the DB transaction used on the audited operation (if any). To group events that are part of the same transaction. |
| **AmbientTransactionId** | string | Unique identifier for the ambient transaction used on the audited operation (if any). To group events that are part of the same ambient transaction. |
| **Entries** | Array of [EventEntry](#evententry) | Array with information about the entities affected by the audited operation |
| **Associations** | Array of [AssociationEntry](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/AssociationEntry.cs) | Independant associations changes, many-to-many relations without a join table with changes (only for EF <=6.2, not available on EF Core) |
| **Result** | integer | Result of the SaveChanges call. Is the number of objects affected by the operation. |
| **Success** | boolean | Boolean to indicate if the operation was successful |
| **ErrorMessage** | string | The exception thrown details (if any) |

- #### [EventEntry](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntry.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Table** | string | Name of the affected table |
| **Name** | string | The entity friendly name (only for EF Core ≥ 3) |
| **Action** | string | Action type (Insert, Update or Delete) |
| **PrimaryKey** | Object | Object with the affected entity's primary key value(s) |
| **ColumnValues** | Object | Object with the affected entity's column values  |
| **Changes** | Array of [ChangeObject](#changeobject) | An array containing the modified columns with the original and new values (only available for Update operations) |
| **Entity** | Object | The object representation of the .NET entity affected (optional) |
| **Valid** | boolean | Bolean indicating if the entity passes the validations |
| **ValidationResults** | Array of string | The validation messages when the entity validation fails |

- #### [ChangeObject](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/EventEntryChange.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **ColumnName** | string | The column name that was updated |
| **OriginalValue** | string | The original value before the update |
| **NewValue** | string | The new value after the update |

### Command Interceptor audit output

The following table describes the output fields for the low-level command interception:

- #### [CommandEvent](https://github.com/thepirat000/Audit.NET/blob/master/src/Audit.EntityFramework/CommandEvent.cs)
| Field Name | Type | Description | 
| ------------ | ---------------- |  -------------- |
| **Database** | string | Name of the database affected |
| **ConnectionId** | Guid | A unique identifier for the database connection. |
| **ContextId** | string | A unique identifier for the context instance and pool lease. |
| **Method** | string | The command method executed (NonQuery, Scalar, Reader) |
| **CommandType** | string | The command type (Text, StoredProcedure) |
| **CommandText** | string | The command text |
| **Parameters** | Dictionary | The parameter values, if any, when `EnableSensitiveDataLogging` is enabled |
| **IsAsync** | boolean | Indicates whether the call was asynchronous |
| **Result** | object | Result of the operation, only for Scalar and NonQuery methods. Reader methods does not include the result on the output. |
| **Success** | boolean | Boolean to indicate if the operation was successful |
| **ErrorMessage** | string | The exception thrown details (if any) |

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

# Entity Framework Data Provider

If you plan to store the audit logs via EntityFramework, you can use the provided `EntityFrameworkDataProvider`. 
Use this to store the logs on audit tables handled by EntityFramework.

> **Note**
> 
> Only the high-level audit events are processed by this data provider. Any other audit event, including the low-level events generated by the command interceptor,
> are ignored by the entity framework data provider. 

For example, you want to audit `Order` and `OrderItem` tables into `Audit_Order` and `Audit_OrderItem` tables respectively, 
and the structure of the `Audit_*` tables mimic the audited table plus some fields like the event date, an action and the username:

![Audit entities](http://i.imgur.com/QvfXS9H.png)

> **Note**
> 
> By default, the library uses the same `DbContext` instance audited to store the audit logs. 
> This is not mandatory and the recommendation is to provide a different _DbContext_ instance per audit event by using the `UseDbcontext()` 
> fluent API.

## EF Provider configuration

To set the EntityFramework data provider globally, set the static `Audit.Core.Configuration.DataProvider` property to a new `EntityFrameworkDataProvider`:

```c#
Audit.Core.Configuration.DataProvider = new EntityFrameworkDataProvider()
{
    DbContextBuilder = ev => new OrderDbContext(),
    AuditTypeMapper = (t, ee) => t == typeof(Order) ? typeof(OrderAudit) : t == typeof(Orderline) ? typeof(OrderlineAudit) : null,
    AuditEntityAction = (evt, entry, auditEntity) =>
    {
        var a = (dynamic)auditEntity;
        a.AuditDate = DateTime.UtcNow;
        a.UserName = evt.Environment.UserName;
        a.AuditAction = entry.Action; // Insert, Update, Delete
        return Task.FromResult(true); // return false to ignore the audit
    }
};
```

Or use the [fluent API](https://github.com/thepirat000/Audit.NET#configuration-fluent-api) `UseEntityFramework` method, this is the recommended approach:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .UseDbContext<OrderDbContext>()
        .AuditTypeExplicitMapper(m => m
            .Map<Order, OrderAudit>()
            .Map<Orderline, OrderlineAudit>()
            .AuditEntityAction<IAudit>((evt, entry, auditEntity) =>
            {
                auditEntity.AuditDate = DateTime.UtcNow;
                auditEntity.UserName = evt.Environment.UserName;
                auditEntity.AuditAction = entry.Action; // Insert, Update, Delete
            })
        )
    );
```

## EF Provider Options

Mandatory:
- **UseDbContext**: A function that returns the DbContext to use for storing the audit events, by default it will use the same context being audited. 
- **AuditTypeMapper**: A function that maps an entity type to its audited type (i.e. Order -> Audit_Order, etc). 
- **ExplicitMapper**: An alternative mapper, as a function that excplicitly maps an entry to its audited type, useful to configure mapping when no entity type is associated with a table, or to setup complex mapping rules.
- **AuditEntityCreator**: An alternative to the mapper, as a function that creates the audit entity instance from the Event Entry and the Audit DbContext. Useful to control the Audit Entity object creation for example when using change-tracking proxies. 
- **AuditEntityAction**: An action to perform on the audit entity before saving it, for example to update specific audit properties like user name or the audit date. It can also be used to filter out audit entities. Make this function return a boolean value to indicate whether to include the entity on the output log. 
- **IgnoreMatchedProperties**: Set to true to avoid the property values copy from the entity to the audited entity (default is false).
- **IgnoreMatchedPropertiesFunc**: Allows to selectively ignore property matching on certain types. It's a function that receives the audit entity type and returns a boolean to indicate if the property matching must be ignored for that type.

## EF Provider configuration examples

The `UseEntityFramework` method provides several ways to indicate the Type Mapper and the Audit Action.

### Map by type name:

You can map the audited entity to its audit log entity by the entity name using the `AuditTypeNameMapper` method, for example to prepend `Audit_` to the entity name. 
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

the AuditEvent (shown here as `ev`) in an instance of `AuditEventEntityFramework`. As such, it can be casted to that type or by using the helper method `ev.GetEntityFrameworkEvent()`.

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeNameMapper(typeName => "Audit_" + typeName)
        .AuditEntityAction<IAudit>((ev, ent, auditEntity) =>
        {
	    var entityFrameworkEvent = ev.GetEntityFrameworkEvent();
	    auditEntity.TransactionId = entityFrameworkEvent.TransactionId;
        }));
```

### Common action:

If your audit log entities implements a common interface or base class, you can use the generic version of the `AuditEntityAction` method 
to configure the action to be performed to each audit trail entity before saving. Also this action can be asynchronous, for example:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeNameMapper(typeName => "Audit_" + typeName)
        .AuditEntityAction<IAudit>(async (ev, ent, auditEntity) =>
        {
            // auditEntity is of IAudit type
            auditEntity.AuditDate = DateTime.UtcNow;
            auditEntity.SomeValue = await GetValueAsync();
        }));
```

### Use the explicit mapper to provide granular configuration per audit type:

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

### Ignore certain entities on the audit log:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .AuditTypeExplicitMapper(m => m
            .Map<Order, Audit_Order>((order, auditOrder) =>
            {
                if (auditOrder.Status == "Expired")
                {
                    return false; // don't want to audit orders in "expired" status
                }
                auditOrder.AuditDate = DateTime.UtcNow;
                return true;
            })));
```

### Custom DbContext instance:

To set a custom DbContext instance for storing the audit events, for example when your Audit_* entities 
are defined in a different database and context (i.e. `AuditDatabaseDbContext`):

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .UseDbContext<AuditDatabaseDbContext>()
        .AuditTypeExplicitMapper(m => m
            .Map<Order, Audit_Order>()
            .AuditEntityAction<IAudit>((ev, ent, auditEntity) =>
            {
                auditEntity.AuditDate = DateTime.UtcNow;
            })));
```

### Map multiple entity types to the same audit type with independent actions:

When you want to store the audit logs of different entities in the same audit table, for example:

![Audit entities 2](https://i.imgur.com/oNxnhUh.png)

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .AuditTypeExplicitMapper(m => m
            .Map<Blog, AuditLog>((blog, audit) =>
            {
                // Action for Blog -> AuditLog
                audit.TableName = "Blog";
                audit.TablePK = blog.Id;
                audit.Title = blog.Title;
            })
            .Map<Post, AuditLog>((post, audit) =>
            {
                // Action for Post -> AuditLog
                audit.TableName = "Post";
                audit.TablePK = post.Id;
                audit.Title = post.Title;
            })
            .AuditEntityAction<AuditLog>((evt, entry, audit) =>
            {
                // Common action on AuditLog
                audit.AuditDate = DateTime.UtcNow;
                audit.AuditAction = entry.Action;
                audit.AuditUsername = Environment.UserName;
            }))
	.IgnoreMatchedProperties(true));
```

Another example for all entities mapping to a single audit log table that stores the changes in a JSON column:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(_ => _
        .AuditTypeMapper(t => typeof(AuditLog))  
        .AuditEntityAction<AuditLog>((ev, entry, entity) =>
        {
            entity.AuditData = entry.ToJson();
            entity.EntityType = entry.EntityType.Name;
            entity.AuditDate = DateTime.Now;
            entity.AuditUser = Environment.UserName;
	    entity.TablePk = entry.PrimaryKey.First().Value.ToString();
        })
	.IgnoreMatchedProperties(true));
```

> **Note**
> 
> Notice the use of `.IgnoreMatchedProperties(true)` to avoid the library trying to set properties automatically by matching names between the audited entities and the type `AuditLog`.

### Map an entity type to multiple audit types, depending on the modified entry:

When you want to save audit logs to different tables for the same entity, for example, 
if you have different audit tables per operation:

```c#
Audit.Core.Configuration.Setup() 
    .UseEntityFramework(ef => ef.AuditTypeExplicitMapper(m => m
        .Map<Blog>(
            mapper: entry => entry.Action == "Update" ? typeof(Audit_Updates_Blog) : typeof(Audit_Blog), 
            entityAction: (ev, entry, entity) =>
            {
                if (entity is Audit_Updates_Blog upd)
                {
                    // action for updates
                }
                else if (entity is Audit_Blog etc)
                {
                    // action for insert/delete
                }
            })
        .AuditEntityAction<IAuditLog>((evt, entry, auditEntity) =>
        {
            // common action...
        })));
```

> - Updates to `Blog` table -> Audit to `Audit_Updates_Blog` table
> - Any other operation on `Blog` table -> Audit to `Audit_Blog` table


### Map Many to Many relations without join entity:

When you want to audit many to many relations which are not mapped to an entity type, i.e. implicitly created join tables.
You have to use the `AuditTypeExplicitMapper` and set up the mapping of the relation table by using `MapTable` or `MapExplicit` methods.

For example, consider the following model:

![Audit entities Many To Many](https://i.imgur.com/HHRs4aw.png)

There are two entities, `Post` and `Tag` with a Many to Many relation between them (note there is no relation entity). 
Also you want to audit the `Post` and `Tag` tables to the `Audit_Post` and `Audit_Tag` tables respectively, and
you want to audit the `PostTag` relation table to an `Audit_PostTag` table.

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(_ => _
        .UseDbContext<YourAuditDbContext>()
        .AuditTypeExplicitMapper(map => map
            .Map<Post, Audit_Post>()
            .Map<Tag, Audit_Tag>()
            .MapTable<Audit_PostTag>("PostTag", (EventEntry ent, Audit_PostTag auditPostTag) =>
            {
                auditPostTag.PostId = ent.ColumnValues["PostsId"];
                auditPostTag.TagId = ent.ColumnValues["TagsId"];
            })
            .AuditEntityAction((ev, entry, auditEntity) =>
            {
                ((dynamic)auditEntity).AuditAction = entry.Action;
                ((dynamic)auditEntity).AuditDate = DateTime.UtcNow;
            })));
```

> The first parameter of `MapTable` is the table name to which the mapping will apply. 
> The generic parameter is the target audit type.
> You can optionally pass an action to execute on the audit entity as the second parameter. 
> If property matching is enabled for the target type, the framework will map the Column values to the entity Property values.

### Map via Factory: 

When you need to control the Audit Entity creation, for example when using [change-tracking proxies](https://docs.microsoft.com/en-us/ef/core/change-tracking/change-detection#change-tracking-proxies),
you can use the `AuditEntityCreator` to specify a factory that creates the Audit Entity for a given entry.

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .UseDbContext<YourAuditDbContext>()
        .AuditEntityCreator(auditDbContext => auditDbContext.CreateProxy<AuditLog>())
        .AuditEntityAction<AuditLog>((ev, ent, auditEntity) =>
        {
            auditEntity.DateTime = DateTime.Now;
            auditEntity.Action = ent.Action;
            auditEntity.Table = ent.Table;
        })
        .IgnoreMatchedProperties());
```

Another example of an andit Entity factory, but mapping to different entity types depending on the audited table:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .UseDbContext<YourAuditDbContext>()
        .AuditEntityCreator((auditDbContext, entry) => entry.Table switch
        {
            "Customer" => auditDbContext.CreateProxy<AuditCustomer>(),
            "User" => auditDbContext.CreateProxy<AuditUser>(),
            _ => auditDbContext.CreateProxy<AuditLog>()
        })
        .AuditEntityAction<IAuditLog>((ev, ent, auditEntity) =>
        {
            auditEntity.DateTime = DateTime.Now;
            auditEntity.Action = ent.Action;
            auditEntity.Table = ent.Table;
        })
        .IgnoreMatchedProperties());

);
```

# Contribute

If you like this project please contribute in any of the following ways:

- [Star](https://github.com/thepirat000/Audit.NET/stargazers) this project on GitHub.
- Request a new feature or expose any bug you found by creating a [new issue](https://github.com/thepirat000/Audit.NET/issues/new).
- Ask any questions about the library on [StackOverflow](http://stackoverflow.com/questions/ask?tags=Audit.NET).
- Subscribe to and use the [Gitter Audit.NET channel](https://gitter.im/Audit-NET/Lobby).
- Support the project by [becoming a Backer](https://opencollective.com/auditnet):
[![Backer](https://opencollective.com/auditnet/tiers/backer.svg?avatarHeight=36&width=600)](https://opencollective.com/auditnet)     
- Spread the word by blogging about it, or sharing it on social networks:
  <p class="share-buttons">
    <a href="https://www.facebook.com/sharer/sharer.php?u=https://nuget.org/packages/Audit.NET/&amp;t=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Share this package on Facebook" src="https://nuget.org/Content/gallery/img/facebook.svg" / >
    </a>
    <a href="https://twitter.com/intent/tweet?url=https://nuget.org/packages/Audit.NET/&amp;text=Check+out+Audit.NET" target="_blank">
      <img width="24" height="24" alt="Tweet this package" src="https://nuget.org/Content/gallery/img/twitter.svg" />
    </a>
  </p>
- Make a donation via PayPal 
[![paypal](https://img.shields.io/badge/donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=thepirat000%40hotmail.com&currency_code=USD&source=url)
