using System;
using System.Collections.Generic;
using Audit.EntityFramework.ConfigurationApi;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class EFConfigTests
    {
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

            Assert.AreEqual("FromAttr", ctx.AuditEventType);
            Assert.AreEqual(true, ctx.IncludeEntityObjects);
            Assert.AreEqual(true, ctx.ExcludeValidationResults);
            Assert.AreEqual(AuditOptionMode.OptIn, ctx.Mode);

            Assert.AreEqual("ForAnyContext", ctx2.AuditEventType);
            Assert.AreEqual(AuditOptionMode.OptOut, ctx2.Mode);
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
            Assert.AreEqual(2, merged.Count);
            Assert.IsNull(mustbenull1);
            Assert.IsNull(mustbenull2);
            Assert.IsNull(mustbenull3);
            var merge = merged[typeof(string)];
            Assert.AreEqual(3, merge.IgnoredProperties.Count);
            Assert.IsTrue(merge.IgnoredProperties.Contains("I1"));
            Assert.IsTrue(merge.IgnoredProperties.Contains("I2"));
            Assert.IsTrue(merge.IgnoredProperties.Contains("I3"));
            Assert.AreEqual(4, merge.OverrideProperties.Count);
            Assert.AreEqual(1, merge.OverrideProperties["C1"].Invoke(null));
            Assert.AreEqual("ATTR", merge.OverrideProperties["C2"].Invoke(null));
            Assert.AreEqual(now, merge.OverrideProperties["C3"].Invoke(null));
            Assert.AreEqual(null, merge.OverrideProperties["C4"].Invoke(null));
            merge = merged[typeof(int)];
            Assert.AreEqual(1, merge.IgnoredProperties.Count);
            Assert.IsTrue(merge.IgnoredProperties.Contains("I3"));
            Assert.AreEqual("INT", merge.OverrideProperties["C2"].Invoke(null));
            Assert.AreEqual(null, merge.OverrideProperties["C4"].Invoke(null));
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
