using Audit.Core;
using Audit.EntityFramework.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using static System.Linq.Queryable;
using static System.Linq.Enumerable; // You only need this if you're using LINQ to Objects
using System.Threading;

namespace Audit.EntityFramework.Core.UnitTest.Context
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
    public class EntityFrameworkTests_Core
    {
        [OneTimeSetUp]
        public void Init()
        {
            var sql =
                @"drop table posts; drop table blogs; create table blogs ( Id int identity(1,1) not null primary key, BloggerName nvarchar(max), Title nvarchar(max) );
                        create table posts ( Id int identity(1,1) not null primary key, Title nvarchar(max), DateCreated datetime, Content nvarchar(max), BlogId int not null constraint FK_P_B foreign key references Blogs (id) );";
            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();

                ctx.Database.ExecuteSqlRaw(sql);
            }

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Database.EnsureCreated();

                if (!ctx.Blogs.Any())
                {
                    ctx.Blogs.Add(new Blog()
                    {
                        BloggerName = "Blogger 1",
                        Title = "Blog 1",
                        Posts = new List<Post>()
                        {
                            new Post() { Title = "Post 1", Content = "Content 1", DateCreated = DateTime.UtcNow },
                            new Post() { Title = "Post 2", Content = "Content 2", DateCreated = DateTime.UtcNow }
                        }
                    });
                    ctx.SaveChanges();
                }
            }
            
            using (var ctx = new MyUnauditedContext())
            {
                ctx.Database.EnsureCreated();

                if (!ctx.Blogs.Any())
                {
                    ctx.Blogs.Add(new Blog()
                    {
                        BloggerName = "Blogger 1",
                        Title = "Blog 1",
                        Posts = new List<Post>()
                        {
                            new Post() { Title = "Post 1", Content = "Content 1", DateCreated = DateTime.UtcNow },
                            new Post() { Title = "Post 2", Content = "Content 2", DateCreated = DateTime.UtcNow }
                        }
                    });
                    ctx.SaveChanges();
                }
            }

            using (var ctx = new MyAuditedContext())
            {
                ctx.Database.EnsureCreated();

                if (!ctx.Blogs.Any())
                {
                    ctx.Blogs.Add(new Blog()
                    {
                        BloggerName = "Blogger 1",
                        Title = "Blog 1",
                        Posts = new List<Post>()
                        {
                            new Post() { Title = "Post 1", Content = "Content 1", DateCreated = DateTime.UtcNow },
                            new Post() { Title = "Post 2", Content = "Content 2", DateCreated = DateTime.UtcNow }
                        }
                    });
                    ctx.SaveChanges();
                }
            }

            using (var ctx = new AuditPerTableContext())
            {
                ctx.Database.EnsureCreated();
            }
        }

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
                
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EF_ProxiedLazyLoading()
        {
            var list = new List<AuditEventEntityFramework>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
                {
                    list.Add(ev as AuditEventEntityFramework);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>(config => config
                    .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyBaseContext>(config => config
                    .ForEntity<Blog>(_ => _.Override("Title", null)));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new MyTransactionalContext())
            {
                var blog = ctx.Blogs.FirstOrDefault();
                blog.Title = title;
                ctx.SaveChanges();
            }

            Assert.That(list.Count, Is.EqualTo(1));
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.That(entries[0].GetEntry().Entity.GetType().FullName.StartsWith("Castle.Proxies."), Is.True);
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Update"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo(title));
        }

        [Test]
        public void Test_EFDataProvider_IdentityContext_Error()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config =>
                {
                    config
                        .AuditTypeMapper(typeName => Type.GetType(typeName + "Audit"))
                        .AuditEntityAction((ev, ent, audEnt) =>
                        {
                            ((dynamic)audEnt).Username = "test";
                        });
                });
            using (var db = new AuditNetTestContext())
            {
                db.Database.EnsureCreated();
                db.Foos.Add(new Foo());
                Assert.Throws<DbUpdateException>(() => {
                    db.SaveChanges();
                });
            }
        }

        [Test]
        public async Task Test_EFDataProvider_IdentityContext_Error_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config =>
                {
                    config
                        .AuditTypeMapper(typeName => Type.GetType(typeName + "Audit"))
                        .AuditEntityAction((ev, ent, audEnt) =>
                        {
                            ((dynamic)audEnt).Username = "test";
                        });
                });

            await using (var db = new AuditNetTestContext())
            {
                await db.Database.EnsureCreatedAsync();
                db.Foos.Add(new Foo());
                Assert.ThrowsAsync<DbUpdateException>(async () => {
                    await db.SaveChangesAsync();
                });
            }
        }

        [Test]
        public void Test_EFDataProvider_IdentityContext_SaveChanges()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider(out var dp);

            using (var db = new AuditNetTestContext())
            {
                db.Database.EnsureCreated();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                db.SaveChanges();
            }

            var auditEvents = dp.GetAllEvents();

            Assert.That(auditEvents, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Test_EFDataProvider_IdentityContext_SaveChanges_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider(out var dp);

            using (var db = new AuditNetTestContext())
            {
                await db.Database.EnsureCreatedAsync();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                await db.SaveChangesAsync();
            }

            var auditEvents = dp.GetAllEvents();

            Assert.That(auditEvents, Has.Count.EqualTo(1));
        }

        [Test]
        public void Test_IdentityContext_SaveChanges_Overload()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider(out var dp);

            using (var db = new AuditNetTestContext())
            {
                db.Database.EnsureCreated();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                db.AddAuditCustomField("Field", "Value");
                db.SaveChanges(acceptAllChangesOnSuccess: true);
            }

            var auditEvents = dp.GetAllEvents();

            Assert.That(auditEvents, Has.Count.EqualTo(1));
            Assert.That(auditEvents[0].CustomFields["Field"].ToString(), Is.EqualTo("Value"));
        }

        [Test]
        public async Task Test_IdentityContext_SaveChanges_Overload_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider(out var dp);

            await using (var db = new AuditNetTestContext(new DbContextOptionsBuilder<AuditNetTestContext>().UseSqlServer(AuditNetTestContext.CnnString).Options))
            {
                await db.Database.EnsureCreatedAsync();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                await db.SaveChangesAsync(acceptAllChangesOnSuccess: true);
            }

            var auditEvents = dp.GetAllEvents();

            Assert.That(auditEvents, Has.Count.EqualTo(1));
        }

        [Test]
        public void Test_IdentityContext_SaveChangesGetAudit()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider();

            EntityFrameworkEvent audit;

            using (var db = new AuditNetTestContext())
            {
                db.Database.EnsureCreated();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                audit = db.SaveChangesGetAudit();
            }

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Database, Is.EqualTo("FooBar"));
        }

        [Test]
        public async Task Test_IdentityContext_SaveChangesGetAudit_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider();

            EntityFrameworkEvent audit;

            await using (var db = new AuditNetTestContext())
            {
                await db.Database.EnsureCreatedAsync();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                audit = await db.SaveChangesGetAuditAsync();
            }

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Database, Is.EqualTo("FooBar"));
        }

        [Test]
        public void Test_EFDataProvider_AuditEntityDisabled_Fluent()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(typeName => Type.GetType(typeName + "Audit"))
                    .AuditEntityAction((ev, ent, audEnt) =>
                    {
                        return false;
                    })
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityDisabled_Fluent_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(typeName => Type.GetType(typeName + "Audit"))
                    .AuditEntityAction((ev, ent, audEnt) =>
                    {
                        return false;
                    })
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                await ctx.AddAsync(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Test_EFDataProvider_AuditEntityDisabled()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = (ev, entry, obj) =>
            {
                // return false to avoid saving
                return Task.FromResult(false);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void Test_EFDataProvider_AuditEntityDisabled_CtorFluent()
        {
            var dp = new EntityFrameworkDataProvider(_ => _
                .AuditTypeMapper(t =>
                {
                    if (t == typeof(Order))
                        return typeof(OrderAudit);
                    if (t == typeof(Orderline))
                        return typeof(OrderlineAudit);
                    return null;
                })
                .AuditEntityAction((ev, entry, obj) =>
                {
                    // return false to avoid saving
                    return false;
                }));

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityDisabledAsync()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = async (ev, entry, obj) =>
            {
                // return false to avoid saving
                return await Task.FromResult(false);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                await ctx.AddAsync(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(0));
            }
        }


        [Test]
        public void Test_EFDataProvider_ProxiedLazyLoading()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                evs.Add(scope.Event);
            });
                
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = (ev, entry, obj) =>
            {
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action;
                }
                return Task.FromResult(true);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = ctx.Order.FirstOrDefault();
                o.Number = id;
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudits = ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderAudits.Count, Is.EqualTo(1));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Number, Is.EqualTo(id));
            }
            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].GetEntry().Entity.GetType().FullName.StartsWith("Castle.Proxies."), Is.True);
        }

        [Test]
        public async Task Test_EFDataProvider_ProxiedLazyLoading_Async()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                evs.Add(scope.Event);
            });

            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = async (ev, entry, obj) =>
            {
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action;
                }
                return await Task.FromResult(true);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = await ctx.Order.FirstOrDefaultAsync();
                o.Number = id;
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudits = await ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToListAsync();
                Assert.That(orderAudits.Count, Is.EqualTo(1));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Number, Is.EqualTo(id));
            }
            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].GetEntry().Entity.GetType().FullName.StartsWith("Castle.Proxies."), Is.True);
        }


        [Test]
        public void Test_EFDataProvider()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = (ev, entry, obj) =>
            {
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action; 
                }

                return Task.FromResult(true);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id, Status = "Pending", OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudit = ctx.OrderAudit.AsNoTracking().SingleOrDefault(a => a.Number.Equals(id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(orderAudit.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(2));
                Assert.That(orderlineAudits[0].Product, Is.EqualTo("p1: " + id));
                Assert.That(orderlineAudits[1].Product, Is.EqualTo("p2: " + id));
                Assert.That(orderAudit.AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.That(orderAudit.UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[1].UserName));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var o = ctx.Order.Single(a => a.Number.Equals(id));
                o.Status = "Cancelled";
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudits = ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderAudits.Count, Is.EqualTo(2));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Status, Is.EqualTo("Cancelled"));
                Assert.That(orderAudits[1].Status, Is.EqualTo("Pending"));
                Assert.That(orderAudits[1].AuditStatus, Is.EqualTo("Insert"));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var ol = ctx.Orderline.Single(a => a.OrderId.Equals(order.Id) && a.Product.StartsWith("p1"));
                ctx.Remove(ol);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(3));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Delete"));
                Assert.That(orderlineAudits[0].Product.StartsWith("p1"), Is.True);
            }
            
        }

        [Test]
        public void Test_EFDataProvider_DifferentContext()
        {
            var dp = new EntityFrameworkDataProvider();

            dp.DbContextBuilder = ev => new AuditInDifferentContext();

            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            dp.AuditEntityAction = async (ev, entry, obj) =>
            {
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action;
                }

                return await Task.FromResult(true);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudit = ctx.OrderAudit.AsNoTracking().SingleOrDefault(a => a.Number.Equals(id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(orderAudit.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(2));
                Assert.That(orderlineAudits[0].Product, Is.EqualTo("p1: " + id));
                Assert.That(orderlineAudits[1].Product, Is.EqualTo("p2: " + id));
                Assert.That(orderAudit.AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.That(orderAudit.UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[1].UserName));
            }

            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = ctx.Order.Single(a => a.Number.Equals(id));
                o.Status = "Cancelled";
                ctx.SaveChanges();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudits = ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderAudits.Count, Is.EqualTo(2));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Status, Is.EqualTo("Cancelled"));
                Assert.That(orderAudits[1].Status, Is.EqualTo("Pending"));
                Assert.That(orderAudits[1].AuditStatus, Is.EqualTo("Insert"));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var ol = ctx.Orderline.Single(a => a.OrderId.Equals(order.Id) && a.Product.StartsWith("p1"));
                ctx.Remove(ol);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(3));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Delete"));
                Assert.That(orderlineAudits[0].Product.StartsWith("p1"), Is.True);
            }

        }

        [Test]
        public void Test_EFDataProvider_IgnorePropertyMatchingByType()
        {
            var dp = new EntityFrameworkDataProvider();

            dp.DbContextBuilder = ev => new AuditInDifferentContext();

            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };

            var id = Guid.NewGuid().ToString();

            dp.AuditEntityAction = (ev, entry, obj) =>
            {
                if (obj is OrderAudit oa)
                {
                    oa.Id = (long) entry.PrimaryKey.First().Value;
                    oa.Number = id;
                }
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action;
                }
                return Task.FromResult(true);
            };

            dp.IgnoreMatchedPropertiesFunc = t => t == typeof(OrderAudit);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            
            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                ctx.Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudit = ctx.OrderAudit.AsNoTracking().SingleOrDefault(a => a.Number.Equals(id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(orderAudit.Id)).ToList();
                Assert.That(orderlineAudits.Count, Is.EqualTo(2));
                Assert.That(orderlineAudits[0].Product, Is.EqualTo("p1: " + id));
                Assert.That(orderlineAudits[1].Product, Is.EqualTo("p2: " + id));
                Assert.That(orderAudit.AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderAudit.Status, Is.Null);
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.That(orderAudit.UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[1].UserName));
            }

            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = ctx.Order.Single(a => a.Number.Equals(id));
                o.Status = "Cancelled";
                ctx.SaveChanges();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudits = ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToList();
                Assert.That(orderAudits.Count, Is.EqualTo(2));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Status, Is.Null);
                Assert.That(orderAudits[1].Status, Is.Null);
                Assert.That(orderAudits[1].AuditStatus, Is.EqualTo("Insert"));
            }
        }

        [Test]
        public async Task Test_EFDataProvider_DifferentContext_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<AuditInDifferentContext>()
                    .AuditTypeMapper(t =>
                    {
                        if (t == typeof(Order))
                            return typeof(OrderAudit);
                        if (t == typeof(Orderline))
                            return typeof(OrderlineAudit);
                        return null;
                    })
                    .AuditEntityAction((ev, entry, obj) =>
                    {
                        var ab = obj as AuditBase;
                        if (ab != null)
                        {
                            ab.AuditDate = DateTime.UtcNow;
                            ab.UserName = ev.Environment.UserName;
                            ab.AuditStatus = entry.Action;
                        }
                        return true;
                    }));

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                await ctx.AddAsync(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudit = await ctx.OrderAudit.AsNoTracking().SingleOrDefaultAsync(a => a.Number.Equals(id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = await ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(orderAudit.Id)).ToListAsync();
                Assert.That(orderlineAudits.Count, Is.EqualTo(2));
                Assert.That(orderlineAudits[0].Product, Is.EqualTo("p1: " + id));
                Assert.That(orderlineAudits[1].Product, Is.EqualTo("p2: " + id));
                Assert.That(orderAudit.AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.That(orderAudit.UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[1].UserName));
            }

            using (var ctx = new AuditedContextNoAuditTables())
            {
                var o = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                o.Status = "Cancelled";
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditInDifferentContext())
            {
                var orderAudits = await ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToListAsync();
                Assert.That(orderAudits.Count, Is.EqualTo(2));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Status, Is.EqualTo("Cancelled"));
                Assert.That(orderAudits[1].Status, Is.EqualTo("Pending"));
                Assert.That(orderAudits[1].AuditStatus, Is.EqualTo("Insert"));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var ol = await  ctx.Orderline.SingleAsync(a => a.OrderId.Equals(order.Id) && a.Product.StartsWith("p1"));
                ctx.Remove(ol);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var orderlineAudits = await ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).OrderByDescending(a => a.AuditDate).ToListAsync();
                Assert.That(orderlineAudits.Count, Is.EqualTo(3));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Delete"));
                Assert.That(orderlineAudits[0].Product.StartsWith("p1"), Is.True);
            }

        }




        [Test]
        public async Task Test_EFDataProvider_Async()
        {
            var dp = new EntityFrameworkDataProvider();

            dp.AuditTypeMapper = (t, e) =>
            {
                if (t == typeof(Order))
                    return typeof(OrderAudit);
                if (t == typeof(Orderline))
                    return typeof(OrderlineAudit);
                return null;
            };
            dp.AuditEntityAction = (ev, entry, obj) =>
            {
                var ab = obj as AuditBase;
                if (ab != null)
                {
                    ab.AuditDate = DateTime.UtcNow;
                    ab.UserName = ev.Environment.UserName;
                    ab.AuditStatus = entry.Action;
                }
                return Task.FromResult(true);
            };

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(dp);
            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending",
                    OrderLines = new Collection<Orderline>()
                    {
                        new Orderline() { Product = "p1: " + id, Quantity = 2 },
                        new Orderline() { Product = "p2: " + id, Quantity = 3 }
                    }
                };
                await ctx.AddAsync(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudit = await ctx.OrderAudit.AsNoTracking().SingleOrDefaultAsync(a => a.Number.Equals(id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = await ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(orderAudit.Id)).ToListAsync();
                Assert.That(orderlineAudits.Count, Is.EqualTo(2));
                Assert.That(orderlineAudits[0].Product, Is.EqualTo("p1: " + id));
                Assert.That(orderlineAudits[1].Product, Is.EqualTo("p2: " + id));
                Assert.That(orderAudit.AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].AuditStatus, Is.EqualTo("Insert"));
                Assert.That(orderlineAudits[1].UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.That(orderAudit.UserName, Is.EqualTo(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[0].UserName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(orderlineAudits[1].UserName));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var o = ctx.Order.Single(a => a.Number.Equals(id));
                o.Status = "Cancelled";
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var orderAudits = await ctx.OrderAudit.AsNoTracking().Where(a => a.Number.Equals(id)).OrderByDescending(a => a.AuditDate).ToListAsync();
                Assert.That(orderAudits.Count, Is.EqualTo(2));
                Assert.That(orderAudits[0].AuditStatus, Is.EqualTo("Update"));
                Assert.That(orderAudits[0].Status, Is.EqualTo("Cancelled"));
                Assert.That(orderAudits[1].Status, Is.EqualTo("Pending"));
                Assert.That(orderAudits[1].AuditStatus, Is.EqualTo("Insert"));
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var ol = await ctx.Orderline.SingleAsync(a => a.OrderId.Equals(order.Id) && a.Product.StartsWith("p1"));
                ctx.Remove(ol);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.Order.SingleAsync(a => a.Number.Equals(id));
                var orderlineAudits = await ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).OrderByDescending(a => a.AuditDate).ToListAsync();
                Assert.That(orderlineAudits.Count, Is.EqualTo(3));
                Assert.That(orderlineAudits[0].AuditStatus, Is.EqualTo("Delete"));
                Assert.That(orderlineAudits[0].Product.StartsWith("p1"), Is.True);
            }
        }

        [Test]
        public void Test_EF_Transaction()
        {
            var provider = new Mock<AuditDataProvider>();
            var auditEvents = new List<AuditEvent>();
            provider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                auditEvents.Add(ev);
                return Guid.NewGuid();
            });
            provider.Setup(p => p.CloneValue(It.IsAny<object>(), It.IsAny<AuditEvent>())).Returns((object obj, AuditEvent _) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object)
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(x => x.OnScopeCreated(sc =>
                {
                    var wcfEvent = sc.GetEntityFrameworkEvent();
                    Assert.That(wcfEvent.Database, Is.EqualTo("BlogsVerbose"));
                }));
            int blogId;
            using (var ctx = new MyAuditedVerboseContext())
            {
                var blog = new Blog() { BloggerName = "fede", Title = "test" };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
                blogId = blog.Id;
            }
            using (var ctx = new MyAuditedVerboseContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    ctx.Posts.Add(new Post() { BlogId = blogId, Content = "other content 1", DateCreated = DateTime.Now, Title = "other title 1" });
                    ctx.SaveChanges();
                    ctx.Posts.Add(new Post() { BlogId = blogId, Content = "other content 2", DateCreated = DateTime.Now, Title = "other title 2" });
                    ctx.SaveChanges();
                    transaction.Rollback();
                }
                ctx.Posts.Add(new Post() { BlogId = blogId, Content = "other content 3", DateCreated = DateTime.Now, Title = "other title 3" });
                
                ctx.SaveChanges();
            }
            var ev1 = (auditEvents[1] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev2 = (auditEvents[2] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev3 = (auditEvents[3] as AuditEventEntityFramework).EntityFrameworkEvent;
            Assert.NotNull(ev1.TransactionId);
            Assert.NotNull(ev2.TransactionId);
            Assert.Null(ev3.TransactionId);
            Assert.That(ev2.TransactionId, Is.EqualTo(ev1.TransactionId));

            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_EF_Actions()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditEvent auditEvent = null;
            provider.Setup(x => x.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                auditEvent = ev;
                return Guid.NewGuid();
            });
            provider.Setup(p => p.CloneValue(It.IsAny<object>(), It.IsAny<AuditEvent>())).Returns((object obj, AuditEvent _) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlRaw(@"
--delete from AuditPosts
--delete from AuditBlogs
delete from Posts
delete from Blogs
SET IDENTITY_INSERT Blogs ON 
insert into Blogs (id, title, bloggername) values (1, 'abc', 'def')
insert into Blogs (id, title, bloggername) values (2, 'ghi', 'jkl')
SET IDENTITY_INSERT Blogs OFF
SET IDENTITY_INSERT Posts ON
insert into Posts (id, title, datecreated, content, blogid) values (1, 'my post 1', GETDATE(), 'this is an example 123', 1)
insert into Posts (id, title, datecreated, content, blogid) values (2, 'my post 2', GETDATE(), 'this is an example 456', 1)
insert into Posts (id, title, datecreated, content, blogid) values (3, 'my post 3', GETDATE(), 'this is an example 789', 1)
insert into Posts (id, title, datecreated, content, blogid) values (4, 'my post 4', GETDATE(), 'this is an example 987', 2)
insert into Posts (id, title, datecreated, content, blogid) values (5, 'my post 5', GETDATE(), 'this is an example 000', 2)
SET IDENTITY_INSERT Posts OFF
                    ");

                var postsblog1 = ctx.Blogs.Include(x => x.Posts)
                    .FirstOrDefault(x => x.Id == 1);
                postsblog1.BloggerName += "-22";

                ctx.Posts.Add(new Post() { BlogId = 1, Content = "content", DateCreated = DateTime.Now, Title = "title" });

                var ch1 = ctx.Posts.FirstOrDefault(x => x.Id == 1);
                ch1.Content += "-code";

                var pr = ctx.Posts.FirstOrDefault(x => x.Id == 5);
                ctx.Remove(pr);

                int result = ctx.SaveChanges();

                Assert.IsFalse(auditEvent.CustomFields.ContainsKey("EntityFrameworkEvent"));
                var efEvent = (auditEvent as AuditEventEntityFramework).EntityFrameworkEvent;

                Assert.That(result, Is.EqualTo(4));
                Assert.That(auditEvent.EventType, Is.EqualTo("BlogsVerbose" + "_" + ctx.GetType().Name));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && e.ColumnValues["Title"].Equals("title") && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Update" && (e.Entity as Blog)?.Id == 1 && e.Changes[0].ColumnName == "BloggerName"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && (e.Entity as Post)?.Id == 5));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && e.ColumnValues["Id"].Equals(5) && (e.Entity as Post)?.Id == 5));
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);

            }
        }

        [Test]
        public async Task Test_EF_Actions_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditEvent auditEvent = null;
            provider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditEvent ev, CancellationToken ct) =>
            {
                auditEvent = ev;
                return Guid.NewGuid();
            });
            provider.Setup(p => p.CloneValue(It.IsAny<object>(), It.IsAny<AuditEvent>())).Returns((object obj, AuditEvent _) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlRaw(@"
--delete from AuditPosts
--delete from AuditBlogs
delete from Posts
delete from Blogs
SET IDENTITY_INSERT Blogs ON 
insert into Blogs (id, title, bloggername) values (1, 'abc', 'def')
insert into Blogs (id, title, bloggername) values (2, 'ghi', 'jkl')
SET IDENTITY_INSERT Blogs OFF
SET IDENTITY_INSERT Posts ON
insert into Posts (id, title, datecreated, content, blogid) values (1, 'my post 1', GETDATE(), 'this is an example 123', 1)
insert into Posts (id, title, datecreated, content, blogid) values (2, 'my post 2', GETDATE(), 'this is an example 456', 1)
insert into Posts (id, title, datecreated, content, blogid) values (3, 'my post 3', GETDATE(), 'this is an example 789', 1)
insert into Posts (id, title, datecreated, content, blogid) values (4, 'my post 4', GETDATE(), 'this is an example 987', 2)
insert into Posts (id, title, datecreated, content, blogid) values (5, 'my post 5', GETDATE(), 'this is an example 000', 2)
SET IDENTITY_INSERT Posts OFF
                    ");

                var postsblog1 = ctx.Blogs.Include(x => x.Posts)
                    .FirstOrDefault(x => x.Id == 1);
                postsblog1.BloggerName += "-22";

                ctx.Posts.Add(new Post() { BlogId = 1, Content = "content", DateCreated = DateTime.Now, Title = "title" });

                var ch1 = ctx.Posts.FirstOrDefault(x => x.Id == 1);
                ch1.Content += "-code";

                var pr = ctx.Posts.FirstOrDefault(x => x.Id == 5);
                ctx.Remove(pr);

                int result = await ctx.SaveChangesAsync();

                Assert.IsFalse(auditEvent.CustomFields.ContainsKey("EntityFrameworkEvent"));
                var efEvent = (auditEvent as AuditEventEntityFramework).EntityFrameworkEvent;

                Assert.That(result, Is.EqualTo(4));
                Assert.That(auditEvent.EventType, Is.EqualTo("BlogsVerbose" + "_" + ctx.GetType().Name));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && e.ColumnValues["Title"].Equals("title") && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Update" && (e.Entity as Blog)?.Id == 1 && e.Changes[0].ColumnName == "BloggerName"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && (e.Entity as Post)?.Id == 5));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && e.ColumnValues["Id"].Equals(5) && (e.Entity as Post)?.Id == 5));
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
                provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);

            }
        }
    }

    public class OtherContextFromDbContext : DbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Blogs");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // from dbcontext
            optionsBuilder.UseSqlServer(CnnString);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }
    
    public class MyUnauditedContext : MyBaseContext
    { 
        public MyUnauditedContext() : base("BlogsUnaudited")
        {
        }
        
        public override bool AuditDisabled
        {
            get { return true; }
            set { }
        }
    }

    [AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = true, AuditEventType = "{database}_{context}")]
    public class MyAuditedVerboseContext : MyBaseContext
    {
        public MyAuditedVerboseContext() : base("BlogsVerbose")
        {
        }

        public void SetDataProvider(AuditDataProvider dataProvider)
        {
            this.AuditDataProvider = dataProvider;
        }
    }

    [AuditDbContext(IncludeEntityObjects = false)]
    public class MyAuditedContext : MyBaseContext
    {
        public MyAuditedContext() : base("BlogsAudited")
        {
        }
    }

    public class MyBaseContext : AuditDbContext
    {
        private string _cnnString;

        public override bool AuditDisabled { get; set; }

        public MyBaseContext(string dbName)
        {
            _cnnString = TestHelper.GetConnectionString(dbName);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_cnnString);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<AuditPost> AuditPosts { get; set; }
        public DbSet<AuditBlog> AuditBlogs { get; set; }
    }

    public class MyTransactionalContext : MyBaseContext
    { 
        public MyTransactionalContext() : base("BlogsTran")
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseLazyLoadingProxies();
        }

        public override void OnScopeCreated(IAuditScope auditScope)
        {
            Database.BeginTransaction();
        }
        public override void OnScopeSaving(IAuditScope auditScope)
        {
            if (auditScope.GetEntityFrameworkEvent().Entries[0].ColumnValues.ContainsKey("BloggerName")
                && auditScope.GetEntityFrameworkEvent().Entries[0].ColumnValues["BloggerName"].ToString() == "ROLLBACK")
            {
                GetCurrentTran().Rollback();
            }
            else
            {
                GetCurrentTran().Commit();
            }
        }

        private IDbContextTransaction GetCurrentTran()
        {
            var dbtxmgr = this.GetInfrastructure().GetService<IDbContextTransactionManager>();
            var relcon = dbtxmgr as IRelationalConnection;
            return relcon.CurrentTransaction;
        }
    }

    [AuditIgnore]
    public class AuditBlog : BaseEntity
    {
        public override int Id { get; set; }
        public int BlogId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Changes { get; set; }
    }
    [AuditIgnore]
    public class AuditPost : BaseEntity
    {
        public override int Id { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Changes { get; set; }
    }

    public abstract class BaseEntity
    {
        public virtual int Id { get; set; }

    }

    public class Blog : BaseEntity
    {
        public override int Id { get; set; }
        public virtual string Title { get; set; }
        public virtual string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post : BaseEntity
    {
        public override int Id { get; set; }
        [MaxLength(20)]
        public virtual string Title { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual string Content { get; set; }
        public virtual int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
    }


    public class Order
    {
        public virtual long Id { get; set; }
        public virtual string Number { get; set; }
        public virtual string Status { get; set; }
        public virtual ICollection<Orderline> OrderLines { get; set; }
    }
    public class Orderline
    {
        public virtual long Id { get; set; }
        public virtual string Product { get; set; }
        public virtual int Quantity { get; set; }

        public virtual long OrderId { get; set; }
        public virtual Order Order { get; set; }
    }
    //[AuditIgnore]
    public abstract class AuditBase
    {
        public DateTime AuditDate { get; set; }
        public string AuditStatus { get; set; }
        public string UserName { get; set; }
    }
    //[AuditIgnore]
    public class OrderAudit : AuditBase
    {
        public virtual long Id { get; set; }

        [Key]
        public virtual long OrderAuditId { get; set; }
        public virtual long OrderId { get; set; }
        public virtual string Number { get; set; }
        public virtual string Status { get; set; }
    }
    //[AuditIgnore]
    public class OrderlineAudit : AuditBase
    {
        public virtual long Id { get; set; }

        [Key]
        public virtual long OrderlineAuditId { get; set; }
        public virtual string Product { get; set; }
        public virtual int Quantity { get; set; }
        public virtual long OrderId { get; set; }
    }

    public class AuditedContextNoAuditTables : AuditDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Audit");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
        }
        public DbSet<Order> Order { get; set; }
        public DbSet<Orderline> Orderline { get; set; }
    }
    public class AuditInDifferentContext : DbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Audit");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
        }
        public DbSet<OrderAudit> OrderAudit { get; set; }
        public DbSet<OrderlineAudit> OrderlineAudit { get; set; }
    }

    public class AuditPerTableContext : AuditDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("Audit");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
            optionsBuilder.UseLazyLoadingProxies();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Order> Order { get; set; }
        public DbSet<Orderline> Orderline { get; set; }
        public DbSet<OrderAudit> OrderAudit { get; set; }
        public DbSet<OrderlineAudit> OrderlineAudit { get; set; }
    }
    public class Foo
    {
        public int Id { get; set; }
        [Required]
        public string Bar { get; set; }
        public string Car { get; set; }
    }
    [AuditIgnore]
    public class FooAudit
    {
        public int Id { get; set; }
        public string Bar { get; set; }
        public string Username { get; set; }
    }
    public class AuditNetTestContext : Audit.EntityFramework.AuditIdentityDbContext
    {
        public static string CnnString = TestHelper.GetConnectionString("FooBar");
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(CnnString);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Foo> Foos { get; set; }
        public DbSet<FooAudit> FooAudits { get; set; }
        public AuditNetTestContext()
        {

        }

        public AuditNetTestContext(DbContextOptions<AuditNetTestContext> options) : base(options)
        {

        }
    }

}

