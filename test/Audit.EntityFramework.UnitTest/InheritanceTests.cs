using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Linq;
using DataBaseService;
using NUnit.Framework;

namespace Audit.EntityFramework.UnitTest
{
    [TestFixture]
    [Category("Sql")]
    public class InheritanceTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            DbConfiguration.Loaded += (_, a) =>
            {
                a.ReplaceService<DbProviderServices>((s, k) => SqlProviderServices.Instance);
                a.ReplaceService<IDbConnectionFactory>((s, k) => new SqlConnectionFactory("data source=(local);Initial Catalog=AuditTest;Integrated Security=True"));
            };
        }

        [SetUp]
        public void SetUp()
        {
             Configuration.Setup()
                .ForContext<BlogsEntities>().Reset();
            Configuration.Setup()
                .ForContext<Entities>().Reset();
        }

        [Test]
        public void Test_Ef_Inheritance()
        {
            var guid = Guid.NewGuid().ToString();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));
            using (DataBaseContext dbContext = new DataBaseContext())
            {
                dbContext.Database.Initialize(false);
            }
            int id;
            using (DataBaseContext context = new DataBaseContext())
            {
                var another = new AnotherEntity();
                another.AnotherColumn = guid;
                context.AnotherEntities.Add(another);
                context.SaveChanges();

                DBEntity ent = new DBEntity();
                ent.Name = "Base";
                ent.Name2 = "Inherited";
                ent.Ticks = 123;
                ent.Timeout = TimeSpan.FromMinutes(5);

                context.Entities.Add(ent);
                context.SaveChanges();
                id = ent.ID;
            }
            using (DataBaseContext context = new DataBaseContext())
            {
                DBEntityBase bse = context.Entities.First(x => x.ID == id);

                if (!(bse is DBEntity))
                    throw new Exception("1");

                DBEntity ent = (DBEntity) bse;

                ent.Name = "Base 2";
                ent.Name2 = "Inherited 2";
                ent.Ticks = 456;
                ent.Timeout = TimeSpan.FromMinutes(10);

                context.SaveChanges();
            }
            using (DataBaseContext context = new DataBaseContext())
            {
                DBEntityBase bse = context.Entities.First(x => x.ID == id);

                context.Entities.Remove(bse);
                context.SaveChanges();
            }

            // add another, add, update, delete
            Assert.AreEqual(4, evs.Count);

            Assert.AreEqual("Insert", evs[0].Entries[0].Action);
            Assert.AreEqual("Another", evs[0].Entries[0].Table);
            Assert.AreEqual(guid, (evs[0].Entries[0].Entity as dynamic).AnotherColumn);

            Assert.AreEqual("Insert", evs[1].Entries[0].Action);
            Assert.AreEqual("Entities", evs[1].Entries[0].Table);
            Assert.AreEqual("Inherited", (evs[1].Entries[0].Entity as dynamic).Name2);

            Assert.AreEqual("Update", evs[2].Entries[0].Action);
            Assert.AreEqual("Entities", evs[2].Entries[0].Table);

            Assert.AreEqual(4, evs[2].Entries[0].Changes.Count);
            Assert.IsTrue(evs[2].Entries[0].Changes.Any(ch => 
                ch.ColumnName == "Timeout" && ((TimeSpan)ch.OriginalValue) == TimeSpan.FromMinutes(5) && ((TimeSpan)ch.NewValue) == TimeSpan.FromMinutes(10)));
            Assert.IsFalse(evs[2].Entries[0].Changes.Any(ch => ch.ColumnName == null));
            Assert.AreEqual("Inherited 2", (evs[2].Entries[0].Entity as dynamic).Name2);

            Assert.AreEqual("Delete", evs[3].Entries[0].Action);
        }

        [Test]
        public void Test_Ef_Inheritance_Double()
        {
            var guid = Guid.NewGuid().ToString();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));
            using (DataBaseContext dbContext = new DataBaseContext())
            {
                dbContext.Database.Initialize(false);
            }
            DBEntity3 ent;
            using (DataBaseContext context = new DataBaseContext())
            {
                ent = new DBEntity3();
                ent.Name = "Base";
                ent.Name2 = "2";
                ent.Name3 = guid;
                ent.Ticks = 123;
                ent.Timeout = TimeSpan.FromMinutes(5);

                context.Entities.Add(ent);
                context.SaveChanges();
            }

            using (DataBaseContext context = new DataBaseContext())
            {
                ent = context.Entities.First(e => e.ID == ent.ID) as DBEntity3;
                ent.Name = "BaseX";
                ent.Name2 = "2X";
                ent.Name3 = guid + "X";
                ent.Ticks = 1234;
                ent.Timeout = TimeSpan.FromMinutes(10);

                context.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);

            Assert.AreEqual("Insert", evs[0].Entries[0].Action);
            Assert.AreEqual("Entities3", evs[0].Entries[0].Table);
            Assert.AreEqual(guid, (evs[0].Entries[0].Entity as dynamic).Name3);

            //ningun columnname null, todos los name_ newvalue terminan en X
            Assert.IsFalse(evs[1].Entries[0].Changes.Any(e => e.ColumnName == null));
            Assert.IsTrue(evs[1].Entries[0].Changes.Where(e => e.ColumnName.StartsWith("Name"))
                .All(e => e.NewValue.ToString().EndsWith("X")));

        }
    }


}
