using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("Integration-SqlServer")]
    public class EfChangeTrackerTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new SimpleContext().Database.CreateIfNotExists();
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public async Task EF_Change_On_Foreign_Key_Reflected_On_Changes_Property()
        {
            var evs = new List<AuditEventEntityFramework>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<SimpleContext>(x => x
                    .IncludeEntityObjects());
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add((AuditEventEntityFramework)ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            SimpleContext.Car car;
            using (var context = new SimpleContext())
            {
                var carName = Guid.NewGuid().ToString();
                var brandName = Guid.NewGuid().ToString();
                car = new SimpleContext.Car() { Name = carName };

                context.Cars.Add(car);

                await context.SaveChangesAsync();

                car.Brand = new SimpleContext.Brand() { Name = brandName };
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(2, evs[1].EntityFrameworkEvent.Entries.Count);

            Assert.IsTrue(evs[1].EntityFrameworkEvent.Entries.Any(e => e.Entity is SimpleContext.Brand));
            var evEntityCar = evs[1].EntityFrameworkEvent.Entries.FirstOrDefault(e => e.Entity is SimpleContext.Car);
            Assert.IsNotNull(evEntityCar);
            Assert.IsTrue(evEntityCar.Changes.Any(ch => ch.ColumnName == "BrandId"));
            Assert.IsNull(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").OriginalValue);
            Assert.IsNotNull(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue);
            Assert.IsTrue(car.BrandId > 0);
            Assert.AreEqual(car.BrandId, evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue);
        }

        [Test]
        public async Task EF_Update_ReloadFromDatabase()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<SimpleContext>(x => x
                    .ReloadDatabaseValues(true)
                    .IncludeEntityObjects());

            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseInMemoryProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var evs = Audit.Core.Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEvents();

            var car = new SimpleContext.Car()
            {
                Name = "OriginalName"
            };

            using (var context = new SimpleContext())
            {
                context.Cars.Add(car);
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(1, evs[0].GetEntityFrameworkEvent()?.Entries.Count);
            Assert.AreEqual("Insert", evs[0].GetEntityFrameworkEvent()?.Entries[0].Action);
            Assert.AreEqual("OriginalName", evs[0].GetEntityFrameworkEvent()?.Entries[0].ColumnValues["Name"]);

            using (var context = new SimpleContext())
            {
                context.Cars.AddOrUpdate(new SimpleContext.Car() { Id = car.Id, Name = "UpdatedName" });
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(1, evs[1].GetEntityFrameworkEvent()?.Entries.Count);
            Assert.AreEqual("Update", evs[1].GetEntityFrameworkEvent()?.Entries[0].Action);
            Assert.AreEqual("OriginalName", evs[1].GetEntityFrameworkEvent()?.Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Name")?.OriginalValue?.ToString());
            Assert.AreEqual("UpdatedName", evs[1].GetEntityFrameworkEvent()?.Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Name")?.NewValue?.ToString());

            using (var context = new SimpleContext())
            {
                var carToDelete = new SimpleContext.Car() { Id = car.Id };
                context.Entry(carToDelete).State = EntityState.Deleted;
                await context.SaveChangesAsync();

            }

            Assert.AreEqual(3, evs.Count);
            Assert.AreEqual(1, evs[2].GetEntityFrameworkEvent()?.Entries.Count);
            Assert.AreEqual("Delete", evs[2].GetEntityFrameworkEvent()?.Entries[0].Action);
            Assert.AreEqual("UpdatedName", evs[2].GetEntityFrameworkEvent()?.Entries[0].ColumnValues["Name"]?.ToString());
        }
    }
}