using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfChangeTrackerTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new SimpleMemoryContext().Database.EnsureCreated();
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public async Task EF_Change_On_Foreign_Key_Reflected_On_Changes_Property()
        {
            var evs = new List<AuditEventEntityFramework>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<SimpleMemoryContext>(x => x
                    .IncludeEntityObjects());
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add((AuditEventEntityFramework)ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            SimpleMemoryContext.Car car;
            using (var context = new SimpleMemoryContext())
            {
                var carName = Guid.NewGuid().ToString();
                var brandName = Guid.NewGuid().ToString();
                car = new SimpleMemoryContext.Car() { Name = carName };

                context.Cars.Add(car);

                await context.SaveChangesAsync();

                car.Brand = new SimpleMemoryContext.Brand() { Name = brandName };
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(2, evs[1].EntityFrameworkEvent.Entries.Count);

            Assert.IsTrue(evs[1].EntityFrameworkEvent.Entries.Any(e => e.Entity is SimpleMemoryContext.Brand));
            var evEntityCar = evs[1].EntityFrameworkEvent.Entries.FirstOrDefault(e => e.Entity is SimpleMemoryContext.Car);
            Assert.IsNotNull(evEntityCar);
            Assert.IsTrue(evEntityCar.Changes.Any(ch => ch.ColumnName == "BrandId"));
            Assert.IsNull(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").OriginalValue);
            Assert.IsNotNull(evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue);
            Assert.IsTrue(car.BrandId > 0);
            Assert.AreEqual(car.BrandId, evEntityCar.Changes.First(ch => ch.ColumnName == "BrandId").NewValue);
        }
    }
}