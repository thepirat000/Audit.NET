using NUnit.Framework;
using System.Collections.Generic;

namespace Audit.EntityFramework.Core.UnitTest
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
                    .AuditEventType("{context}:Type1"));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DepartmentContext2>(_ => _
                    .IncludeEntityObjects(false)
                    .ForEntity<Department>(emp => emp.Override(e => e.Name, "Override 2"))
                    .AuditEventType("{context}:Type2"));

            using (var context = new DepartmentContext1())
            {
                context.Database.EnsureCreated();
                var set = context.Set<Department>();
                var dept = new Department()
                {
                    Name = "test 1",
                    Location = "location 1"
                };
                set.Add(dept);
                context.SaveChanges(true);
            }

            using (var context2 = new DepartmentContext2())
            {
                context2.Database.EnsureCreated();
                
                var dept = new Department()
                {
                    Name = "test 2",
                    Location = "location 2"
                };
                context2.Departments.Add(dept);
                context2.SaveChanges(true);
            }

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].EntityFrameworkEvent.Entries.Count, Is.EqualTo(1));
            Assert.That(evs[1].EntityFrameworkEvent.Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].EntityFrameworkEvent.Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[1].EntityFrameworkEvent.Entries[0].Action, Is.EqualTo("Insert"));

            Assert.That(evs[0].EventType, Is.EqualTo("DepartmentContext1:Type1"));
            Assert.That(evs[1].EventType, Is.EqualTo("DepartmentContext2:Type2"));

            Assert.That(evs[0].EntityFrameworkEvent.Entries[0].ColumnValues["Name"] as string, Is.EqualTo("Override test 1"));
            Assert.That(evs[1].EntityFrameworkEvent.Entries[0].ColumnValues["Name"] as string, Is.EqualTo("Override 2"));
            Assert.That(evs[0].EntityFrameworkEvent.Entries[0].Entity, Is.Not.Null);
            Assert.That(evs[1].EntityFrameworkEvent.Entries[0].Entity, Is.Null);
        }
    }
}
