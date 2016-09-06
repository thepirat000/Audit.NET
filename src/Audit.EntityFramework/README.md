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
public class MyEntitites : DbContext
{
    public DbSet<Blog> Blogs { get; set; }
    public DbSet<Post> Posts { get; set; }
}
```

to enable the audit log, you should change it to this:
```c#
public class MyEntitites : Audit.EntityFramework.AuditDbContext
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
- **IncludeEntityObjects**: To indicate if the output should contain the modified entities objects. (Default is false)
- **AuditEventType**: To indicate the event type to use on the audit event. (Default is the context name). Can contain the following placeholder: 
  - {context}: replaced with the Db Context type name.
  - {database}: replaced with the database name.

For example:
```c#
[AuditDbContext(Mode = AuditOptionMode.OptIn, IncludeEntityObjects = false, AuditEventType = "{database}_{context}" )]
public class MyEntitites : Audit.EntityFramework.AuditDbContext
{
...
```

You can also change the settings by changing the  
