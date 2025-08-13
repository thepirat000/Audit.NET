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

## ZZZ Projects - Sponsorship

[Entity Framework Extensions](https://entityframework-extensions.net/) and [Dapper Plus](https://dapper-plus.net/) are major sponsors and are proud to contribute to the development of Audit.NET

Combine the power of auditing with the speed of Bulk Operations to get the best of both worlds — audit and performance.

[![Entity Framework Extensions - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert)

[![Dapper Plus - Sponsor](https://raw.githubusercontent.com/thepirat000/Audit.NET/master/documents/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert)

## EF library version 

The following table shows the entity framework package version used for each .NET framework and audit library:

<sub>Target</sub> \ <sup>Library</sup> | `Audit.EntityFramework` / `Audit.EntityFramework.Identity` | `Audit.EntityFramework.Core` / `Audit.EntityFramework.Identity.Core` |  
------------ | ---------------- |  -------------- |
**.NET 4.6.2** | EntityFramework 6.5.0 | N/C | 
**.NET 4.7.2** | EntityFramework 6.5.0 | N/C | 
**.NET Standard 2.1** | EntityFramework 6.5.0 | Microsoft.EntityFrameworkCore 5.0.17 | 
**.NET 6.0** | EntityFramework 6.5.0 | Microsoft.EntityFrameworkCore 6.0.36 | 
**.NET 7.0** | EntityFramework 6.5.0 | Microsoft.EntityFrameworkCore 7.0.20 | 
**.NET 8.0** | EntityFramework 6.5.0 | Microsoft.EntityFrameworkCore 8.0.11 | 
**.NET 9.0** | EntityFramework 6.5.0 | Microsoft.EntityFrameworkCore 9.0.0 | 

> N/C: Not Compatible

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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        return _helper.SaveChanges(_auditContext, () => base.SaveChanges(acceptAllChangesOnSuccess));
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _helper.SaveChangesAsync(_auditContext, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), cancellationToken);
    }
}
```

> **Note**
> 
> No other `SaveChanges` override is needed, since all the other overloads will call one of these two.

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

Or using DI, such as with ASP.NET Core:

```c#
builder.Services.AddDbContext<MyContext>(c => c
    .UseSqlServer(CONNECTION_STRING)
    .AddInterceptors(new AuditSaveChangesInterceptor())
```

> **Note**
> 
> Notice that a new instance of the interceptor is registered for each DbContext instance. This is because the auditing interceptor contains state linked to the current context instance.
 
#### Considerations

- All the Save Changes interception methods produces the same output. You should use **only one** of these methods, otherwise you could get duplicated audit logs.

### Low-Level Command Interception

A low-level command interceptor is also provided for Entity Framework Core. 

In order to audit low-level operations like *reads*, *stored procedure calls* and *non-query commands*, you can attach the provided `AuditCommandInterceptor` to 
your `DbContext` configuration. 

For example:

```c#
var options = new DbContextOptionsBuilder()
    .AddInterceptors(new AuditCommandInterceptor())
    .Options;
using (var ctx = new MyContext(options))
{
    // ...
}
```

Or inside DbContext configuration:

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

Or using DI, such as with ASP.NET Core:

```c#
builder.Services.AddDbContext<MyContext>(c => c
    .UseSqlServer(CONNECTION_STRING)
    .AddInterceptors(new AuditCommandInterceptor())
```

> **Note**
> 
> The **Command Interceptor** generates a different type of [audit output](#command-interceptor-audit-output) than the **Save Changes Interceptor**.
> Nevertheless, you can combine the Command Interceptor with any of the Save Changes interception mechanisms.

## Configuration 

### Output

EF audit events are stored via a _Data Provider_. You can either use one of the [available data providers](https://github.com/thepirat000/Audit.NET#data-providers-included) or implement your own.

The Audit Data Provider can be configured in several ways:

- Per `DbContext` instance by explicitly setting the `AuditDataProvider` property.
  For example:
  ```c#
  public class MyContext : AuditDbContext
  {
    public MyContext()
    {
        AuditDataProvider = new SqlDataProvider(config => config...);
    }
  }
  ```

- By registering an `AuditDataProvider` instance in the dependency injection container.

  For example:
  ```c#
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddSingleton<AuditDataProvider>(new SqlDataProvider(config => config...));
    }
  }
  ```

- Globally, by setting the `AuditDataProvider` instance through the `Audit.Core.Configuration.DataProvider` static property or the `Audit.Core.Configuration.Use()` methods.

  For example:
  ```c#
  public class Program
  {
    public static void Main(string[] args)
    {
      Audit.Core.Configuration.Setup().UseSqlServer(config => config...);
    }
  }
  ```

If you intend to store audit logs with EF, consider using the [Entity Framework Data Provider](#entity-framework-data-provider).

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
- **IncludeReaderEventsPredicate**: Predicate to include the ReaderExecuting events based on the event data. By default, all the ReaderExecuting events are included. This predicate is ignored if ExcludeReaderEvents is set to true.
- **ExcludeNonQueryEvents**: Boolean value to indicate whether to exclude the events handled by NonQueryExecuting. Default is false to include the NonQueryExecuting events.
- **ExcludeScalarEvents**: Boolean value to indicate whether to exclude the events handled by ScalarExecuting. Default is false to include the ScalarExecuting events.
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the execute method name). Can contain the following placeholders: 
    - \{context}: replaced with the Db Context type name.
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
- **ReloadDatabaseValues**: Value to indicate if the original values of the audited entities should be queried from database before saving the audit event.
> The *ReloadDatabaseValues* configuration is beneficial for making modifications without explicitly retrieving the entity first. 
It can be enabled when using DbSet.Update or DbSet.Remove with an object that wasn't retrieved from the database. When enabled, it queries the database prior to any entity modification to record the original values in the audit event.

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

You can also use the `AuditInclude` attribute on the properties (when using OptIn Mode):
```c#
[AuditInclude]
public class Post
{
    public int Id { get; set; }
    [AuditInclude]
    public string Name { get; set; }
    ...
}
```

> **Note**
> 
> When using opt-in mode, if an entity is included and none of its properties have the `[AuditInclude]` attribute, then all its properties will be included in the audit output by default. 
> Otherwise, if an entity is included and at least one of its properties has the `[AuditInclude]` attribute, then only those properties will be included in the audit output.

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
- **OnScopeSaved**: Called after the `AuditScope` saving.

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
| **Valid** | boolean | Boolean indicating if the entity passes the validations |
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
| **CommandType** | [CommandType](https://learn.microsoft.com/en-us/dotnet/api/system.data.commandtype) | The command type (Text, StoredProcedure, etc) |
| **CommandSource** | [CommandSource](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.diagnostics.commandsource) | The command source type (SaveChanges, LinqQuery, etc) |
| **CommandText** | string | The command text |
| **Parameters** | Dictionary | The parameter values, if any, when `EnableSensitiveDataLogging` is enabled |
| **IsAsync** | boolean | Indicates whether the call was asynchronous |
| **Result** | object | Result of the operation. Query results are only included when IncludeReaderResults is set to true. |
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

### Getting the entity framework event

The `AuditDbContext` provides an alternative Save Changes operation (`SaveChangesGetAudit()` method) to save the changes and get the generated `EntityFrameworkEvent` object. 
This is useful when you want to get the audit event information generated by a particular Save Changes operation.

For example:

```c#
// Save the changes and get the generated audit event
var efEvent = await _dbContext.SaveChangesGetAuditAsync();
	
// Log all the operations to the tables affected
foreach(var entry in efEvent.Entries)
{
	Console.WriteLine($"{entry.Action} {entry.Table}");
}
```

---

# **Entity Framework Data Providers**

## **Summary Table**
| **Provider**                     | **Purpose**           | **Key Features**          |
|----------------------------------|-----------------------|---------------------------|
| `EntityFrameworkDataProvider` | Stores high-level audit logs in dedicated audit tables handled by EF. | - Default: Uses same `DbContext` as audited operations. <br> - Configurable via fluent API or static setup. |
| `DbContextDataProvider<T, TEntity>` | Generic provider for mapping any audit event to a specific EF Core entity. | - Flexible mapping for any audit event to a single entity. <br> - Mapper function for customizing event-to-entity logic. <br> - Supports event replacement (updates).|
| `DbContextDataProvider`  | Non-generic provider for storing audit events across multiple entities or tables dynamically. | - Designed for complex scenarios requiring diverse mapping. <br> - Works with multiple unrelated tables/entities. <br> - Does not support event replacement (updates). |

## **Entity Framework Data Provider**

The `EntityFrameworkDataProvider` is ideal when you want to store audit logs in dedicated audit tables managed by Entity Framework. 
It captures high-level audit events, such as those generated during `SaveChanges` operations, while ignoring low-level events.

> **Key Notes**  
> - This provider processes only high-level audit events. Low-level events generated by EF Core’s command interceptors are ignored.  
> - Consider using the [Generic DbContext Provider](#generic-dbcontext-data-provider) for storing any type of audit events using an EF Core DbContext.

For example, you want to audit `Order` and `OrderItem` tables into `Audit_Order` and `Audit_OrderItem` tables respectively, 
and the structure of the `Audit_*` tables mimic the audited table plus some fields like the event date, an action and the username:

![Audit entities](http://i.imgur.com/QvfXS9H.png)

> **Note**
> By default, the library uses the same `DbContext` instance audited to store the audit logs. 
> This is not mandatory and the recommendation is to provide a different _DbContext_ instance per audit event by using the `UseDbcontext()` 
> fluent API.

> **Best Practice**  
> Use a separate `DbContext` instance for audit logging by utilizing the `UseDbContext` fluent API.

### **Entity Framework Data Provider - Configuration**

#### **Global Configuration**
Set the `Audit.Core.Configuration.DataProvider` property to an `EntityFrameworkDataProvider`:

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

#### **Fluent API (Recommended)**
Use the fluent API for better readability and customization:

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

### **Entity Framework Data Provider - Options**

| **Option**                  | **Description**                | **Default**      |
|-----------------------------|--------------------------------|------------------|
| **UseDbContext**            | Specifies the `DbContext` for storing audit events. | Uses the same context as audited operations. |
| **DisposeDbContext**        | Indicates whether to dispose of the audit `DbContext` after use. | `false` |
| **AuditTypeMapper**         | Maps entity types to their corresponding audit entity types (e.g., `Order` -> `Audit_Order`). | Required when entities differ. |
| **ExplicitMapper**          | An alternative mapper function for explicit entry-to-audited type mapping, ideal for tables without entity types or complex mapping rules. | Optional. |
| **AuditEntityCreator**      | Specifies a factory method for creating audit entities, useful for handling proxies or dynamic scenarios. | Optional. |
| **AuditEntityAction**       | Allows custom logic (e.g., setting properties like username or filtering specific entries). | Optional. |
| **IgnoreMatchedProperties** | Disables automatic property matching between audited and original entities. | `false`                                                                    |

### **Entity Framework Data Provider - Examples**

The `UseEntityFramework` method provides several ways to indicate the Type Mapper and the Audit Action.

#### Map by type name:

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

#### Common action:

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

#### Use the explicit mapper to provide granular configuration per audit type:

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

#### Ignore certain entities on the audit log:

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

#### Custom DbContext instance:

To set a custom DbContext instance for storing the audit events, for example when your Audit_* entities 
are defined in a different database and context (i.e. `AuditDatabaseDbContext`):

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(x => x
        .UseDbContext<AuditDatabaseDbContext>()
        .DisposeDbContext()
        .AuditTypeExplicitMapper(m => m
            .Map<Order, Audit_Order>()
            .AuditEntityAction<IAudit>((ev, ent, auditEntity) =>
            {
                auditEntity.AuditDate = DateTime.UtcNow;
            })));
```

#### Map multiple entity types to the same audit type with independent actions:

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

#### Map an entity type to multiple audit types, depending on the modified entry:

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

#### Map Many to Many relations without join entity:

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
        .DisposeDbContext()
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

#### Map via Factory: 

When you need to control the Audit Entity creation, for example when using [change-tracking proxies](https://docs.microsoft.com/en-us/ef/core/change-tracking/change-detection#change-tracking-proxies),
you can use the `AuditEntityCreator` to specify a factory that creates the Audit Entity for a given entry. 

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .UseDbContext<YourAuditDbContext>()
        .DisposeDbContext()
        .AuditEntityCreator(auditDbContext => auditDbContext.CreateProxy<AuditLog>())
        .AuditEntityAction<AuditLog>((ev, ent, auditEntity) =>
        {
            auditEntity.DateTime = DateTime.Now;
            auditEntity.Action = ent.Action;
            auditEntity.Table = ent.Table;
        })
        .IgnoreMatchedProperties());
```

Another example of an audit Entity factory, but mapping to different entity types depending on the audited table:

```c#
Audit.Core.Configuration.Setup()
    .UseEntityFramework(ef => ef
        .UseDbContext<YourAuditDbContext>()
        .DisposeDbContext()
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

## **Generic DbContext Data Provider**

The `DbContextDataProvider<TDbContext, TEntity>` enables flexible and customizable mapping of any audit event to specific entities within an EF Core `DbContext`.

> **Key Difference**  
> Unlike the `EntityFrameworkDataProvider`, this provider is not limited to high-level audit events, allowing you to store any type of audit data.

### **Generic DbContext Data Provider - Options**

| **Property**           | **Description**                                     |
|-------------------------|----------------------------------------------------|
| **DbContextBuilder / UseDbContext()** | A function that returns the `DbContext` to store the audit events. If unspecified, the parameterless constructor is used to create the `DbContext` instance. |
| **DbContextOptions / UseDbContextOptions()** | The `DbContextOptions` used to create the `DbContext` instance, serving as an alternative to the `DbContextBuilder`. |
| **Mapper**              | A function mapping the audit event to the entity. It receives the audit event and the audit entity instance (existing or new) and hydrates the instance. |
| **DisposeDbContext**    | A boolean indicating if the audit `DbContext` should be disposed of after saving the audit. Default is `false`. |

### **Generic DbContext Data Provider - Example**

The following example shows how to configure the `DbContextDataProvider` to store the audit events in a custom DbContext:

```c#
Audit.Core.Configuration.Setup()
    .UseDbContext<MyDbContext, AuditLog>(config => config
        .DbContextBuilder(_ => new MyDbContext())
        .Mapper((auditEvent, auditEntity) =>
        {
            auditEntity.AuditDate = auditEvent.StartDate;
            auditEntity.UserName = auditEvent.Environment.UserName;
            auditEntity.JsonData = auditEvent.ToJson();
        }));


public class MyDbContext : DbContext
{
    public DbSet<AuditLog> AuditLogs { get; set; }
}
```

## **Non-Generic DbContext Data Provider**

The `DbContextDataProvider` class uses Entity Framework Core to manage the writing of audit events. 
It enables the mapping of each audit event to a specific entity (or multiple entities) within a `DbContext`.

> **Note**  
> Unlike the generic version, `DbContextDataProvider<TDbContext, TEntity>`, the non-generic `DbContextDataProvider` 
is suited for scenarios where audit events are distributed across various entities or tables, or when a single audit event spans multiple unrelated tables. This behavior makes the non-generic version ideal for cases requiring more dynamic or diverse mappings.

### **Generic DbContext Data Provider - Options**

| **Property**                 | **Description**   |
|------------------------------|-------------------|
| **DbContextBuilder / UseDbContext()** | A function that returns the `DbContext` for storing audit events. If not specified, the parameterless constructor is used to create the `DbContext` instance. |
| **DbContextOptions / UseDbContextOptions()** | The `DbContextOptions` used to create the `DbContext` instance, as an alternative to the `DbContextBuilder`. |
| **EntityBuilder**                  | A function that maps an `AuditEvent` to the entity/entities for insertion into the database. It takes the audit event as input and returns the corresponding entity/entities. Returns NULL to ignore the event. |
| **DisposeDbContext**               | A boolean indicating whether the audit `DbContext` should be disposed after saving the audit. Default is `false`. |

### **Generic DbContext Data Provider - Examples**

#### Basic configuration

The following example shows how to configure the `DbContextDataProvider` to store the audit events in a custom DbContext.

In this case, `MyDbContext` is a custom DbContext that contains the `AuditLog` entity.  

```c#
Audit.Core.Configuration.Setup()
    .UseDbContext(config => config
        .DbContextBuilder(_ => new MyDbContext())
        .EntityBuilder(auditEvent => new AuditLog
        {
            JsonData = auditEvent.ToJson(),
            CreatedDate = DateTime.Now
        }));
```

#### Different audit entities

The following example shows how to configure the `DbContextDataProvider` to store the audit events in a custom DbContext.

In this case, `MyDbContext` is a custom DbContext that contains the `AuditLog` and `AuditLogSpecial` entities. 
Depending on the `EventType` property of the audit event, the `EntityBuilder` function will return the corresponding entity to insert:


```c#
Audit.Core.Configuration.Setup()
    .UseDbContext(config => config
        .UseDbContextOptions(new DbContextOptionsBuilder<MyDbContext>().UseSqlServer("Connection string...").Options))
        .EntityBuilder(auditEvent =>
        {
            if (auditEvent.EventType == "A")
            {
                return new AuditLogSpecial { JsonData = auditEvent.ToJson(), CreatedDate = DateTime.Now };
            }
            
            return new AuditLog { JsonData = auditEvent.ToJson(), CreatedDate = DateTime.Now };
        }));
```

#### Multiple audit entities

The following example shows how to configure the `DbContextDataProvider` to store the audit events as multiple entities in a custom DbContext.

In this case, the entity builder function return a list of unrelated entities to insert:


```c#
Audit.Core.Configuration.Setup()
    .UseDbContext(config => config
        .DbContextBuilder(_ => new MyDbContext())
        .EntityBuilder(auditEvent =>
        {
            var entities = new List<AuditLog>();

            foreach (var entry in auditEvent.GetEntityFrameworkEvent().Entries)
            {
                entities.Add(new AuditLog { JsonData = auditEvent.ToJson(), CreatedDate = DateTime.Now });
            }

            return entities;
        }));
```

# Contribute

If you like this project please contribute in any of the following ways:

- [Sponsoring this project](https://github.com/sponsors/thepirat000).
- [![paypal](https://img.shields.io/badge/donate-PayPal-blue.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=thepirat000%40hotmail.com&currency_code=USD&source=url)
