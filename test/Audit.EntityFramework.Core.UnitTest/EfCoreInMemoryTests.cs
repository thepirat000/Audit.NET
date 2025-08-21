﻿using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Audit.Core.Providers;
using Audit.IntegrationTest;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
    public class EfCoreInMemoryTests
    {
        private static readonly Random Rnd = new Random();

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new BlogsMemoryContext().Database.EnsureCreated();
            new BlogsContext().Database.EnsureCreated();
            Audit.Core.Configuration.ResetCustomActions();
        }

#if EF_CORE_7_OR_GREATER
        [Test]
        public void Test_EF_OwnedEntity_ToJson()
        {
            using var context = new Context_OwnedEntity_ToJson();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(ev.GetEntityFrameworkEvent());
                }));

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.People.Add(new Context_OwnedEntity_ToJson.Person()
            {
                Id = 1,
                Name = "Development",
                Address = new Context_OwnedEntity_ToJson.Address { City = "Vienna", Street = "Street" },
            });

            context.SaveChanges();

            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(evs[0].Entries.Count, Is.EqualTo(2));

            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[0].Entries[1].Action, Is.EqualTo("Insert"));

            Assert.That(evs[0].Entries[0].ColumnValues["Id"], Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].ColumnValues["Name"], Is.EqualTo("Development"));

            Assert.That(evs[0].Entries[1].ColumnValues["City"], Is.EqualTo("Vienna"));
            Assert.That(evs[0].Entries[1].ColumnValues["Street"], Is.EqualTo("Street"));

            Assert.AreEqual(1, ((dynamic)evs[0].Entries[0].Entity).Id);
            Assert.AreEqual("Vienna", ((dynamic)evs[0].Entries[0].Entity).Address.City);
        }

        [Test]
        public async Task Test_EF_OwnedEntity_ToJson_Async()
        {
            using var context = new Context_OwnedEntity_ToJson();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(ev.GetEntityFrameworkEvent());
                }));

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            await context.People.AddAsync(new Context_OwnedEntity_ToJson.Person()
            {
                Id = 1,
                Name = "Development",
                Address = new Context_OwnedEntity_ToJson.Address { City = "Vienna", Street = "Street" },
            });

            await context.SaveChangesAsync();

            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(evs[0].Entries.Count, Is.EqualTo(2));

            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[0].Entries[1].Action, Is.EqualTo("Insert"));

            Assert.That(evs[0].Entries[0].ColumnValues["Id"], Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].ColumnValues["Name"], Is.EqualTo("Development"));

            Assert.That(evs[0].Entries[1].ColumnValues["City"], Is.EqualTo("Vienna"));
            Assert.That(evs[0].Entries[1].ColumnValues["Street"], Is.EqualTo("Street"));

            Assert.AreEqual(1, ((dynamic)evs[0].Entries[0].Entity).Id);
            Assert.AreEqual("Vienna", ((dynamic)evs[0].Entries[0].Entity).Address.City);
        }
#endif

        [Test]
        public void Test_EF_SaveChangesGetAudit()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseNullProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_test")
                .Options;
            var id = Rnd.Next();
            EntityFrameworkEvent efEventInsert;
            EntityFrameworkEvent efEventRemove;
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "1",
                    Token = "2"
                };
                ctx.Users.Add(user);
                efEventInsert = ctx.SaveChangesGetAudit();

                ctx.Users.Remove(user);
                efEventRemove = ctx.SaveChangesGetAudit();
            }

            Assert.That(efEventInsert, Is.Not.Null);
            Assert.That(efEventRemove, Is.Not.Null);
            Assert.That(efEventInsert.Result, Is.EqualTo(1));
            Assert.That(efEventInsert.Entries.Count, Is.EqualTo(1));
            Assert.That(efEventInsert.Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(efEventInsert.Entries[0].Table, Is.EqualTo("User"));
            Assert.That(efEventInsert.Entries[0].PrimaryKey.First().Value.ToString(), Is.EqualTo(id.ToString()));

            Assert.That(efEventRemove.Result, Is.EqualTo(1));
            Assert.That(efEventRemove.Entries.Count, Is.EqualTo(1));
            Assert.That(efEventRemove.Entries[0].Action, Is.EqualTo("Delete"));
            Assert.That(efEventRemove.Entries[0].Table, Is.EqualTo("User"));
            Assert.That(efEventRemove.Entries[0].PrimaryKey.First().Value.ToString(), Is.EqualTo(id.ToString()));
        }

        [Test]
        public async Task Test_EF_SaveChangesGetAudit_Async()
        {
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseNullProvider()
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_test")
                .Options;
            var id = Rnd.Next();
            EntityFrameworkEvent efEventInsert;
            EntityFrameworkEvent efEventRemove;
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "1",
                    Token = "2"
                };
                ctx.Users.Add(user);
                efEventInsert = await ctx.SaveChangesGetAuditAsync();

                ctx.Users.Remove(user);
                efEventRemove = await ctx.SaveChangesGetAuditAsync();
            }

            Assert.That(efEventInsert, Is.Not.Null);
            Assert.That(efEventRemove, Is.Not.Null);
            Assert.That(efEventInsert.Result, Is.EqualTo(1));
            Assert.That(efEventInsert.Entries.Count, Is.EqualTo(1));
            Assert.That(efEventInsert.Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(efEventInsert.Entries[0].Table, Is.EqualTo("User"));
            Assert.That(efEventInsert.Entries[0].PrimaryKey.First().Value.ToString(), Is.EqualTo(id.ToString()));
            Assert.That(efEventRemove.Result, Is.EqualTo(1));
            Assert.That(efEventRemove.Entries.Count, Is.EqualTo(1));
            Assert.That(efEventRemove.Entries[0].Action, Is.EqualTo("Delete"));
            Assert.That(efEventRemove.Entries[0].Table, Is.EqualTo("User"));
            Assert.That(efEventRemove.Entries[0].PrimaryKey.First().Value.ToString(), Is.EqualTo(id.ToString()));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_EF_DataProvider_DbContextDisposal(bool dispose)
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false))
                .UseOptIn()
                .Include<Blog>();

            var ctx1 = new BlogsContext();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .UseDbContext(_ => ctx1)
                    .DisposeDbContext(dispose)
                    .AuditTypeMapper(_ => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, ent, au) =>
                    {
                        au.Title = ent.ColumnValues["BloggerName"].ToString();
                        au.AuditDate = DateTime.Now;
                    }));

            var blog = new Blog()
            {
                BloggerName = Guid.NewGuid().ToString(),
                Title = "test"
            };
            CommonAudit audit;
            using (var ctx2 = new BlogsContext())
            {
                ctx2.Blogs.Add(blog);
                ctx2.SaveChanges();

                audit = ctx2.CommonAudits.FirstOrDefault(x => x.Title == blog.BloggerName);
            }

            var disposedField = typeof(DbContext).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Title, Is.EqualTo(blog.BloggerName));
            Assert.That((bool?)disposedField?.GetValue(ctx1), Is.EqualTo(dispose));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_EF_DataProvider_DbContextDisposalAsync(bool dispose)
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false))
                .UseOptIn()
                .Include<Blog>();

            var ctx1 = new BlogsContext();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .UseDbContext(_ => ctx1)
                    .DisposeDbContext(dispose)
                    .AuditTypeMapper(_ => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, ent, au) =>
                    {
                        au.Title = ent.ColumnValues["BloggerName"].ToString();
                        au.AuditDate = DateTime.Now;
                    }));

            var blog = new Blog()
            {
                BloggerName = Guid.NewGuid().ToString(),
                Title = "test"
            }; 
            CommonAudit audit;
            using (var ctx2 = new BlogsContext())
            {
                await ctx2.Blogs.AddAsync(blog);
                await ctx2.SaveChangesAsync();

                audit = await ctx2.CommonAudits.FirstOrDefaultAsync(x => x.Title == blog.BloggerName);
            }

            var disposedField = typeof(DbContext).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Title, Is.EqualTo(blog.BloggerName));
            Assert.That((bool?)disposedField?.GetValue(ctx1), Is.EqualTo(dispose));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Test_EF_DataProvider_DbContextShouldDisposeEvenIfExceptionThrown(bool dispose)
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false))
                .UseOptIn()
                .Include<Blog>();

            var ctx1 = new BlogsContext();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .UseDbContext(_ => ctx1)
                    .DisposeDbContext(dispose)
                    .AuditTypeMapper(_ => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, ent, au) =>
                    {
                        au.AuditAction = "";
                        throw new ArgumentException("test exception");
#pragma warning disable CS0162
                        return null;
#pragma warning restore CS0162
                    }));

            var blog = new Blog()
            {
                BloggerName = Guid.NewGuid().ToString(),
                Title = "test"
            };
            
            using (var ctx2 = new BlogsContext())
            {
                ctx2.Blogs.AddAsync(blog);

                var exception = Assert.Throws<AggregateException>(() => ctx2.SaveChanges());
                Assert.That(exception.GetBaseException().Message, Is.EqualTo("test exception"));
            }

            var disposedField = typeof(DbContext).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That((bool?)disposedField?.GetValue(ctx1), Is.EqualTo(dispose));
        }


        [TestCase(true)]
        [TestCase(false)]
        public async Task Test_EF_DataProvider_DbContextShouldDisposeEvenIfExceptionThrownAsync(bool dispose)
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x
                    .IncludeEntityObjects(false))
                .UseOptIn()
                .Include<Blog>();

            var ctx1 = new BlogsContext();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseEntityFramework(ef => ef
                    .UseDbContext(_ => ctx1)
                    .DisposeDbContext(dispose)
                    .AuditTypeMapper(_ => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, ent, au) =>
                    {
                        au.AuditAction = "";
                        throw new ArgumentException("test exception");
#pragma warning disable CS0162
                        return null;
#pragma warning restore CS0162
                    }));

            var blog = new Blog()
            {
                BloggerName = Guid.NewGuid().ToString(),
                Title = "test"
            };

            using (var ctx2 = new BlogsContext())
            {
                await ctx2.Blogs.AddAsync(blog);

                var exception = Assert.ThrowsAsync<ArgumentException>(async () => await ctx2.SaveChangesAsync());
                Assert.That(exception.GetBaseException().Message, Is.EqualTo("test exception"));
            }

            var disposedField = typeof(DbContext).GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That((bool?)disposedField?.GetValue(ctx1), Is.EqualTo(dispose));
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

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].CustomFields.ContainsKey("TestCustomField"), Is.True);
            Assert.That(evs[0].CustomFields["TestCustomField"].ToString(), Is.EqualTo("1"));
            Assert.That(evs[1].CustomFields["TestCustomField"].ToString(), Is.EqualTo("2"));
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
                Assert.That(audit.AuditId, Is.EqualTo(1));
                Assert.That(audit.Action, Is.EqualTo("Insert"));
                Assert.That(audit.AuditUser, Is.EqualTo("user"));
                Assert.That(audit.Name, Is.EqualTo("test"));
                Assert.That(audit.UserId, Is.EqualTo(id));
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

                Assert.That(auditLogs.Count, Is.EqualTo(1));
                Assert.That(orderAudits.Count, Is.EqualTo(1));
                Assert.That(orderAudits[0].UserName, Is.EqualTo("test user"));
                Assert.That(auditLogs[0].AuditUser, Is.EqualTo("test user"));
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

                Assert.That(audit.AuditId, Is.EqualTo(1));
                Assert.That(audit.Action, Is.EqualTo("Insert"));
                Assert.That(audit.AuditUser, Is.EqualTo("user"));
                Assert.That(audit.Name, Is.EqualTo("test"));
                Assert.That(audit.UserId, Is.EqualTo(id));
            }
        }


#if EF_CORE_5_OR_GREATER

        [Test]
        public void Test_ChangeTrackingProxyContext()
        {
            var evs = new List<EntityFrameworkEvent>();
            var guid = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .UseDbContext<ChangeTrackingProxyContext>()
                    .AuditEntityCreator((ctx, ent) => ctx.CreateProxy<ChangeTrackingProxyContext.AuditLog>())
                    .AuditEntityAction<ChangeTrackingProxyContext.AuditLog>((ev, ent, auditEntity) => 
                    { 
                        auditEntity.DateTime = DateTime.Now;
                        auditEntity.Action = ent.Action;
                        auditEntity.Table = ent.Table;
                    })
                    .IgnoreMatchedProperties(t => t != typeof(ChangeTrackingProxyContext.AuditLog)));

            using var context = new ChangeTrackingProxyContext();

            context.Customers.Add(context.CreateProxy<ChangeTrackingProxyContext.Customer>(test => test.CustomerName = guid));
            context.SaveChanges();

            var log = context.AuditLogs.FirstOrDefault(a => a.CustomerName == guid);
            Assert.That(log, Is.Not.Null);
            Assert.That(log.CustomerName, Is.EqualTo(guid));
            Assert.That(log.Action, Is.EqualTo("Insert"));
            Assert.That(log.Table, Is.EqualTo("Customer"));
        }

        [Test]
        public async Task Test_ChangeTrackingProxyContext_Async()
        {
            var evs = new List<EntityFrameworkEvent>();
            var guid = Guid.NewGuid().ToString();
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .UseDbContext<ChangeTrackingProxyContext>()
                    .AuditEntityCreator(ctx => ctx.CreateProxy<ChangeTrackingProxyContext.AuditLog>())
                    .AuditEntityAction<ChangeTrackingProxyContext.AuditLog>(async (ev, ent, auditEntity) =>
                    {
                        auditEntity.DateTime = DateTime.Now;
                        auditEntity.Action = ent.Action;
                        auditEntity.Table = ent.Table;
                        await Task.Delay(0);
                    })
                    .IgnoreMatchedProperties(t => t != typeof(ChangeTrackingProxyContext.AuditLog)));

            using var context = new ChangeTrackingProxyContext();

            await context.Customers.AddAsync(context.CreateProxy<ChangeTrackingProxyContext.Customer>(test => test.CustomerName = guid));
            await context.SaveChangesAsync();

            var log = await context.AuditLogs.FirstOrDefaultAsync(a => a.CustomerName == guid);
            Assert.That(log, Is.Not.Null);
            Assert.That(log.CustomerName, Is.EqualTo(guid));
            Assert.That(log.Action, Is.EqualTo("Insert"));
            Assert.That(log.Table, Is.EqualTo("Customer"));
        }

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

            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(evs[0].Entries.Count, Is.EqualTo(2));

            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[0].Entries[1].Action, Is.EqualTo("Insert"));

            Assert.That(evs[0].Entries[0].ColumnValues["Id"], Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].ColumnValues["Name"], Is.EqualTo("Development"));

            Assert.That(evs[0].Entries[1].ColumnValues["Address_City"], Is.EqualTo("Vienna"));
            Assert.That(evs[0].Entries[1].ColumnValues["Address_Street"], Is.EqualTo("Street"));
            
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

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[1].Entries.Count, Is.EqualTo(6)); // 2 inserts to Person + 4 inserts to PersonDepartment
            Assert.That(evs[1].Entries.All(e => e.Action == "Insert"), Is.True);
            Assert.That(evs[1].Entries.Count(e => e.Table == "Persons"), Is.EqualTo(2));
            Assert.That(evs[1].Entries.Count(e => e.Table == "DepartmentPerson"), Is.EqualTo(4));
            Assert.That(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.ColumnValues.ContainsKey("DepartmentsId")), Is.True);
            Assert.That(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.ColumnValues.ContainsKey("PersonsId")), Is.True);
            Assert.That(evs[1].Entries.Where(e => e.Table == "DepartmentPerson").All(dpe => dpe.PrimaryKey.Count == 2), Is.True);
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

            Assert.That(evs.Count, Is.EqualTo(2));
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

            Assert.That(evs.Count, Is.EqualTo(2));
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

            Assert.That(evs.Count, Is.EqualTo(2));
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

            Assert.That(evs.Count, Is.EqualTo(2));
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

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries.Count, Is.EqualTo(1));
            var entry = evs[0].GetEntityFrameworkEvent().Entries[0];
            Assert.That(entry.Changes.Count, Is.EqualTo(1));
            var changeToken = entry.Changes.First(_ => _.ColumnName == "Token");
            Assert.That(changeToken.OriginalValue, Is.EqualTo("***"));
            Assert.That(changeToken.NewValue, Is.EqualTo("***"));
            Assert.IsFalse(entry.ColumnValues.ContainsKey("Password"));
            Assert.That(entry.ColumnValues["Token"], Is.EqualTo("***"));
        }

        [Test]
        public async Task Test_EF_Provider_Override_Is_Respected()
        {
            var overrideValue = "******";

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsMemoryContext>(config =>
                {
                    config.ForEntity<User>(x =>
                    {
                        x.Override(u => u.Name, overrideValue);
                    });
                });

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_override_test")
                .Options;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .UseDbContext<BlogsMemoryContext>(options)
                    .AuditTypeNameMapper(name => $"{name}Audit")
                    .AuditEntityAction<UserAudit>((ev, entry, audit) =>
                    {
                        audit.UserId = ((dynamic)entry.GetEntry().Entity).Id;
                        audit.Action = entry.Action;
                    }));

            var id = Rnd.Next();
            UserAudit userAudit = null;
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "123",
                    Token = "token"
                };
                await ctx.Users.AddAsync(user);
                await ctx.SaveChangesAsync();

                userAudit = ctx.UserAudits.FirstOrDefault(x => x.UserId == id);
            }

            Assert.That(userAudit, Is.Not.Null);
            Assert.That(userAudit.Name, Is.EqualTo(overrideValue));
        }

        [Test]
        public async Task Test_EF_Provider_Override_Is_Respected_With_Custom_ColumnName()
        {
            var overrideValue = "Name2_Override";

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsMemoryContext>(config =>
                {
                    config.ForEntity<User>(x =>
                    {
                        x.Override(u => u.Name2, overrideValue);
                    });
                });

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_override_test")
                .Options;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .UseDbContext<BlogsMemoryContext>(options)
                    .AuditTypeNameMapper(name => $"{name}Audit")
                    .AuditEntityAction<UserAudit>((ev, entry, audit) =>
                    {
                        audit.UserId = ((dynamic)entry.GetEntry().Entity).Id;
                        audit.Action = entry.Action;
                    }));

            var id = Rnd.Next();
            UserAudit userAudit = null;
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Name2 = "test2",
                    Password = "123",
                    Token = "token"
                };
                await ctx.Users.AddAsync(user);
                await ctx.SaveChangesAsync();

                userAudit = ctx.UserAudits.FirstOrDefault(x => x.UserId == id);
            }

            Assert.That(userAudit, Is.Not.Null);
            Assert.That(userAudit.Name2, Is.EqualTo(overrideValue));
        }

        [Test]
        public async Task Test_EF_Provider_Override_Is_Respected_With_Custom_ColumnName_And_Factory()
        {
            var overrideValue = "Name2_Override";

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsMemoryContext>(config =>
                {
                    config.ForEntity<User>(x =>
                    {
                        x.Override(u => u.Name2, overrideValue);
                    });
                });

            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_override_test")
                .Options;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .UseDbContext<BlogsMemoryContext>(options)
                    .AuditEntityCreator(_ => new UserAudit())
                    .AuditEntityAction<UserAudit>((ev, entry, audit) =>
                    {
                        audit.UserId = ((dynamic)entry.GetEntry().Entity).Id;
                        audit.Action = entry.Action;
                    }));

            var id = Rnd.Next();
            UserAudit userAudit = null;
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Name2 = "test2",
                    Password = "123",
                    Token = "token"
                };
                await ctx.Users.AddAsync(user);
                await ctx.SaveChangesAsync();

                userAudit = ctx.UserAudits.FirstOrDefault(x => x.UserId == id);
            }

            Assert.That(userAudit, Is.Not.Null);
            Assert.That(userAudit.Name2, Is.EqualTo(overrideValue));
        }

        [Test]
        public void Test_EF_Provider_EntityType_With_MapExplicit()
        {
            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_map_explicit_test")
                .Options;

            Type entityType1 = null;
            Type entityType2 = null;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .UseDbContext<BlogsMemoryContext>(options)
                    .AuditTypeExplicitMapper(map => map
                        .MapExplicit<UserAudit>(entry =>
                        {
                            entityType1 = entry.EntityType;
                            return true;
                        }, (entry, audit) =>
                        {
                            entityType2 = entry.EntityType;
                        })));

            var id = Rnd.Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
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

            Assert.That(entityType1, Is.Not.Null);
            Assert.That(entityType1, Is.EqualTo(typeof(User)));
            Assert.That(entityType2, Is.Not.Null);
            Assert.That(entityType2, Is.EqualTo(typeof(User)));
        }

        [Test]
        public async Task Test_EF_Provider_EntityType_With_MapExplicitAsync()
        {
            var options = new DbContextOptionsBuilder<BlogsMemoryContext>()
                .UseInMemoryDatabase(databaseName: "database_map_explicit_test")
                .Options;

            Type entityType1 = null;
            Type entityType2 = null;

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .UseDbContext<BlogsMemoryContext>(options)
                    .AuditTypeExplicitMapper(map => map
                        .MapExplicit<UserAudit>(entry =>
                        {
                            entityType1 = entry.EntityType;
                            return true;
                        }, (entry, audit) =>
                        {
                            entityType2 = entry.EntityType;
                        })));

            var id = Rnd.Next();
            using (var ctx = new BlogsMemoryContext(options))
            {
                var user = new User()
                {
                    Id = id,
                    Name = "test",
                    Password = "123",
                    Token = "token"
                };
                await ctx.Users.AddAsync(user);
                await ctx.SaveChangesAsync();
            }

            Assert.That(entityType1, Is.Not.Null);
            Assert.That(entityType1, Is.EqualTo(typeof(User)));
            Assert.That(entityType2, Is.Not.Null);
            Assert.That(entityType2, Is.EqualTo(typeof(User)));
        }
    }
}