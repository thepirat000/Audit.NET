using NUnit.Framework;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("LocalDb")]
    public class InheritanceTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.AuditDisabled = false;
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


            using (var ctx = new ProjectContext("data source=localhost;initial catalog=inheritance_test;integrated security=true;"))
            {
                ctx.Database.CreateIfNotExists();
            }

            var cm = new ClientMapping();
            using (var db = new ProjectContext("data source=localhost;initial catalog=inheritance_test;integrated security=true;"))
            {
                cm.PropertyOne = "one";
                cm.PropertyTwo = "two";
                db.Mapping.Add(cm);
                db.SaveChanges();
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
