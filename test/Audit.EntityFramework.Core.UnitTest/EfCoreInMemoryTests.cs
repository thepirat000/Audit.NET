using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfCoreInMemoryTests
    {
        private static readonly Random Rnd = new Random();

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new BlogsMemoryContext().Database.EnsureCreated();
        }

#if EF_CORE_5

        [Test]
        public void Test_OwnedEntity_EFCore5()
        {
            using var context = new Context_OwnedEntity();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(ev.GetEntityFrameworkEvent());
                }));

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Departments.Add(new Context_OwnedEntity.Department()
            {
                Id = 1,
                Name = "Development",
                Address = new Context_OwnedEntity.Address { City = "Vienna", Street = "Street" },
            });

            context.SaveChanges();

            Assert.AreEqual(1, evs.Count);

            Assert.AreEqual(2, evs[0].Entries.Count);
            
            Assert.AreEqual("Insert", evs[0].Entries[0].Action);
            Assert.AreEqual("Insert", evs[0].Entries[1].Action);

            Assert.AreEqual(1, evs[0].Entries[0].ColumnValues["Id"]);
            Assert.AreEqual("Development", evs[0].Entries[0].ColumnValues["Name"]);

            Assert.AreEqual("Vienna", evs[0].Entries[1].ColumnValues["Address_City"]);
            Assert.AreEqual("Street", evs[0].Entries[1].ColumnValues["Address_Street"]);
            
            Assert.AreEqual(1, ((dynamic)evs[0].Entries[0].Entity).Id);
            Assert.AreEqual("Vienna", ((dynamic)evs[0].Entries[0].Entity).Address.City);
        }

        [Test]
        public void Test_ManyToMany_EFCore5()
        {
            using var context = new Context_ManyToMany();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(ev.GetEntityFrameworkEvent());
                }));

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Departments.Add(new Context_ManyToMany.Department() { Id = 1, Name = "Development" });
            context.Departments.Add(new Context_ManyToMany.Department() { Id = 2, Name = "Research" });

            context.SaveChanges();

            context.Persons.Add(new Context_ManyToMany.Person() { Id = 1, Name = "Alice", Departments = context.Departments.ToList() });
            context.Persons.Add(new Context_ManyToMany.Person() { Id = 2, Name = "Bob", Departments = context.Departments.ToList() });

            context.SaveChanges();

            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual(6, evs[1].Entries.Count); // 2 inserts to Person + 4 inserts to PersonDepartment
            Assert.IsTrue(evs[1].Entries.All(e => e.Action == "Insert"));
            Assert.AreEqual(2, evs[1].Entries.Count(e => e.Table == "Persons"));
            Assert.AreEqual(4, evs[1].Entries.Count(e => e.Table == "DepartmentPerson"));
            Assert.IsTrue(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.ColumnValues.ContainsKey("DepartmentsId")));
            Assert.IsTrue(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.ColumnValues.ContainsKey("PersonsId")));
            Assert.IsTrue(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.PrimaryKey.Count == 2));
        }
#endif

        [Test]
        public async Task Test_EF_SaveChangesAsyncOverride()
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
            var id = Rnd.Next();
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
                await ctx.SaveChangesAsync();

                ctx.Users.Remove(user);
                await ctx.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
        }

        [Test]
        public async Task Test_EF_SaveChangesAsyncAcceptChangesOverride()
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
            var id = Rnd.Next();
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
                await ctx.SaveChangesAsync(true);

                ctx.Users.Remove(user);
                await ctx.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
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
            var id = Rnd.Next();
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

                ctx.Users.Remove(user);
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);
        }

        [Test]
        public void Test_EF_SaveChangesOverride()
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
            var id = Rnd.Next();
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
                ctx.SaveChanges();

                ctx.Users.Remove(user);
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);
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
            var id = Rnd.Next();
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

                ctx.Users.Remove(user);
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);
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