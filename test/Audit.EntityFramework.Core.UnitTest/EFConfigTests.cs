using System;
using System.Collections.Generic;
using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
using Audit.EntityFramework.Providers;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class EFConfigTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            EntityFramework.Configuration.Setup().ForAnyContext().Reset();
        }

        [Test]
        public void Test_EntityFramework_Config_Precedence()
        {
            EntityFramework.Configuration.Setup()
                .ForContext<MyContext>(x => x.AuditEventType("ForContext"))
                .UseOptIn();
            EntityFramework.Configuration.Setup()
                .ForAnyContext(x => x.AuditEventType("ForAnyContext").IncludeEntityObjects(true).ExcludeValidationResults(true))
                .UseOptOut();

            var ctx = new MyContext();
            var ctx2 = new AnotherContext();

            Assert.That(ctx.AuditEventType, Is.EqualTo("FromAttr"));
            Assert.That(ctx.IncludeEntityObjects, Is.EqualTo(true));
            Assert.That(ctx.ExcludeValidationResults, Is.EqualTo(true));
            Assert.That(ctx.Mode, Is.EqualTo(AuditOptionMode.OptIn));

            Assert.That(ctx2.AuditEventType, Is.EqualTo("ForAnyContext"));
            Assert.That(ctx2.Mode, Is.EqualTo(AuditOptionMode.OptOut));
        }


        [Test]
        public void Test_EF_MergeEntitySettings()
        {
            var now = DateTime.Now;
            var helper = new DbContextHelper();
            var attr = new Dictionary<Type, EfEntitySettings>();
            var local = new Dictionary<Type, EfEntitySettings>();
            var global = new Dictionary<Type, EfEntitySettings>();
            attr[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I1" }),
                OverrideProperties = new Dictionary<string, Func<EntityEntry, object>>() { { "C1", _ => 1 }, { "C2", _ => "ATTR" } }
            };
            local[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I1", "I2" }),
                OverrideProperties = new Dictionary<string, Func<EntityEntry, object>>() { { "C2", _ => "LOCAL" }, { "C3", _ => now } }
            };
            global[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I3" }),
                OverrideProperties = new Dictionary<string, Func<EntityEntry, object>>() { { "C2", _ => "GLOBAL" }, { "C4", _ => null } }
            };

            attr[typeof(int)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I3" }),
                OverrideProperties = new Dictionary<string, Func<EntityEntry, object>>() { { "C2", _ => "INT" }, { "C4", _ => null } }
            };

            var merged = helper.MergeEntitySettings(attr, local, global);
            var mustbenull1 = helper.MergeEntitySettings(null, null, null);
            var mustbenull2 = helper.MergeEntitySettings(null, new Dictionary<Type, EfEntitySettings>(), null);
            var mustbenull3 = helper.MergeEntitySettings(new Dictionary<Type, EfEntitySettings>(), new Dictionary<Type, EfEntitySettings>(), new Dictionary<Type, EfEntitySettings>());
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(mustbenull1, Is.Null);
            Assert.That(mustbenull2, Is.Null);
            Assert.That(mustbenull3, Is.Null);
            var merge = merged[typeof(string)];
            Assert.That(merge.IgnoredProperties.Count, Is.EqualTo(3));
            Assert.That(merge.IgnoredProperties.Contains("I1"), Is.True);
            Assert.That(merge.IgnoredProperties.Contains("I2"), Is.True);
            Assert.That(merge.IgnoredProperties.Contains("I3"), Is.True);
            Assert.That(merge.OverrideProperties.Count, Is.EqualTo(4));
            Assert.That(merge.OverrideProperties["C1"].Invoke(null), Is.EqualTo(1));
            Assert.That(merge.OverrideProperties["C2"].Invoke(null), Is.EqualTo("ATTR"));
            Assert.That(merge.OverrideProperties["C3"].Invoke(null), Is.EqualTo(now));
            Assert.That(merge.OverrideProperties["C4"].Invoke(null), Is.EqualTo(null));
            merge = merged[typeof(int)];
            Assert.That(merge.IgnoredProperties.Count, Is.EqualTo(1));
            Assert.That(merge.IgnoredProperties.Contains("I3"), Is.True);
            Assert.That(merge.OverrideProperties["C2"].Invoke(null), Is.EqualTo("INT"));
            Assert.That(merge.OverrideProperties["C4"].Invoke(null), Is.EqualTo(null));
        }

        [Test]
        public void Test_EntityFramework_UseEntityFramework()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework((t, ee) => typeof(AuditEvent), (e, ee, o) => { }, t => true,
                    ee => typeof(AuditEvent),
                    (context, entry) => new object());

            var dataProvider = Audit.Core.Configuration.DataProviderAs<EntityFrameworkDataProvider>();
            Assert.That(dataProvider, Is.Not.Null);
        }
    }

    [AuditDbContext(AuditEventType = "FromAttr")]
    public class MyContext : AuditDbContext
    {
    }
    public class AnotherContext : AuditDbContext
    {
    }
}
