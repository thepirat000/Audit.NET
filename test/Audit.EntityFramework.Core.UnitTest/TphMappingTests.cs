#if EF_CORE_7_OR_GREATER
using Audit.Core;
using Audit.Core.Providers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest;

[TestFixture]
public class TphMappingTests
{
    [SetUp]
    public void Setup()
    {
        Audit.Core.Configuration.Reset();
        Audit.EntityFramework.Configuration.Setup().ForAnyContext().Reset();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TphUpdate_Contains_CorrectColumnNames(bool mapChangesByColumn)
    {
        Audit.Core.Configuration.Setup().UseNullProvider();
        Audit.Core.Configuration.ExcludeEnvironmentInfo = true;

        Audit.EntityFramework.Configuration.Setup()
            .ForContext<TphTestContext>(c => c.IncludeEntityObjects().ReloadDatabaseValues().MapChangesByColumn(mapChangesByColumn))
            .UseOptOut();

        var opts = new DbContextOptionsBuilder<TphTestContext>()
            .UseInMemoryDatabase(nameof(TphMappingTests) + "_" + Guid.NewGuid())
            .Options;

        var dataProvider = new InMemoryDataProvider();
        var nullProvider = new NullDataProvider();

        // Insert, no audit
        await using (var ctx = new TphTestContext(opts, nullProvider))
        {
            await ctx.Database.EnsureDeletedAsync();
            await ctx.Database.EnsureCreatedAsync();
            ctx.Bases.Add(new ThingOne { Title = "Thing1", ValueOne = 33 });
            ctx.Bases.Add(new ThingTwo { Title = "Thing2", ValueTwo = 66 });
            await ctx.SaveChangesAsync();
        }

        // Update, audit
        await using (var ctx = new TphTestContext(opts, dataProvider))
        {
            var thing2 = await ctx.Set<ThingTwo>().SingleAsync();
            thing2.Title = "Thing2-NewTitle";
            await ctx.SaveChangesAsync();
        }

        var events = dataProvider.GetAllEventsOfType<AuditEventEntityFramework>();

        Assert.That(events, Has.Count.EqualTo(1));

        var entry = events[0].EntityFrameworkEvent.Entries[0];

        Assert.Multiple(() =>
        {
            if (mapChangesByColumn)
            {
                Assert.That(entry.ChangesByColumn, Does.ContainKey("title"));
                Assert.That(entry.ChangesByColumn["title"].OriginalValue, Is.EqualTo("Thing2"));
                Assert.That(entry.ChangesByColumn["title"].NewValue, Is.EqualTo("Thing2-NewTitle"));
            }
            else
            {
                Assert.That(entry.Changes, Has.Count.EqualTo(1));
                Assert.That(entry.Changes[0].ColumnName, Is.EqualTo("title"));
                Assert.That(entry.Changes[0].OriginalValue, Is.EqualTo("Thing2"));
                Assert.That(entry.Changes[0].NewValue, Is.EqualTo("Thing2-NewTitle"));
            }

            Assert.That(entry.ColumnValues, Does.ContainKey("id"));
            Assert.That(entry.ColumnValues, Does.ContainKey("title"));
            Assert.That(entry.ColumnValues, Does.ContainKey("value_two"));
            Assert.That(entry.ColumnValues, Does.ContainKey("dtype"));

            Assert.That(entry.PrimaryKey, Does.ContainKey("id"));
            Assert.That(entry.Table, Is.EqualTo("things"));
        });
    }

    public sealed class TphTestContext : AuditDbContext
    {
        public TphTestContext(DbContextOptions<TphTestContext> opts, IAuditDataProvider dp) : base(opts)
        {
            AuditDataProvider = dp;
        }

        public DbSet<ThingBase> Bases => Set<ThingBase>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ThingBase>(e =>
            {
                e.UseTphMappingStrategy();

                e.ToTable("things");
                e.HasKey(p => p.Id);

                e.Property(p => p.Id)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("id");

                e.Property(p => p.Title)
                    .IsRequired()
                    .HasColumnName("title");

                e.HasDiscriminator<string>("DType")
                    .HasValue<ThingOne>("ONE")
                    .HasValue<ThingTwo>("TWO");

                e.Property<string>("DType").HasColumnName("dtype");
            });

            modelBuilder.Entity<ThingOne>(e =>
            {
                e.Property(p => p.ValueOne)
                    .IsRequired()
                    .HasColumnName("value_one");
            });

            modelBuilder.Entity<ThingTwo>(e =>
            {
                e.Property(p => p.ValueTwo)
                    .IsRequired()
                    .HasColumnName("value_two");
            });
        }
    }

    public abstract class ThingBase
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
    }

    public sealed class ThingOne : ThingBase
    {
        public int ValueOne { get; set; }
    }

    public sealed class ThingTwo : ThingBase
    {
        public int ValueTwo { get; set; }
    }
}
#endif
