using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class OptInModeTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup().ForAnyContext().Reset();
            Audit.EntityFramework.Configuration.Setup().ForContext<OptInDbContext>().Reset();
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_OptIn_ByAttribute_EntityIncluded_NoPropertyExplicitlyIncluded()
        {
            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);
            using var ctx = new OptInDbContext();
            var entity = new MyEntity_NoPropertyExplicitlyIncluded()
            {
                Name = "Test Entity",
                IgnoredProp = "This property should not be included"
            };

            ctx.MyEntity_NoPropertyExplicitlyIncluded.Add(entity);

            ctx.SaveChanges();

            var events = dp.GetAllEventsOfType<AuditEventEntityFramework>();

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            var entry = events[0].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.ColumnValues, Has.Count.EqualTo(2)); // Id, Name
            Assert.That(entry.ColumnValues["Id"], Is.GreaterThan(0));
            Assert.That(entry.ColumnValues["Name"], Is.EqualTo("Test Entity"));
            Assert.That(entry.ColumnValues.ContainsKey("IgnoredProp"), Is.False);

            entity.Name = "Updated Entity";
            entity.IgnoredProp = "This property should still not be included";
            ctx.SaveChanges();

            events = dp.GetAllEventsOfType<AuditEventEntityFramework>();
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[1].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            entry = events[1].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.Changes, Has.Count.EqualTo(1)); // Name
            Assert.That(entry.Changes[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(entry.Changes[0].OriginalValue, Is.EqualTo("Test Entity"));
            Assert.That(entry.Changes[0].NewValue, Is.EqualTo("Updated Entity"));
        }

        [Test]
        public void Test_OptIn_ByAttribute_EntityIncluded_OnePropertyExplicitlyIncluded()
        {
            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

            using var ctx = new OptInDbContext();
            var entity = new MyEntity_OnePropertyExplicitlyIncluded()
            {
                Name = "Test Entity",
                Description = "This property should not be included",
                IgnoredProp = "This property should not be included"
            };

            ctx.MyEntity_OnePropertyExplicitlyIncluded.Add(entity);

            ctx.SaveChanges();

            var events = dp.GetAllEventsOfType<AuditEventEntityFramework>();

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            var entry = events[0].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.ColumnValues, Has.Count.EqualTo(1)); // Name
            Assert.That(entry.ColumnValues["Name"], Is.EqualTo("Test Entity"));
            Assert.That(entry.ColumnValues.ContainsKey("Id"), Is.False);
            Assert.That(entry.ColumnValues.ContainsKey("IgnoredProp"), Is.False);

            entity.Name = "Updated Entity";
            entity.IgnoredProp = "This property should still not be included";
            entity.Description = "This property should not be included either";
            ctx.SaveChanges();

            events = dp.GetAllEventsOfType<AuditEventEntityFramework>();
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[1].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            entry = events[1].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.Changes, Has.Count.EqualTo(1)); // Name
            Assert.That(entry.Changes[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(entry.Changes[0].OriginalValue, Is.EqualTo("Test Entity"));
            Assert.That(entry.Changes[0].NewValue, Is.EqualTo("Updated Entity"));
        }

        [Test]
        public void Test_OptIn_ByConfig_EntityIncluded_OnePropertyExplicitlyIncluded()
        {
            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<OptInDbContext>()
                .UseOptIn()
                .Include<MyEntity_ByConfig_OnePropertyExplicitlyIncluded>(config => config
                    .IncludeProperty(p => p.Name)
                    .IncludeProperty("Name2"));
                
            using var ctx = new OptInDbContext();
            var entity = new MyEntity_ByConfig_OnePropertyExplicitlyIncluded()
            {
                Name = "Test Entity",
                Name2 = "Test Entity 2",
                Description = "This property should not be included",
                IgnoredProp = "This property should not be included"
            };

            ctx.MyEntity_ByConfig_OnePropertyExplicitlyIncluded.Add(entity);

            ctx.SaveChanges();

            var events = dp.GetAllEventsOfType<AuditEventEntityFramework>();

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            var entry = events[0].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.ColumnValues, Has.Count.EqualTo(2)); // Name, Name2
            Assert.That(entry.ColumnValues["Name"], Is.EqualTo("Test Entity"));
            Assert.That(entry.ColumnValues["Name2"], Is.EqualTo("Test Entity 2"));
            Assert.That(entry.ColumnValues.ContainsKey("Id"), Is.False);
            Assert.That(entry.ColumnValues.ContainsKey("IgnoredProp"), Is.False);

            entity.Name = "Updated Entity";
            entity.Name2 = "Updated Entity 2";
            entity.IgnoredProp = "This property should still not be included";
            entity.Description = "This property should not be included either";
            ctx.SaveChanges();

            events = dp.GetAllEventsOfType<AuditEventEntityFramework>();
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[1].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            entry = events[1].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.Changes, Has.Count.EqualTo(2)); // Name, Name2
            Assert.That(entry.Changes[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(entry.Changes[0].OriginalValue, Is.EqualTo("Test Entity"));
            Assert.That(entry.Changes[0].NewValue, Is.EqualTo("Updated Entity"));
            Assert.That(entry.Changes[1].ColumnName, Is.EqualTo("Name2"));
            Assert.That(entry.Changes[1].OriginalValue, Is.EqualTo("Test Entity 2"));
            Assert.That(entry.Changes[1].NewValue, Is.EqualTo("Updated Entity 2"));
        }

        [Test]
        public void Test_OptIn_ByGlobalConfig_EntityIncluded_OnePropertyExplicitlyIncluded()
        {
            Audit.Core.Configuration.Setup().UseInMemoryProvider(out var dp);

            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext()
                .UseOptIn()
                .Include<MyEntity_ByConfig_OnePropertyExplicitlyIncluded>(config => config
                    .IncludeProperty(p => p.Name)
                    .IncludeProperty("Name2"));

            using var ctx = new OptInDbContext();
            var entity = new MyEntity_ByConfig_OnePropertyExplicitlyIncluded()
            {
                Name = "Test Entity",
                Name2 = "Test Entity 2",
                Description = "This property should not be included",
                IgnoredProp = "This property should not be included"
            };

            ctx.MyEntity_ByConfig_OnePropertyExplicitlyIncluded.Add(entity);

            ctx.SaveChanges();

            var events = dp.GetAllEventsOfType<AuditEventEntityFramework>();

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            var entry = events[0].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.ColumnValues, Has.Count.EqualTo(2)); // Name, Name2
            Assert.That(entry.ColumnValues["Name"], Is.EqualTo("Test Entity"));
            Assert.That(entry.ColumnValues["Name2"], Is.EqualTo("Test Entity 2"));
            Assert.That(entry.ColumnValues.ContainsKey("Id"), Is.False);
            Assert.That(entry.ColumnValues.ContainsKey("IgnoredProp"), Is.False);

            entity.Name = "Updated Entity";
            entity.Name2 = "Updated Entity 2";
            entity.IgnoredProp = "This property should still not be included";
            entity.Description = "This property should not be included either";
            ctx.SaveChanges();

            events = dp.GetAllEventsOfType<AuditEventEntityFramework>();
            Assert.That(events, Has.Count.EqualTo(2));
            Assert.That(events[1].EntityFrameworkEvent.Entries, Has.Count.EqualTo(1));
            entry = events[1].EntityFrameworkEvent.Entries[0];
            Assert.That(entry.Changes, Has.Count.EqualTo(2)); // Name, Name2
            Assert.That(entry.Changes[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(entry.Changes[0].OriginalValue, Is.EqualTo("Test Entity"));
            Assert.That(entry.Changes[0].NewValue, Is.EqualTo("Updated Entity"));
            Assert.That(entry.Changes[1].ColumnName, Is.EqualTo("Name2"));
            Assert.That(entry.Changes[1].OriginalValue, Is.EqualTo("Test Entity 2"));
            Assert.That(entry.Changes[1].NewValue, Is.EqualTo("Updated Entity 2"));
        }
    }

    [AuditDbContext(Mode = AuditOptionMode.OptIn, IncludeEntityObjects = true)]
    public class OptInDbContext : AuditDbContext
    {
        public DbSet<MyEntity_NoPropertyExplicitlyIncluded> MyEntity_NoPropertyExplicitlyIncluded { get; set; }
        public DbSet<MyEntity_OnePropertyExplicitlyIncluded> MyEntity_OnePropertyExplicitlyIncluded { get; set; }
        public DbSet<MyEntity_ByConfig_OnePropertyExplicitlyIncluded> MyEntity_ByConfig_OnePropertyExplicitlyIncluded { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("OptInTest");
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseLazyLoadingProxies();
        }
    }

    /// <summary>
    /// AuditInclude on the Entity class with no properties explicitly included, which means all properties are included by default.
    /// </summary>
    [AuditInclude]
    public class MyEntity_NoPropertyExplicitlyIncluded
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [AuditIgnore]
        public string IgnoredProp { get; set; }
    }
    
    /// <summary>
    /// AuditInclude on the Entity class with only one property explicitly included.
    /// </summary>
    [AuditInclude]
    public class MyEntity_OnePropertyExplicitlyIncluded
    {
        public int Id { get; set; }

        [AuditInclude]
        public string Name { get; set; }

        public string Description { get; set; }

        [AuditIgnore]
        public string IgnoredProp { get; set; }
    }

    public class MyEntity_ByConfig_OnePropertyExplicitlyIncluded
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Name2 { get; set; }

        public string Description { get; set; }

        public string IgnoredProp { get; set; }

    }
}
