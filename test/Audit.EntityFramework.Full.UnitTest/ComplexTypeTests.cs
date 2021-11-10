using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("LocalDb")]
    public class ComplexTypeTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Test_ComplexType_Logging()
        {
            var evs = new List<EntityFrameworkEvent>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(config =>
                    config.OnInsert(ev => evs.Add(ev.GetEntityFrameworkEvent())));

            // Reset previous config
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<WorkContext>().Reset();

            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(_ => _.IncludeEntityObjects());

            using (var context = new WorkContext())
            {
                var employee = context.Set<Employee>().FirstOrDefault();
                employee.Address.City = "City 2";
                context.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(1, evs[0].Entries.Count);
            Assert.AreEqual(1, evs[1].Entries.Count);
            Assert.AreEqual("Insert", evs[0].Entries[0].Action);
            Assert.AreEqual("Update", evs[1].Entries[0].Action);
            Assert.IsTrue(evs[0].Entries[0].ColumnValues["Address"] is Address);
            Assert.AreEqual("City 1", (evs[0].Entries[0].ColumnValues["Address"] as Address).City);
            Assert.AreEqual("City 2", (evs[1].Entries[0].ColumnValues["Address"] as Address).City);
            Assert.IsTrue((evs[1].Entries[0].Changes.Any(ch => ch.ColumnName == "Address" && (ch.OriginalValue as Address)?.City == "City 1")));
            Assert.IsTrue((evs[1].Entries[0].Changes.Any(ch => ch.ColumnName == "Address" && (ch.NewValue as Address)?.City == "City 2")));

        }
    }
}
