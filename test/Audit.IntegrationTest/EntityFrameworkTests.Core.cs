#if NETCOREAPP1_0 || NETCOREAPP2_0
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Audit.EntityFramework.Providers;
using Audit.SqlServer.Providers;
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Data.SqlClient;

namespace Audit.IntegrationTest
{
    [TestFixture(Category = "EF")]
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
                ctx.Database.ExecuteSqlCommand(sql);
            }
        }

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EFDataProvider_IdentityContext_Error()
        {
            // Issue #106
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
                db.Foos.Add(new Foo());
                Assert.Throws<DbUpdateException>(() => {
                    db.SaveChanges();
                });
            }
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
                Assert.AreEqual(0, orderlineAudits.Count);
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
                Assert.AreEqual(0, orderlineAudits.Count);
            }
        }

        [Test]
        public void Test_EFDataProvider_AuditEntityDisabled()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = t =>
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
                return false;
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
                Assert.AreEqual(0, orderlineAudits.Count);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityDisabledAsync()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = t =>
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
                return false;
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
                Assert.AreEqual(0, orderlineAudits.Count);
            }
        }

        [Test]
        public void Test_EFDataProvider()
        {
            var dp = new EntityFrameworkDataProvider();
            dp.AuditTypeMapper = t =>
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
                return true;
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
                Assert.AreEqual(2, orderlineAudits.Count);
                Assert.AreEqual("p1: " + id, orderlineAudits[0].Product);
                Assert.AreEqual("p2: " + id, orderlineAudits[1].Product);
                Assert.AreEqual("Insert", orderAudit.AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[0].AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[1].AuditStatus);
                Assert.AreEqual(orderlineAudits[0].UserName, orderlineAudits[1].UserName);
                Assert.AreEqual(orderlineAudits[0].UserName, orderAudit.UserName);
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
                Assert.AreEqual(2, orderAudits.Count);
                Assert.AreEqual("Update", orderAudits[0].AuditStatus);
                Assert.AreEqual("Cancelled", orderAudits[0].Status);
                Assert.AreEqual("Pending", orderAudits[1].Status);
                Assert.AreEqual("Insert", orderAudits[1].AuditStatus);
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
                Assert.AreEqual(3, orderlineAudits.Count);
                Assert.AreEqual("Delete", orderlineAudits[0].AuditStatus);
                Assert.IsTrue(orderlineAudits[0].Product.StartsWith("p1"));
            }
            
        }

        [Test]
        public void Test_EFDataProvider_DifferentContext()
        {
            var dp = new EntityFrameworkDataProvider();

            dp.DbContextBuilder = ev => new AuditInDifferentContext();

            dp.AuditTypeMapper = t =>
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
                return true;
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
                Assert.AreEqual(2, orderlineAudits.Count);
                Assert.AreEqual("p1: " + id, orderlineAudits[0].Product);
                Assert.AreEqual("p2: " + id, orderlineAudits[1].Product);
                Assert.AreEqual("Insert", orderAudit.AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[0].AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[1].AuditStatus);
                Assert.AreEqual(orderlineAudits[0].UserName, orderlineAudits[1].UserName);
                Assert.AreEqual(orderlineAudits[0].UserName, orderAudit.UserName);
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
                Assert.AreEqual(2, orderAudits.Count);
                Assert.AreEqual("Update", orderAudits[0].AuditStatus);
                Assert.AreEqual("Cancelled", orderAudits[0].Status);
                Assert.AreEqual("Pending", orderAudits[1].Status);
                Assert.AreEqual("Insert", orderAudits[1].AuditStatus);
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
                Assert.AreEqual(3, orderlineAudits.Count);
                Assert.AreEqual("Delete", orderlineAudits[0].AuditStatus);
                Assert.IsTrue(orderlineAudits[0].Product.StartsWith("p1"));
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
                Assert.AreEqual(2, orderlineAudits.Count);
                Assert.AreEqual("p1: " + id, orderlineAudits[0].Product);
                Assert.AreEqual("p2: " + id, orderlineAudits[1].Product);
                Assert.AreEqual("Insert", orderAudit.AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[0].AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[1].AuditStatus);
                Assert.AreEqual(orderlineAudits[0].UserName, orderlineAudits[1].UserName);
                Assert.AreEqual(orderlineAudits[0].UserName, orderAudit.UserName);
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
                Assert.AreEqual(2, orderAudits.Count);
                Assert.AreEqual("Update", orderAudits[0].AuditStatus);
                Assert.AreEqual("Cancelled", orderAudits[0].Status);
                Assert.AreEqual("Pending", orderAudits[1].Status);
                Assert.AreEqual("Insert", orderAudits[1].AuditStatus);
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
                Assert.AreEqual(3, orderlineAudits.Count);
                Assert.AreEqual("Delete", orderlineAudits[0].AuditStatus);
                Assert.IsTrue(orderlineAudits[0].Product.StartsWith("p1"));
            }

        }




        [Test]
        public async Task Test_EFDataProvider_Async()
        {
            var dp = new EntityFrameworkDataProvider();

            dp.AuditTypeMapper = t =>
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
                return true;
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
                Assert.AreEqual(2, orderlineAudits.Count);
                Assert.AreEqual("p1: " + id, orderlineAudits[0].Product);
                Assert.AreEqual("p2: " + id, orderlineAudits[1].Product);
                Assert.AreEqual("Insert", orderAudit.AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[0].AuditStatus);
                Assert.AreEqual("Insert", orderlineAudits[1].AuditStatus);
                Assert.AreEqual(orderlineAudits[0].UserName, orderlineAudits[1].UserName);
                Assert.AreEqual(orderlineAudits[0].UserName, orderAudit.UserName);
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
                Assert.AreEqual(2, orderAudits.Count);
                Assert.AreEqual("Update", orderAudits[0].AuditStatus);
                Assert.AreEqual("Cancelled", orderAudits[0].Status);
                Assert.AreEqual("Pending", orderAudits[1].Status);
                Assert.AreEqual("Insert", orderAudits[1].AuditStatus);
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
                Assert.AreEqual(3, orderlineAudits.Count);
                Assert.AreEqual("Delete", orderlineAudits[0].AuditStatus);
                Assert.IsTrue(orderlineAudits[0].Product.StartsWith("p1"));
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
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object)
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(x => x.OnScopeCreated(sc =>
                {
                    var wcfEvent = sc.Event.GetEntityFrameworkEvent();
                    Assert.AreEqual("Blogs", wcfEvent.Database);
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
            Assert.AreEqual(ev1.TransactionId, ev2.TransactionId);

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
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlCommand(@"
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

                Assert.AreEqual(4, result);
                Assert.AreEqual("Blogs" + "_" + ctx.GetType().Name, auditEvent.EventType);
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
            provider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync((AuditEvent ev) =>
            {
                auditEvent = ev;
                return Guid.NewGuid();
            });
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            using (var ctx = new MyAuditedVerboseContext())
            {
                ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlCommand(@"
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

                Assert.AreEqual(4, result);
                Assert.AreEqual("Blogs" + "_" + ctx.GetType().Name, auditEvent.EventType);
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Insert" && e.ColumnValues["Title"].Equals("title") && (e.Entity as Post)?.Title == "title"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Update" && (e.Entity as Blog)?.Id == 1 && e.Changes[0].ColumnName == "BloggerName"));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && (e.Entity as Post)?.Id == 5));
                Assert.True(efEvent.Entries.Any(e => e.Action == "Delete" && e.ColumnValues["Id"].Equals(5) && (e.Entity as Post)?.Id == 5));
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
                provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);

            }
        }
    }

    public class OtherContextFromDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // from dbcontext
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Blogs;integrated security=true;");
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
        public override bool AuditDisabled
        {
            get { return true; }
            set { }
        }
    }

    [AuditDbContext(Mode = AuditOptionMode.OptOut, IncludeEntityObjects = true, AuditEventType = "{database}_{context}")]
    public class MyAuditedVerboseContext : MyBaseContext
    {
        public void SetDataProvider(AuditDataProvider dataProvider)
        {
            this.AuditDataProvider = dataProvider;
        }
    }

    [AuditDbContext(IncludeEntityObjects = false)]
    public class MyAuditedContext : MyBaseContext
    {

    }

    public class MyBaseContext : AuditDbContext
    {
        public override bool AuditDisabled { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Blogs;integrated security=true;");
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
        public override void OnScopeCreated(AuditScope auditScope)
        {
            Database.BeginTransaction();
        }
        public override void OnScopeSaving(AuditScope auditScope)
        {
            if (auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues.ContainsKey("BloggerName")
                && auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues["BloggerName"].ToString() == "ROLLBACK")
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
        public string Title { get; set; }
        public string BloggerName { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
    }
    public class Post : BaseEntity
    {
        public override int Id { get; set; }
        [MaxLength(20)]
        public string Title { get; set; }
        public DateTime DateCreated { get; set; }
        public string Content { get; set; }
        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }


    public class Order
    {
        public long Id { get; set; }
        public string Number { get; set; }
        public string Status { get; set; }
        public virtual ICollection<Orderline> OrderLines { get; set; }
    }
    public class Orderline
    {
        public long Id { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }

        public long OrderId { get; set; }
        public Order Order { get; set; }
    }

    public abstract class AuditBase
    {
        public DateTime AuditDate { get; set; }
        public string AuditStatus { get; set; }
        public string UserName { get; set; }
    }

    public class OrderAudit : AuditBase
    {
        public long Id { get; set; }
        public string Number { get; set; }
        public string Status { get; set; }
    }
    public class OrderlineAudit : AuditBase
    {
        public long Id { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public long OrderId { get; set; }
    }

    public class AuditedContextNoAuditTables : AuditDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Audit;integrated security=true;");
        }
        public DbSet<Order> Order { get; set; }
        public DbSet<Orderline> Orderline { get; set; }
    }
    public class AuditInDifferentContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Audit;integrated security=true;");
        }
        public DbSet<OrderAudit> OrderAudit { get; set; }
        public DbSet<OrderlineAudit> OrderlineAudit { get; set; }
    }

    public class AuditPerTableContext : AuditDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=Audit;integrated security=true;");
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
    public class FooAudit
    {
        public int Id { get; set; }
        public string Bar { get; set; }
        public string Username { get; set; }
    }

    public class AuditNetTestContext : Audit.EntityFramework.AuditIdentityDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("data source=localhost;initial catalog=FooBar;integrated security=true;");
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
    }

}
#endif
