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
            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public async Task Test_EF_CustomFields_OnScopeCreated()
        {
            var evs = new List<AuditEventEntityFramework>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        // Do nothing, will include the events on OnScopeCreated   
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnScopeCreated(scope =>
                {
                    evs.Add(AuditEvent.FromJson<AuditEventEntityFramework>(scope.Event.ToJson()));
                }));

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
                    Password = "12345",
                    Token = "Test123"
                };
                ctx.Users.Add(user);
                ctx.AddAuditCustomField("TestCustomField", 1L);
                await ctx.SaveChangesAsync();
                ctx.Users.Remove(user);
                ctx.AddAuditCustomField("TestCustomField", 2L);
                await ctx.SaveChangesAsync();
            }

            Assert.AreEqual(2, evs.Count);
            Assert.IsTrue(evs[0].CustomFields.ContainsKey("TestCustomField"));
            Assert.AreEqual("1", evs[0].CustomFields["TestCustomField"].ToString());
            Assert.AreEqual("2", evs[1].CustomFields["TestCustomField"].ToString());
        }

        [Test]
        public void Test_EF_Provider_ExplicitMapper_MapExplicit()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false)
                    .ExcludeTransactionId(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .AuditTypeExplicitMapper(config => config
                        .MapExplicit<UserAudit>(ee => ee.Table == "User", (ee, userAudit) =>
                        {
                            userAudit.AuditId = 1;
                            userAudit.UserId = (int)ee.ColumnValues["Id"];
                            userAudit.AuditUser = "us";
                        })
                        .Map((User user, UserAudit userAudit) =>
                        {
                            // Should never get here, since the user table is handled explicitly
                            userAudit.Action = "Invalid";
                        })
                        .AuditEntityAction((ee, ent, obj) =>
                        {
                            ((UserAudit)obj).Action = ent.Action;
                            ((UserAudit)obj).AuditUser += "er";
                        }))
                    .IgnoreMatchedProperties(false));

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_user_test")
                .Options;
            var id = Rnd.Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();

                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "123",
                    Token = "token"
                };
                ctx.Users.Add(user);
                ctx.SaveChanges();
            }

            // Assert
            using (var ctx = new BlogsMemoryContext(options))
            {
                var audit = ctx.UserAudits.Single(u => u.UserId == id);
                Assert.AreEqual(1, audit.AuditId);
                Assert.AreEqual("Insert", audit.Action);
                Assert.AreEqual("user", audit.AuditUser);
                Assert.AreEqual("test", audit.Name);
                Assert.AreEqual(id, audit.UserId);
            }
        }

private bool IsCommonEntity(Type type)
{
    return type == typeof(OrderMemoryContext.Order) || type == typeof(OrderMemoryContext.Orderline);
}

        [Test]
        public async Task Test_MapExplicitAndMultipleActions()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .AuditTypeExplicitMapper(config => config
                        .Map<OrderMemoryContext.Order, OrderMemoryContext.OrderAudit>()
                        .Map<OrderMemoryContext.Orderline, OrderMemoryContext.OrderlineAudit>()
                        .MapExplicit<OrderMemoryContext.AuditLog>(ee => !IsCommonEntity(ee.GetEntry().Entity.GetType()), (entry, auditLog) =>
                        {
                            auditLog.AuditData = entry.ToJson();
                            auditLog.EntityType = entry.EntityType.Name;
                            auditLog.AuditDate = DateTime.Now;
                            auditLog.AuditUser = "test user";
                            auditLog.TablePk = entry.PrimaryKey.First().Value.ToString();
                        })
                        .AuditEntityAction((evt, entry, entity) =>
                        {
                            if (entity is OrderMemoryContext.IAudit auditEntity)
                            {
                                auditEntity.AuditDate = DateTime.UtcNow;
                                auditEntity.UserName = "test user";
                                auditEntity.AuditAction = entry.Action;
                            }                
                        }))
                    .IgnoreMatchedProperties(type => type == typeof(OrderMemoryContext.AuditLog)));
            var id = Rnd.Next();

            var options = new DbContextOptionsBuilder<OrderMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_orders")
                .Options;
            using (var ctx = new OrderMemoryContext(options))
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();

                ctx.Add(new OrderMemoryContext.Order()
                {
                    Id = id,
                    Name = id.ToString()
                });
                await ctx.SaveChangesAsync();

                ctx.Add(new OrderMemoryContext.Product()
                {
                    Id = (id + 1),
                    Name = (id + 1).ToString()
                });
                await ctx.SaveChangesAsync();

            }

            using (var ctx = new OrderMemoryContext(options))
            {
                var auditLogs = ctx.AuditLogs.Where(l => l.TablePk == (id + 1).ToString()).ToList();
                var orderAudits = ctx.OrderAudits.Where(l => l.Id == id).ToList();

                Assert.AreEqual(1, auditLogs.Count);
                Assert.AreEqual(1, orderAudits.Count);
                Assert.AreEqual("test user", orderAudits[0].UserName);
                Assert.AreEqual("test user", auditLogs[0].AuditUser);
            }

        }

        [Test]
        public async Task Test_EF_Provider_ExplicitMapper_MapExplicit_Async()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false)
                    .ExcludeTransactionId(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .AuditTypeExplicitMapper(config => config
                        .MapExplicit<UserAudit>(ee => ee.Table == "User", (ee, userAudit) =>
                        {
                            userAudit.AuditId = 1;
                            userAudit.UserId = (int)ee.ColumnValues["Id"];
                            userAudit.AuditUser = "us";
                        })
                        .Map((User user, UserAudit userAudit) =>
                        {
                            // Should never get here, since the user table is handled explicitly
                            userAudit.Action = "Invalid";
                        })
                        .AuditEntityAction((ee, ent, obj) =>
                        {
                            ((UserAudit)obj).Action = ent.Action;
                            ((UserAudit)obj).AuditUser += "er";
                        }))
                    .IgnoreMatchedProperties(false));

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_user_test")
                .Options;
            var id = Rnd.Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();

                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "123",
                    Token = "token"
                };
                ctx.Users.Add(user);
                await ctx.SaveChangesAsync();
            }

            // Assert
            using (var ctx = new BlogsMemoryContext(options))
            {
                var audit = ctx.UserAudits.Single(u => u.UserId == id);
                Assert.AreEqual(1, audit.AuditId);
                Assert.AreEqual("Insert", audit.Action);
                Assert.AreEqual("user", audit.AuditUser);
                Assert.AreEqual("test", audit.Name);
                Assert.AreEqual(id, audit.UserId);
            }
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
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .ResetActions();

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