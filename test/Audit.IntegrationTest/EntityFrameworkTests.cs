using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;
#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
#else
using System.Data.Entity;
#endif

namespace Audit.IntegrationTest
{
    [TestFixture(Category ="EF")]
    public class EntityFrameworkTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EF_Override_Func()
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
                    .ForEntity<IntegrationTest.Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(config => config
                    .ForEntity<IntegrationTest.Blog>(_ => _.Format(b => b.Title, t => t + "X")));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new MyTransactionalContext())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }
            using (var ctx = new MyTransactionalContext())
            {
                var blog = ctx.Blogs.First(b => b.Title == title);
                blog.BloggerName = "another";
                blog.Title = "NewTitle";
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, list.Count);
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Insert", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual(title + "X", entries[0].ColumnValues["Title"]);
            entries = list[1].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Update", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual("NewTitleX", entries[0].ColumnValues["Title"]);
            Assert.AreEqual(1, entries[0].Changes.Count);
            Assert.AreEqual("Title", entries[0].Changes[0].ColumnName);
            Assert.AreEqual("NewTitleX", entries[0].Changes[0].NewValue);
            Assert.AreEqual(title + "X", entries[0].Changes[0].OriginalValue);
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_1()
        {
            //AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task<bool>> entityAsyncAction);

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>()
                        .AuditEntityAction<OrderAudit>(async (ev, ent, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                            return true;
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_2()
        {
            //AuditEntityAction(Func<AuditEvent, EventEntry, object, Task<bool>> entityAsyncAction);

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>()
                        .AuditEntityAction(async (ev, ent, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                            return true;
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_3()
        {
            //AuditEntityAction(Func<AuditEvent, EventEntry, object, Task> entityAsyncAction);

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>()
                        .AuditEntityAction(async (ev, ent, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_4()
        {
            //AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task> entityAsyncAction);

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>()
                        .AuditEntityAction<OrderAudit>(async (ev, ent, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        
        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_1()
        {
            //Map<TSourceEntity, TAuditEntity>(Func<AuditEvent, EventEntry, TAuditEntity, Task> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>(async (ae, ee, audEnt) => 
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_2()
        {
            //Map<TSourceEntity, TAuditEntity>(Func<AuditEvent, EventEntry, TAuditEntity, Task<bool>> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>(async (ae, ee, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                            return true;
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_3()
        {
            //Map<TSourceEntity, TAuditEntity>(Func<TSourceEntity, TAuditEntity, Task> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>(async (se, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_4()
        {
            //Map<TSourceEntity, TAuditEntity>(Func<TSourceEntity, TAuditEntity, Task<bool>> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order, OrderAudit>(async (se, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                            return true;
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_5()
        {
            //Map<TSourceEntity>(Func<EventEntry, Type> mapper, Func<AuditEvent, EventEntry, object, Task<bool>> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order>(ee => typeof(OrderAudit), async (ae, ee, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                            return true;
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_6()
        {
            //Map<TSourceEntity>(Func<EventEntry, Type> mapper, Func<AuditEvent, EventEntry, object, Task> entityAsyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .Map<Order>(ee => typeof(OrderAudit), async (ae, ee, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_7()
        {
            //MapExplicit<TAuditEntity>(Func<EventEntry, bool> predicate, Func<EventEntry, TAuditEntity, Task> entityAsyncAction = null)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .MapExplicit<OrderAudit>(ee => true, async (ee, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_Map_8()
        {
            //MapTable<TAuditEntity>(string tableName, Func<EventEntry, TAuditEntity, Task> entityAsyncAction = null)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeExplicitMapper(m => m
                        .MapTable<OrderAudit>("Order", async (ee, audEnt) =>
                        {
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }))
                );

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_TypeMapper_1()
        {
            //AuditEntityAction(Func<AuditEvent, EventEntry, object, Task> asyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(t => typeof(OrderAudit))
                    .AuditEntityAction(async (ae, ee, obj) =>
                        {
                            var audEnt = (OrderAudit)obj;
                            audEnt.AuditDate = DateTime.UtcNow;
                            await Task.Delay(1000);
                            audEnt.Status = "FromAction";
                        }));

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_TypeMapper_2()
        {
            //AuditEntityAction(Func<AuditEvent, EventEntry, object, Task<bool>> asyncFunction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(t => typeof(OrderAudit))
                    .AuditEntityAction(async (ae, ee, obj) =>
                    {
                        var audEnt = (OrderAudit)obj;
                        audEnt.AuditDate = DateTime.UtcNow;
                        await Task.Delay(1000);
                        audEnt.Status = "FromAction";
                        return true;
                    }));

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_TypeMapper_3()
        {
            //AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task> asyncAction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(t => typeof(OrderAudit))
                    .AuditEntityAction<OrderAudit>(async (ae, ee, audEnt) =>
                    {
                        audEnt.AuditDate = DateTime.UtcNow;
                        await Task.Delay(1000);
                        audEnt.Status = "FromAction";
                    }));

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

        [Test]
        public async Task Test_EFDataProvider_AuditEntityAsyncAction_TypeMapper_4()
        {
            //AuditEntityAction<T>(Func<AuditEvent, EventEntry, T, Task<bool>> asyncFunction)

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(config => config
                    .AuditTypeMapper(t => typeof(OrderAudit))
                    .AuditEntityAction<OrderAudit>(async (ae, ee, audEnt) =>
                    {
                        audEnt.AuditDate = DateTime.UtcNow;
                        await Task.Delay(1000);
                        audEnt.Status = "FromAction";
                        return true;
                    }));

            var id = Guid.NewGuid().ToString();
            using (var ctx = new AuditPerTableContext())
            {
                var o = new Order()
                {
                    Number = id,
                    Status = "Pending"
                };
                ctx.Order.Add(o);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = await ctx.OrderAudit.AsQueryable().FirstOrDefaultAsync(o => o.Number == id);
                Assert.IsNotNull(order);
                Assert.AreEqual("FromAction", order.Status);
            }
        }

#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
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
                    .ForEntity<IntegrationTest.Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyBaseContext>(config => config
                    .ForEntity<IntegrationTest.Blog>(_ => _.Override("Title", null)));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new MyTransactionalContext())
            {
                var blog = ctx.Blogs.FirstOrDefault();
                blog.Title = title;
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, list.Count);
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.IsTrue(entries[0].GetEntry().Entity.GetType().FullName.StartsWith("Castle.Proxies."));
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Update", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual(title, entries[0].ColumnValues["Title"]);
        }
#endif

        [Test]
        public void Test_EF_IgnoreOverride_CheckCrossContexts()
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
                    .ForEntity<IntegrationTest.Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyBaseContext>(config => config
                    .ForEntity<IntegrationTest.Blog>(_ => _.Override("Title", null)));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new MyTransactionalContext())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

             Assert.AreEqual(1, list.Count);
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Insert", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual(title, entries[0].ColumnValues["Title"]);
        }

        [Test]
        public void Test_IgnoreOverrideProperties_Basic()
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
                  .ForEntity<IntegrationTest.Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
              .ForAnyContext(config => config
                  .ForEntity<IntegrationTest.Blog>(_ => _.Override("Title", null)));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new MyTransactionalContext())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            {
                var blog = ctx.Blogs.First(b => b.Title == title);
                blog.BloggerName = "another";
                blog.Title = "x";
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, list.Count);
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Insert", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual(null, entries[0].ColumnValues["Title"]);
            entries = list[1].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Update", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual(null, entries[0].ColumnValues["Title"]);
            Assert.AreEqual(1, entries[0].Changes.Count);
            Assert.AreEqual("Title", entries[0].Changes[0].ColumnName);
            Assert.AreEqual(null, entries[0].Changes[0].NewValue);
            Assert.AreEqual(null, entries[0].Changes[0].OriginalValue);
        }


        [Test]
        public void Test_EF_PrimaryKeyUpdate()
        {
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(p => p
                    .OnInsert(ev => { logs.Add(ev); }));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptOut();

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "abc",
                    Title = "Test_EF_PrimaryKeyUpdate",
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = "post-test",
                            Content = "post content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual("Blogs", logs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("Posts", logs[0].GetEntityFrameworkEvent().Entries[1].Table);
            Assert.AreEqual((int)logs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"], (int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"]);
            Assert.IsTrue((int)logs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"] > 0);
            Assert.IsTrue((int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"] > 0);
            Assert.IsTrue((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["Id"] > 0);
            Assert.IsTrue((int)logs[0].GetEntityFrameworkEvent().Entries[1].PrimaryKey["Id"] > 0);
            Assert.IsTrue((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["BlogId"] > 0);
            Assert.AreEqual((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["BlogId"], (int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"]);

        }

        [Test]
        public void Test_EF_IncludeIgnoreFilters()
        {
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .IncludeStackTrace()
                .UseDynamicProvider(p => p
                    .OnInsert(ev =>
                    {
                        logs.Add(ev);
                    }));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptOut()
                        .IgnoreAny(t => t.Name.StartsWith("Blo"));

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = "post1-test",
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(1, logs[0].GetEntityFrameworkEvent().Entries.Count);
            Assert.AreEqual("Posts", logs[0].GetEntityFrameworkEvent().Entries[0].Table);

            logs.Clear();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptIn()
                        .IncludeAny(t => t.Name.StartsWith("Blog"));

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = "post1-test",
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            Assert.AreEqual(1, logs.Count);
            Assert.AreEqual(1, logs[0].GetEntityFrameworkEvent().Entries.Count);
            Assert.AreEqual("Blogs", logs[0].GetEntityFrameworkEvent().Entries[0].Table);

            Assert.IsTrue(logs[0].Environment.StackTrace.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
        }


        [Test]
        public void Test_EF_SaveOnSameContext_Transaction()
        {
            var b1Title = Guid.NewGuid().ToString().Substring(1, 10);
            var p1Title = Guid.NewGuid().ToString().Substring(1, 10);
            var logs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .IncludeStackTrace()
                .UseDynamicProvider(p => p
                    .OnInsert(ev =>
                    {
                        logs.Add(ev);
                    }))
                 .WithCreationPolicy(EventCreationPolicy.Manual);

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "fede",
                    Title = b1Title,
                    Posts = new List<IntegrationTest.Post>()
                    {
                        new Post()
                        {
                            Title = p1Title,
                            Content = "post1 content",
                            DateCreated = DateTime.Now
                        }
                    }
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            { 
                var p = ctx.Posts.FirstOrDefault(x => x.Title == p1Title);
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == b1Title);
                Assert.NotNull(p);
                Assert.NotNull(b);
                ctx.Blogs.Remove(b);
                ctx.Posts.Remove(p);
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new IntegrationTest.Blog()
                {
                    BloggerName = "ROLLBACK",
                    Title = "blog1-test"
                });
                ctx.SaveChanges();
            }

            using (var ctx = new MyTransactionalContext())
            {
                var p = ctx.Posts.FirstOrDefault(x => x.Title == p1Title);
                var b = ctx.Blogs.FirstOrDefault(x => x.Title == b1Title);
                Assert.Null(p);
                Assert.Null(b);
            }

            Assert.AreEqual(3, logs.Count);
#if NET452 || NET461
            Assert.IsTrue(logs[0].Environment.StackTrace.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
#endif
        }

#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
        private IDbContextTransaction GetCurrentTran(DbContext context)
        {
            var dbtxmgr = context.GetInfrastructure().GetService<IDbContextTransactionManager>();
            var relcon = dbtxmgr as IRelationalConnection;
            return relcon.CurrentTransaction;
        }
#else
        private DbContextTransaction GetCurrentTran(DbContext context)
        {
            return context.Database.CurrentTransaction;
        }
#endif
    }
}