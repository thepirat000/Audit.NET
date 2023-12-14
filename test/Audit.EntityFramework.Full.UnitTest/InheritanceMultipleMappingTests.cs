#if NET462 || NET472
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("LocalDb")]
    public class InheritanceMultipleMappingTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.AuditDisabled = false;
            Database.SetInitializer<DataBaseContext>(null);
        }

        [Test]
        public void Test_InheritanceWithMultipleMappings()
        {
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(_ => _.OnInsert(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));

            using (DataBaseContext dbContext = new DataBaseContext())
            {
                dbContext.Database.CreateIfNotExists();
                dbContext.Database.Initialize(true);
            }

            using (DataBaseContext context = new DataBaseContext())
            {
                var leafTwo = new LeafFour
                {
                    Attention = "Attention",
                };
                DbSet<LeafFour> set = context.Set<LeafFour>();
                set.Add(leafTwo);
                context.SaveChanges();
            }
            using (DataBaseContext context = new DataBaseContext())
            {
                LeafFour entity = context.Set<LeafFour>().First();
                entity.Attention = "AttentionUpdate";
                context.SaveChanges();
            }
            using (DataBaseContext context = new DataBaseContext())
            {
                LeafFour entity = context.Set<LeafFour>().First();
                DbSet<LeafFour> set = context.Set<LeafFour>();
                set.Remove(entity);
                context.SaveChanges();
            }

            Assert.AreEqual(3, evs.Count);
            Assert.AreEqual(1, evs[0].Entries.Count);
            Assert.AreEqual(1, evs[1].Entries.Count);
            Assert.AreEqual(1, evs[2].Entries.Count);
            Assert.AreEqual("Insert", evs[0].Entries[0].Action);
            Assert.AreEqual("Update", evs[1].Entries[0].Action);
            Assert.AreEqual("Delete", evs[2].Entries[0].Action);
            Assert.AreEqual("Attention", evs[0].Entries[0].ColumnValues["Attention"]);
            Assert.AreEqual("AttentionUpdate", evs[1].Entries[0].ColumnValues["Attention"]);
            Assert.AreEqual("LeafFour", evs[0].Entries[0].Table);
            Assert.AreEqual("LeafFour", evs[1].Entries[0].Table);
            Assert.AreEqual("LeafFour", evs[2].Entries[0].Table);
            Assert.IsTrue(evs[0].Entries[0].ColumnValues.ContainsKey("LeafProp"));
            Assert.IsTrue(evs[1].Entries[0].ColumnValues.ContainsKey("LeafProp"));
            Assert.IsTrue(evs[2].Entries[0].ColumnValues.ContainsKey("LeafProp"));
        }
    }


    public class DataBaseContext : DbContext
    {
        private static DbContextHelper _helper = new DbContextHelper();
        private readonly IAuditDbContext _auditContext;
        public static string CnnString = TestHelper.GetConnectionString("inheritance_test_multi");

        public DataBaseContext() : base(CnnString)
        {
            _auditContext = new DefaultAuditContext(this);
            _helper.SetConfig(_auditContext);

            //comments this lines and the problem persists.
            Audit.EntityFramework.Configuration.Setup().ForContext<DataBaseContext>(config =>
            {
                config.IncludeEntityObjects();
                config.IncludeIndependantAssociations();
            }).UseOptOut();
        }

        public override int SaveChanges()
        {
            return _helper.SaveChanges(_auditContext, () => base.SaveChanges());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Register Entity types
            List<Type> entityTypesToRegister = Assembly.GetExecutingAssembly().GetTypes().Where(type =>
                type.BaseType != null && !type.IsAbstract && type.BaseType.IsGenericType &&
                (type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>) ||
                 type.BaseType.GetGenericTypeDefinition() == typeof(BaseMap<>))).ToList();

            foreach (object configurationInstance in entityTypesToRegister.Select(Activator.CreateInstance))
                modelBuilder.Configurations.Add((dynamic)configurationInstance);

        }
    }

    //entities
    public enum ItemType
    {
        Type1,
        Type2,
        Type3,
        Type4,
    }
    [Serializable]
    public abstract class BaseClass
    {
        public int Id { get; set; }

        public bool Active { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        protected BaseClass()
        {
            Active = true;
        }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
    public abstract class Root : BaseClass
    {
        public ItemType Type { get; protected set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Attention { get; set; }
        public string AttentionComment { get; set; }
        public virtual IEnumerable AsEnumerable()
        {
            return null;
        }
    }
    public abstract class MiddleTwo : Root
    {
        public bool? IncludePendingResults { get; set; }
        public bool? IncludeStandardTests { get; set; }
        public bool? IncludeMicrobiologyTests { get; set; }
        public bool? IncludeToxicologyTests { get; set; }
        public bool? IncludePathologyTests { get; set; }
    }
    public class LeafFour : MiddleTwo
    {
        public LeafFour()
        {
            Type = ItemType.Type4;
        }
        public string LeafProp { get; set; }
    }

    // mapping
    abstract class BaseMap<T> : EntityTypeConfiguration<T> where T : BaseClass
    {
        protected BaseMap()
        {
            HasKey(t => t.Id);
            Property(t => t.Active)
                .IsRequired();
            Property(t => t.RowVersion)
                .IsRowVersion();
        }
    }
    class RootMap : BaseMap<Root>
    {
        public RootMap()
        {
            // Properties
            Property(t => t.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
            Property(t => t.Type)
                .IsRequired();

            // Db Mappings
            ToTable("TransmissionItems");
        }
    }
    class LeafFourMap : BaseMap<LeafFour>
    {
        public LeafFourMap()
        {
            // Db Mappings
            Map(t => t.Requires(m => m.Type).Equals(ItemType.Type4)).ToTable("LeafFour");
        }
    }
}
#endif