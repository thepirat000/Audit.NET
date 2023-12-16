using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
    public class InheritanceTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.AuditDisabled = false;
        }

        [Test]
        public void Test_NoInheritance_MultipleContexts()
        {
            var evs = new List<AuditEventEntityFramework>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(config =>
                    config.OnInsert(ev => evs.Add(ev as AuditEventEntityFramework)));

            // Reset previous config
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DepartmentContext1>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DepartmentContext2>().Reset();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DepartmentContext1>(_ => _
                    .IncludeEntityObjects()
                    .ForEntity<Department>(emp => emp.Override(e => e.Name, e => "Override " + e.CurrentValues["Name"]))
                    .AuditEventType("Type1"));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DepartmentContext2>(_ => _
                    .IncludeEntityObjects(false)
                    .ForEntity<Department>(emp => emp.Override(e => e.Name, "Override 2"))
                    .AuditEventType("Type2"));

            using (var context = new DepartmentContext1())
            {
                var set = context.Set<Department>();
                var dept = new Department()
                {
                    Name = "test 1",
                    Location = "location 1"
                };
                set.Add(dept);
                context.SaveChanges();
            }

            using (var context2 = new DepartmentContext2())
            {
                var set = context2.Set<Department>();
                var dept = new Department()
                {
                    Name = "test 2",
                    Location = "location 2"
                };
                set.Add(dept);
                context2.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(1, evs[0].EntityFrameworkEvent.Entries.Count);
            Assert.AreEqual(1, evs[1].EntityFrameworkEvent.Entries.Count);
            Assert.AreEqual("Insert", evs[0].EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual("Insert", evs[1].EntityFrameworkEvent.Entries[0].Action);

            Assert.AreEqual("Type1", evs[0].EventType);
            Assert.AreEqual("Type2", evs[1].EventType);

            Assert.AreEqual("Override test 1", evs[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"] as string);
            Assert.AreEqual("Override 2", evs[1].EntityFrameworkEvent.Entries[0].ColumnValues["Name"] as string);
            Assert.IsNotNull(evs[0].EntityFrameworkEvent.Entries[0].Entity);
            Assert.IsNull(evs[1].EntityFrameworkEvent.Entries[0].Entity);
        }

        [Test]
        public void Test_EF_EntityInheritance()
        {
            var evs = new List<AuditEventEntityFramework>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<ProjectContext>(c => c
                   .IncludeEntityObjects()
                   .IncludeIndependantAssociations())
                .UseOptOut()
                .IgnoreAny(t => t.Name.EndsWith("Audit"));

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev as AuditEventEntityFramework);
                }));


            var cnnString = TestHelper.GetConnectionString("inheritance_test");

            using (var ctx = new ProjectContext(cnnString))
            {
                ctx.Database.CreateIfNotExists();
            }

            var cm = new ClientMapping();
            using (var db = new ProjectContext(cnnString))
            {
                cm.PropertyOne = "one";
                cm.PropertyTwo = "two";
                db.Mapping.Add(cm);
                db.SaveChanges();
            }

            using (var ctx = new ProjectContext(cnnString))
            {
                ctx.Database.Delete();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(1, evs[0].EntityFrameworkEvent.Entries.Count);
            Assert.AreEqual("Mapping", evs[0].EntityFrameworkEvent.Entries[0].Table);
            Assert.AreEqual(3, evs[0].EntityFrameworkEvent.Entries[0].ColumnValues.Count);
            Assert.IsNotNull(evs[0].EntityFrameworkEvent.Entries[0].ColumnValues["Id"]);
            Assert.IsNotNull(evs[0].EntityFrameworkEvent.Entries[0].Entity);
            Assert.AreEqual(typeof(ClientMapping), evs[0].EntityFrameworkEvent.Entries[0].Entity.GetType());
            Assert.AreEqual("one", evs[0].EntityFrameworkEvent.Entries[0].ColumnValues["PropertyOne"]);
            Assert.AreEqual("two", evs[0].EntityFrameworkEvent.Entries[0].ColumnValues["PropertyTwo"]);

        }

    }

    public abstract class Mapping
    {
        public int Id { get; private set; }
        public string PropertyOne { get; set; }
        public string PropertyTwo { get; set; }
    }

    public class MappingConfiguration : EntityTypeConfiguration<Mapping>
    {
        public MappingConfiguration()
        {
            ToTable("Mapping");

            Map<ClientMapping>(m => m.Requires("EntityType").HasValue("Client")); // EntityType is the discrimitator
            Map<DivisionMapping>(m => m.Requires("EntityType").HasValue("Division"));
            Map<ProductMapping>(m => m.Requires("EntityType").HasValue("Product"));

            Property(m => m.PropertyOne)
               .IsRequired();

            Property(m => m.PropertyTwo)
               .IsRequired();
        }
    }

    public class ClientMapping : Mapping
    {
    }
    public class DivisionMapping : Mapping
    {
    }
    public class ProductMapping : Mapping
    {
    }

    public class ProjectAuditContext : AuditDbContext
    {
        public ProjectAuditContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }
        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }

    public class ProjectContext : ProjectAuditContext//, IProjectContext
    {
        public ProjectContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public DbSet<Mapping> Mapping { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.AddFromAssembly(typeof(MappingConfiguration).Assembly);
        }

        public override Task<int> SaveChangesAsync()
        {
            return SaveChangesAsync(CancellationToken.None);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
