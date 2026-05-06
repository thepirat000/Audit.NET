using Audit.Core;
using Audit.Core.Providers;
using Audit.IntegrationTest;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest;

[TestFixture]
[Category(TestCommon.Category.Integration)]
[Category(TestCommon.Category.SqlServer)]
public class ReloadDatabaseValuesAfterSaveTests
{
    [SetUp]
    public void Setup()
    {
        Audit.EntityFramework.Configuration.Setup().ForAnyContext().Reset();
        Audit.Core.Configuration.Reset();
    }

    [Test]
    public async Task ReloadDatabaseValuesAfterSave_InsertAndUpdate_ShouldReloadComputedColumns()
    {
        Audit.Core.Configuration.Setup().UseNullProvider();
        Audit.Core.Configuration.ExcludeEnvironmentInfo = true;

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<ReloadTestContext>(c => c
                .IncludeEntityObjects()
                .ReloadDatabaseValuesAfterSave()
                .MapChangesByColumn()
                .ForEntity<Thing>(c => c
                    .Ignore(p => p.IgnoreMe)
                    .Format(p => p.FormatMe, v => $"Formatted: {v}"))
            )
            .UseOptOut();

        var dbName = $"Reload_{Guid.NewGuid().ToString()[0..7]}";

        var opts = new DbContextOptionsBuilder<ReloadTestContext>()
            .UseSqlServer(TestHelper.GetConnectionString(dbName))
            .Options;

        var dataProvider = new InMemoryDataProvider();
        var nullProvider = new NullDataProvider();

        // Create DB, no audit
        await using (var ctx = new ReloadTestContext(opts, nullProvider))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();
        }

        // Insert
        var name = "My Thing";
        await using (var ctx = new ReloadTestContext(opts, dataProvider))
        {
            var thing = new Thing { Name = name, FormatMe = "123" };
            ctx.Things.Add(thing);
            await ctx.SaveChangesAsync();
        }

        // Update
        var updatedName = "My Thing updated";
        await using (var ctx = new ReloadTestContext(opts, dataProvider))
        {
            var thing = await ctx.Things.FirstAsync();
            thing.Name = updatedName;
            thing.FormatMe = "1234";
            await ctx.SaveChangesAsync();
        }

        // Delete
        await using (var ctx = new ReloadTestContext(opts, dataProvider))
        {
            var thing = await ctx.Things.FirstAsync();
            ctx.Things.Remove(thing);
            await ctx.SaveChangesAsync();
        }

        var entries = dataProvider.GetAllEventsOfType<AuditEventEntityFramework>().SelectMany(ev => ev.EntityFrameworkEvent.Entries).ToList();

        // Assert
        Assert.That(entries, Has.Count.EqualTo(3));

        Assert.Multiple(() =>
        {
            Assert.That(entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(entries[0].ColumnValues["initial_name_length"], Is.EqualTo(name.Length));
            Assert.That(entries[0].ColumnValues, Does.Not.ContainKey("ignore_me"));
            Assert.That(entries[0].ColumnValues["format_me"], Is.EqualTo("Formatted: 123"));

            Assert.That(entries[1].Action, Is.EqualTo("Update"));
            Assert.That(entries[1].ColumnValues["initial_name_length"], Is.EqualTo(updatedName.Length));
            Assert.That(entries[1].ColumnValues, Does.Not.ContainKey("ignore_me"));
            Assert.That(entries[1].ColumnValues["format_me"], Is.EqualTo("Formatted: 1234"));

            Assert.That(entries[2].Action, Is.EqualTo("Delete"));
        });

        await using (var ctx = new ReloadTestContext(opts, nullProvider))
        {
            await ctx.Database.EnsureDeletedAsync();
        }
    }
    

    public sealed class ReloadTestContext : AuditDbContext
    {
        public ReloadTestContext(DbContextOptions<ReloadTestContext> opts, IAuditDataProvider dp) : base(opts)
        {
            this.AuditDataProvider = dp;
        }
        
        public DbSet<Thing> Things => Set<Thing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Thing>(e =>
            {
                e.ToTable("things");

                e.HasKey(p => p.Id);
                e.Property(p => p.Id)
                    .ValueGeneratedOnAdd()
                    .HasDefaultValueSql("NEWSEQUENTIALID()")
                    .HasColumnName("id");

                e.Property(p => p.Name)
                    .IsRequired()
                    .HasColumnName("name");

                e.Property(p => p.FormatMe)
                    .IsRequired()
                    .HasColumnName("format_me");

                e.Property(p => p.InitialNameLength)
                    .IsRequired()
                    .HasComputedColumnSql("LEN(name)", false)
                    .HasColumnName("initial_name_length");
            });
        }
    }

    public sealed class Thing
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public long InitialNameLength { get; set; }
        [AuditIgnore]
        public string IgnoreMe { get; set; }

        public string FormatMe { get; set; }
    }
}