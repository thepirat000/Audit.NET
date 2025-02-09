using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using System.Threading.Tasks;

using Audit.Core;
using Audit.EntityFramework.Full.UnitTest;

using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTestIntegrationTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
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
                    .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(config => config
                    .ForEntity<Blog>(_ => _.Format(b => b.Title, t => t + "X")));

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

            Assert.That(list.Count, Is.EqualTo(2));
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Insert"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo(title + "X"));
            entries = list[1].EntityFrameworkEvent.Entries;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Update"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo("NewTitleX"));
            Assert.That(entries[0].Changes.Count, Is.EqualTo(1));
            Assert.That(entries[0].Changes[0].ColumnName, Is.EqualTo("Title"));
            Assert.That(entries[0].Changes[0].NewValue, Is.EqualTo("NewTitleX"));
            Assert.That(entries[0].Changes[0].OriginalValue, Is.EqualTo(title + "X"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
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
                Assert.That(order, Is.Not.Null);
                Assert.That(order.Status, Is.EqualTo("FromAction"));
            }
        }

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
                    .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyBaseContext>(config => config
                    .ForEntity<Blog>(_ => _.Override("Title", null)));

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

            Assert.That(list.Count, Is.EqualTo(1));
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Insert"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo(title));
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
                  .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
              .ForAnyContext(config => config
                  .ForEntity<Blog>(_ => _.Override("Title", null)));

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

            Assert.That(list.Count, Is.EqualTo(2));
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Insert"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo(null));
            entries = list[1].EntityFrameworkEvent.Entries;
            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].Action, Is.EqualTo("Update"));
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.That(entries[0].ColumnValues["Title"], Is.EqualTo(null));
            Assert.That(entries[0].Changes.Count, Is.EqualTo(1));
            Assert.That(entries[0].Changes[0].ColumnName, Is.EqualTo("Title"));
            Assert.That(entries[0].Changes[0].NewValue, Is.EqualTo(null));
            Assert.That(entries[0].Changes[0].OriginalValue, Is.EqualTo(null));
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
                ctx.Blogs.Add(new Blog()
                {
                    BloggerName = "abc",
                    Title = "Test_EF_PrimaryKeyUpdate",
                    Posts = new List<Post>()
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

            Assert.That(logs.Count, Is.EqualTo(1));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries[0].Table, Is.EqualTo("Blogs"));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries[1].Table, Is.EqualTo("Posts"));
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"], Is.EqualTo((int)logs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"]));
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"] > 0, Is.True);
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"] > 0, Is.True);
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["Id"] > 0, Is.True);
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[1].PrimaryKey["Id"] > 0, Is.True);
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["BlogId"] > 0, Is.True);
            Assert.That((int)logs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"], Is.EqualTo((int)logs[0].GetEntityFrameworkEvent().Entries[1].ColumnValues["BlogId"]));

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
                ctx.Blogs.Add(new Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<Post>()
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

            Assert.That(logs.Count, Is.EqualTo(1));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries.Count, Is.EqualTo(1));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries[0].Table, Is.EqualTo("Posts"));

            logs.Clear();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<MyTransactionalContext>()
                    .Reset()
                    .UseOptIn()
                        .IncludeAny(t => t.Name.StartsWith("Blog"));

            using (var ctx = new MyTransactionalContext())
            {
                ctx.Blogs.Add(new Blog()
                {
                    BloggerName = "fede",
                    Title = "blog1-test",
                    Posts = new List<Post>()
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

            Assert.That(logs.Count, Is.EqualTo(1));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries.Count, Is.EqualTo(1));
            Assert.That(logs[0].GetEntityFrameworkEvent().Entries[0].Table, Is.EqualTo("Blogs"));

            Assert.That(logs[0].Environment.StackTrace.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name), Is.True);
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
                ctx.Blogs.Add(new Blog()
                {
                    BloggerName = "fede",
                    Title = b1Title,
                    Posts = new List<Post>()
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
                ctx.Blogs.Add(new Blog()
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

            Assert.That(logs.Count, Is.EqualTo(3));
#if NET462
            Assert.That(logs[0].Environment.StackTrace.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name), Is.True);
#endif
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
                db.Database.CreateIfNotExists();
                db.Foos.Add(new Foo());
                Assert.Throws<DbEntityValidationException>(() => {
                    db.SaveChanges();
                });
            }
        }

        [Test]
        public void Test_EFDataProvider_IdentityContext_Error_Async()
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
                db.Database.CreateIfNotExists();
                db.Foos.Add(new Foo());
                Assert.ThrowsAsync<DbEntityValidationException>(async () => {
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
                db.Database.CreateIfNotExists();
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
                db.Database.CreateIfNotExists();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                db.AddAuditCustomField("Field", "Value");
                await db.SaveChangesAsync();
            }

            var auditEvents = dp.GetAllEvents();

            Assert.That(auditEvents, Has.Count.EqualTo(1));
            Assert.That(auditEvents[0].CustomFields, Has.Count.EqualTo(1));
            Assert.That(auditEvents[0].CustomFields["Field"].ToString(), Is.EqualTo("Value"));
        }

        [Test]
        public void Test_IdentityContext_SaveChangesGetAudit()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider();

            EntityFrameworkEvent audit;

            using (var db = new AuditNetTestContext())
            {
                db.Database.CreateIfNotExists();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                audit = db.SaveChangesGetAudit();
            }

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Database, Is.EqualTo("FooBar2"));
        }

        [Test]
        public async Task Test_IdentityContext_SaveChangesGetAudit_Async()
        {
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider();

            EntityFrameworkEvent audit;

            using (var db = new AuditNetTestContext())
            {
                db.Database.CreateIfNotExists();
                var foo = new Foo()
                {
                    Bar = "Test"
                };
                db.Foos.Add(foo);
                audit = await db.SaveChangesGetAuditAsync();
            }

            Assert.That(audit, Is.Not.Null);
            Assert.That(audit.Database, Is.EqualTo("FooBar2"));
        }

        private DbContextTransaction GetCurrentTran(DbContext context)
        {
            return context.Database.CurrentTransaction;
        }

        [AuditDbContext(IncludeEntityObjects = true)]
        public class AuditPerTableContext : AuditDbContext
        {
            public static string CnnString = TestHelper.GetConnectionString("Audit");

            public AuditPerTableContext()
                : base(CnnString)
            {
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                Database.SetInitializer<AuditPerTableContext>(null);
                modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            }

            public DbSet<Order> Order { get; set; }
            public DbSet<Orderline> Orderline { get; set; }
            public DbSet<OrderAudit> OrderAudit { get; set; }
            public DbSet<OrderlineAudit> OrderlineAudit { get; set; }
        }
        public class MyBaseContext : AuditDbContext
        {
            public static string CnnString = TestHelper.GetConnectionString("Blogs");
            public override bool AuditDisabled { get; set; }

            public MyBaseContext()
                : base(CnnString)
            {
            }

            public DbSet<Blog> Blogs { get; set; }
            public DbSet<Post> Posts { get; set; }
            public DbSet<AuditPost> AuditPosts { get; set; }
            public DbSet<AuditBlog> AuditBlogs { get; set; }
        }
        public abstract class BaseEntity
        {
            public virtual int Id { get; set; }

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
            [Key, Column(Order = 1)]
            public DateTime AuditDate { get; set; }
            public string AuditStatus { get; set; }
            public string UserName { get; set; }
        }

        public class OrderAudit : AuditBase
        {
            [Key, Column(Order = 0)]
            public long Id { get; set; }
            public string Number { get; set; }
            public string Status { get; set; }
        }
        public class OrderlineAudit : AuditBase
        {
            [Key, Column(Order = 0)]
            public long Id { get; set; }
            public string Product { get; set; }
            public int Quantity { get; set; }
            public long OrderId { get; set; }
        }

        public class MyTransactionalContext : MyBaseContext
        {
            public override void OnScopeCreated(IAuditScope auditScope)
            {
                Database.BeginTransaction();
            }
            public override void OnScopeSaving(IAuditScope auditScope)
            {
                if (auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues.ContainsKey("BloggerName")
                    && auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues["BloggerName"].Equals("ROLLBACK"))
                {
                    Database.CurrentTransaction.Rollback();
                }
                else
                {
                    Database.CurrentTransaction.Commit();
                }
            }
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
            public static string CnnString = TestHelper.GetConnectionString("FooBar2");
            
            public DbSet<Foo> Foos { get; set; }
            public DbSet<FooAudit> FooAudits { get; set; }

            public AuditNetTestContext() : base(CnnString)
            {

            }
        }
    }
}