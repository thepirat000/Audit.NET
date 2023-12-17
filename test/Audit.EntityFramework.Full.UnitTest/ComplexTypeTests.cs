using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
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

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(1));
            Assert.That(evs[1].Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[1].Entries[0].Action, Is.EqualTo("Update"));
            Assert.That(evs[0].Entries[0].ColumnValues["Address"] is Address, Is.True);
            Assert.That((evs[0].Entries[0].ColumnValues["Address"] as Address).City, Is.EqualTo("City 1"));
            Assert.That((evs[1].Entries[0].ColumnValues["Address"] as Address).City, Is.EqualTo("City 2"));
            Assert.That((evs[1].Entries[0].Changes.Any(ch => ch.ColumnName == "Address" && (ch.OriginalValue as Address)?.City == "City 1")), Is.True);
            Assert.That((evs[1].Entries[0].Changes.Any(ch => ch.ColumnName == "Address" && (ch.NewValue as Address)?.City == "City 2")), Is.True);

        }
    }
}
