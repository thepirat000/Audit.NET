using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Audit.IntegrationTest;
using NUnit.Framework;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
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

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[1].EntityFrameworkEvent.Entries.Count, Is.EqualTo(2));

            Assert.That(evs[1].EntityFrameworkEvent.Entries.Any(e => e.Entity is SimpleContext.Brand), Is.True);
            var evEntityCar = evs[1].EntityFrameworkEvent.Entries.FirstOrDefault(e => e.Entity is SimpleContext.Car);
            Assert.That(evEntityCar, Is.Not.Null);
            Assert.That(evEntityCar.Changes.Any(ch => ch.ColumnName == "BrandId"), Is.True);
            Assert.That(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").OriginalValue, Is.Null);
            Assert.That(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue, Is.Not.Null);
            Assert.That(car.BrandId > 0, Is.True);
            Assert.That(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue, Is.EqualTo(car.BrandId));
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent()?.Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent()?.Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[0].GetEntityFrameworkEvent()?.Entries[0].ColumnValues["Name"], Is.EqualTo("OriginalName"));

            using (var context = new SimpleContext())
            {
                context.Cars.AddOrUpdate(new SimpleContext.Car() { Id = car.Id, Name = "UpdatedName" });
                await context.SaveChangesAsync();
            }

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[1].GetEntityFrameworkEvent()?.Entries.Count, Is.EqualTo(1));
            Assert.That(evs[1].GetEntityFrameworkEvent()?.Entries[0].Action, Is.EqualTo("Update"));
            Assert.That(evs[1].GetEntityFrameworkEvent()?.Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Name")?.OriginalValue?.ToString(), Is.EqualTo("OriginalName"));
            Assert.That(evs[1].GetEntityFrameworkEvent()?.Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Name")?.NewValue?.ToString(), Is.EqualTo("UpdatedName"));

            using (var context = new SimpleContext())
            {
                var carToDelete = new SimpleContext.Car() { Id = car.Id };
                context.Entry(carToDelete).State = EntityState.Deleted;
                await context.SaveChangesAsync();

            }

            Assert.That(evs.Count, Is.EqualTo(3));
            Assert.That(evs[2].GetEntityFrameworkEvent()?.Entries.Count, Is.EqualTo(1));
            Assert.That(evs[2].GetEntityFrameworkEvent()?.Entries[0].Action, Is.EqualTo("Delete"));
            Assert.That(evs[2].GetEntityFrameworkEvent()?.Entries[0].ColumnValues["Name"]?.ToString(), Is.EqualTo("UpdatedName"));
        }
    }
}