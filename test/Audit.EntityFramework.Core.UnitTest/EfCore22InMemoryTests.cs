using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfCore22InMemoryTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new BlogsMemoryContext().Database.EnsureCreated();
        }

        [Test]
        public void Test_EF_SaveChangesAcceptChangesOverride()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_test")
                .Options;
            var id = new Random().Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "fede",
                    Password = "142857",
                    Token = "aaabbb"
                };
                ctx.Users.Add(user);
                ctx.SaveChanges(true);
            }

            Assert.AreEqual(1, evs.Count);

        }

        [Test]
        public void Test_EF_IgnoreOverrideInheritance()
        {
            var guid = Guid.NewGuid().ToString().Substring(0, 6);
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_test")
                .Options;
            var id = new Random().Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "fede",
                    Password = "142857",
                    Token = "aaabbb"
                };
                ctx.Users.Add(user);
                Audit.Core.Configuration.AuditDisabled = true;
                ctx.SaveChanges();
                Audit.Core.Configuration.AuditDisabled = false;

                var usr = ctx.Users.First(u => u.Id == id);
                usr.Password = "1234";
                usr.Token = "xxxaaa";
                ctx.SaveChanges();

            }

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(1, evs[0].GetEntityFrameworkEvent().Entries.Count);
            var entry = evs[0].GetEntityFrameworkEvent().Entries[0];
            Assert.AreEqual(1, entry.Changes.Count);
            var changeToken = entry.Changes.First(_ => _.ColumnName == "Token");
            Assert.AreEqual("***", changeToken.OriginalValue);
            Assert.AreEqual("***", changeToken.NewValue);
            Assert.IsFalse(entry.ColumnValues.ContainsKey("Password"));
            Assert.AreEqual("***", entry.ColumnValues["Token"]);
        }
    }
}