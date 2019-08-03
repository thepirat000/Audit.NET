using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Internal;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfCore22Tests
    {
        [OneTimeSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new BlogsContext().Database.EnsureCreated();
        }

        [Test]
        public void Test_EF_TransactionId()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev);
                }));

            var id = Guid.NewGuid().ToString().Substring(0, 8);
            using (var context = new BlogsContext())
            {
                using (var tran = context.Database.BeginTransaction())
                {
                    var blog = context.Blogs.First();
                    blog.Title = id;
                    context.SaveChanges();
                    tran.Commit();
                }
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(evs[0].GetEntityFrameworkEvent().TransactionId));
        }

        [Test]
        public void Test_EF_TransactionId_Exclude_ByContext()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev);
                }));

            var id = Guid.NewGuid().ToString().Substring(0, 8);
            using (var context = new BlogsContext())
            {
                context.ExcludeTransactionId = true;
                using (var tran = context.Database.BeginTransaction())
                {
                    var blog = context.Blogs.First();
                    blog.Title = id;
                    context.SaveChanges();
                    tran.Commit();
                }
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsNull(evs[0].GetEntityFrameworkEvent().TransactionId);
            Assert.IsNull(evs[0].GetEntityFrameworkEvent().AmbientTransactionId);
        }

        [Test]
        public void Test_EF_TransactionId_Exclude_ByConfig()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(_ => _.ExcludeTransactionId(true));

            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev);
                }));

            var id = Guid.NewGuid().ToString().Substring(0, 8);

            using (var context = new BlogsContext())
            {
                using (var tran = context.Database.BeginTransaction())
                {
                    var blog = context.Blogs.First();
                    blog.Title = id;
                    context.SaveChanges();
                    tran.Commit();
                }
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsNull(evs[0].GetEntityFrameworkEvent().TransactionId);
            Assert.IsNull(evs[0].GetEntityFrameworkEvent().AmbientTransactionId);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>().Reset();
        }

        [Test]
        public void Test_ProxiedLazyLoading()
        {
            var guid = Guid.NewGuid().ToString().Substring(0, 6);
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x.
                    IncludeEntityObjects(true))
                .UseOptIn();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.Core.Configuration.AddOnCreatedAction(scope =>
            {
                var efEvent = scope.GetEntityFrameworkEvent();
                efEvent.CustomFields["Additional Field On event"] = new { x = 1, y = "one" };
                efEvent.Entries[0].CustomFields["Additional Field On entry"] = new { x = 2, y = "two" };
            });
            AuditEventEntityFramework ev2;
            using (var ctx = new BlogsContext())
            {
                var blog = ctx.Blogs.FirstOrDefault();
                blog.Title = guid;

                ctx.SaveChanges();

                ev2 = AuditEvent.FromJson<AuditEventEntityFramework>(evs[0].ToJson());
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].GetEntityFrameworkEvent().Entries[0].Entity.GetType().FullName.StartsWith("Castle.Proxies."));
            Assert.AreEqual(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"], evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"]);
            Assert.AreEqual(guid, evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Title"]);
            Assert.AreEqual("Blogs", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("dbo", evs[0].GetEntityFrameworkEvent().Entries[0].Schema.ToLower());
            Assert.AreEqual(1, (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).x);
            Assert.AreEqual("two", (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).y);
            Assert.AreEqual("one", (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).y);
            Assert.AreEqual(2, (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).x);
            Assert.IsNotNull(ev2.EntityFrameworkEvent.CustomFields["Additional Field On event"]);
            Assert.IsNotNull(ev2.EntityFrameworkEvent.Entries[0].CustomFields["Additional Field On entry"]);
        }

        [Test]
        public void Test_EF_CustomFields()
        {
            var guid = Guid.NewGuid().ToString().Substring(0, 6);
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsContext>(x => x.
                    IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.Core.Configuration.AddOnCreatedAction(scope =>
            {
                var efEvent = scope.GetEntityFrameworkEvent();
                efEvent.CustomFields["Additional Field On event"] = new { x = 1, y = "one" };
                efEvent.Entries[0].CustomFields["Additional Field On entry"] = new { x = 2, y = "two" };
            });

            using (var ctx = new BlogsContext())
            {
                var blog = new Blog()
                {
                    Title = guid
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

            var ev2 = AuditEvent.FromJson<AuditEventEntityFramework>(evs[0].ToJson());

            Assert.AreEqual(1, evs.Count);

            Assert.AreEqual(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"], evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"]);
            Assert.AreEqual(guid, evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Title"]);
            Assert.AreEqual("Blogs", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("dbo", evs[0].GetEntityFrameworkEvent().Entries[0].Schema.ToLower());
            Assert.AreEqual(1, (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).x);
            Assert.AreEqual("two", (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).y);
            Assert.AreEqual("one", (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).y);
            Assert.AreEqual(2, (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).x);
            Assert.IsNotNull(ev2.EntityFrameworkEvent.CustomFields["Additional Field On event"]);
            Assert.IsNotNull(ev2.EntityFrameworkEvent.Entries[0].CustomFields["Additional Field On entry"]);
        }

        [Test]
        public void Test_EF_CompositeRepeatedForeignKey()
        {
            // Issue #178
            var events = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    events.Add(ev.GetEntityFrameworkEvent());
                }));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DemoContext>(cfg => cfg.IncludeEntityObjects());

            var id = new Random().Next();

            using (var ctx = new DemoContext())
            {
                var tenant = new Tenant() { Id = id, Name = "tenant" };
                ctx.Tenants.Add(tenant);

                ctx.SaveChanges();

                var employee1 = new Employee() { Id = id + 1, Name = "test1", TenantId = tenant.Id };
                ctx.Employees.Add(employee1);
                var employee2 = new Employee() { Id = id + 2, Name = "test2", TenantId = tenant.Id };
                ctx.Employees.Add(employee2);

                ctx.SaveChanges();

                var petty = new PettyCashTransaction() { Id = id + 3, EmployeeId = employee1.Id, TenantId = tenant.Id, TrusteeId = employee2.Id };
                ctx.Pettys.Add(petty);
                ctx.SaveChanges();

                var emp = ctx.Employees.Single(x => x.Id == employee1.Id);
                emp.Name = $"test1-updated";
                ctx.SaveChanges();
            }

            Assert.AreEqual(4, events.Count);
            Assert.AreEqual(1, events[0].Entries.Count);
            Assert.AreEqual(id, events[0].Entries[0].ColumnValues["Id"]);
            Assert.AreEqual("tenant", events[0].Entries[0].ColumnValues["Name"]);
            Assert.AreEqual(id, events[0].Entries[0].PrimaryKey["Id"]);

            Assert.AreEqual(2, events[1].Entries.Count);
            Assert.AreEqual(id+1, events[1].Entries[0].ColumnValues["Id"]);
            Assert.AreEqual("test1", events[1].Entries[0].ColumnValues["Name"]);
            Assert.AreEqual(id, events[1].Entries[0].ColumnValues["TenantId"]);
            Assert.AreEqual(2, events[1].Entries[0].PrimaryKey.Count);
            Assert.AreEqual(id+1, events[1].Entries[0].PrimaryKey["Id"]);
            Assert.AreEqual(id, events[1].Entries[0].PrimaryKey["TenantId"]);

            Assert.AreEqual(id + 2, events[1].Entries[1].ColumnValues["Id"]);
            Assert.AreEqual("test2", events[1].Entries[1].ColumnValues["Name"]);
            Assert.AreEqual(id, events[1].Entries[1].ColumnValues["TenantId"]);
            Assert.AreEqual(2, events[1].Entries[1].PrimaryKey.Count);
            Assert.AreEqual(id + 2, events[1].Entries[1].PrimaryKey["Id"]);
            Assert.AreEqual(id, events[1].Entries[1].PrimaryKey["TenantId"]);

            Assert.AreEqual(1, events[2].Entries.Count);
            Assert.AreEqual(id + 3, events[2].Entries[0].ColumnValues["Id"]);
            Assert.AreEqual(id + 1, events[2].Entries[0].ColumnValues["EmployeeId"]);
            Assert.AreEqual(id, events[2].Entries[0].ColumnValues["TenantId"]);
            Assert.AreEqual(id + 2, events[2].Entries[0].ColumnValues["TrusteeId"]);
            Assert.AreEqual(id + 3, events[2].Entries[0].PrimaryKey["Id"]);

            Assert.AreEqual(1, events[3].Entries.Count);
            Assert.AreEqual(1, events[3].Entries[0].Changes.Count);
            Assert.AreEqual("Name", events[3].Entries[0].Changes[0].ColumnName);
            Assert.AreEqual("test1", events[3].Entries[0].Changes[0].OriginalValue);
            Assert.AreEqual("test1-updated", events[3].Entries[0].Changes[0].NewValue);

        }

        [Test]
        public void Test_EF_MapMultipleTypesToSameAuditType()
        {
            var guid = Guid.NewGuid().ToString();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<BlogsContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Blog, CommonAudit>((blog, commonAudit) =>
                        {
                            commonAudit.EntityType = "Blog";
                            commonAudit.EntityId = blog.Id;
                            commonAudit.Title = blog.Title;
                            commonAudit.Group = guid;
                        })
                        .Map<Post, CommonAudit>((post, commonAudit) =>
                        {
                            commonAudit.EntityType = "Post";
                            commonAudit.EntityId = post.Id;
                            commonAudit.Title = post.Title;
                            commonAudit.Group = guid;
                        })
                        .AuditEntityAction<IAuditEntity>((ev, entry, entity) =>
                        {
                            entity.AuditAction = entry.Action;
                            entity.AuditDate = DateTime.Now;
                            entity.AuditUser = Environment.UserName;
                            entity.Exception = ev.GetEntityFrameworkEvent().ErrorMessage;
                        })));

            using (var context = new BlogsContext())
            {
                context.Blogs.Add(new Blog { BloggerName = guid, Title = "TestBlog" });
                context.SaveChanges();

                var blog = context.Blogs.First(b => b.BloggerName == guid);

                context.Posts.Add(new Post() { BlogId = blog.Id, Title = "TestPost", Content = guid });
                context.SaveChanges();

                var post = context.Posts.First(b => b.Content == guid);

                var audits = context.CommonAudits.Where(a => a.Group == guid).OrderBy(a => a.AuditDate).ToList();

                Assert.AreEqual(2, audits.Count);
                Assert.AreEqual("Blog", audits[0].EntityType);
                Assert.AreEqual(blog.Id, audits[0].EntityId);
                Assert.AreEqual(blog.Title, audits[0].Title);

                Assert.AreEqual("Post", audits[1].EntityType);
                Assert.AreEqual(post.Id, audits[1].EntityId);
                Assert.AreEqual(post.Title, audits[1].Title);

                Assert.AreEqual(Environment.UserName, audits[0].AuditUser);
                Assert.AreEqual(Environment.UserName, audits[1].AuditUser);
            }
        }

        [Test]
        public void Test_EF_MapAllTypesToSameAuditType()
        {
            var guid = Guid.NewGuid().ToString();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<BlogsContext>()
                    .AuditTypeMapper(t => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, entry, entity) =>
                    {
                        entity.AuditAction = JsonConvert.SerializeObject(entry, Audit.Core.Configuration.JsonSettings);
                        entity.Group = guid;
                        entity.EntityId = (int)entry.PrimaryKey.First().Value;
                        entity.EntityType = entry.EntityType.Name;
                        entity.AuditDate = DateTime.Now;
                        entity.AuditUser = Environment.UserName;
                        entity.Exception = ev.GetEntityFrameworkEvent().ErrorMessage;
                    }));

            using (var context = new BlogsContext())
            {
                context.Blogs.Add(new Blog { BloggerName = guid, Title = "TestBlog" });
                context.SaveChanges();

                var blog = context.Blogs.First(b => b.BloggerName == guid);

                context.Posts.Add(new Post() { BlogId = blog.Id, Blog = blog, Title = "TestPost", Content = guid });
                context.SaveChanges();

                var post = context.Posts.First(b => b.Content == guid);

                var audits = context.CommonAudits.Where(a => a.Group == guid).OrderBy(a => a.AuditDate).ToList();

                Assert.AreEqual(2, audits.Count);
                Assert.AreEqual("Blog", audits[0].EntityType);
                Assert.AreEqual(blog.Id, audits[0].EntityId);
                Assert.AreEqual(blog.Title, audits[0].Title);

                Assert.AreEqual("Post", audits[1].EntityType);
                Assert.AreEqual(post.Id, audits[1].EntityId);
                Assert.AreEqual(post.Title, audits[1].Title);

                Assert.AreEqual(Environment.UserName, audits[0].AuditUser);
                Assert.AreEqual(Environment.UserName, audits[1].AuditUser);
            }
        }

        [Test]
        public void Test_EF_MapOneTypeToMultipleAuditTypes()
        {
            var guid = Guid.NewGuid().ToString();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<BlogsContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Blog>(mapper: entry => entry.Action == "Update" ? typeof(BlogAudit) : typeof(CommonAudit), 
                        entityAction: (ev, entry, entity) =>
                        {
                            if (entry.Action == "Update")
                            {
                                Assert.AreEqual(typeof(BlogAudit), entity.GetType());
                                var ba = entity as BlogAudit;
                                ba.BlogId = (int)entry.PrimaryKey.First().Value;
                            }
                            else
                            {
                                Assert.AreEqual(typeof(CommonAudit), entity.GetType());
                                var ca = entity as CommonAudit;
                                ca.Group = guid;
                                ca.EntityType = entry.EntityType.Name;
                                ca.EntityId = (int)entry.PrimaryKey.First().Value;
                            }
                        })
                        .Map<Post, PostAudit>((post, postAudit) =>
                        {
                            postAudit.PostId = post.Id;
                        })
                        .AuditEntityAction<IAuditEntity>((ev, entry, entity) =>
                        {
                            entity.AuditAction = entry.Action;
                            entity.AuditDate = DateTime.Now;
                            entity.AuditUser = Environment.UserName;
                            entity.Exception = ev.GetEntityFrameworkEvent().ErrorMessage;
                        })));

            using (var context = new BlogsContext())
            {
                context.Blogs.Add(new Blog { BloggerName = guid, Title = "TestBlog" });
                context.SaveChanges(); // CommonAudit

                var blog = context.Blogs.First(b => b.BloggerName == guid);
                var newTitle = guid.Substring(0, 10);
                blog.Title = newTitle;
                context.SaveChanges(); // BlogAudit

                var audits = context.CommonAudits.Where(a => a.Group == guid).OrderBy(a => a.AuditDate).ToList();
                var blogaudits = context.BlogsAudits.Where(x => x.Title == newTitle).ToList();

                Assert.IsNotNull(blogaudits);
                Assert.AreEqual(1, blogaudits.Count);
                Assert.AreEqual(blog.Id, blogaudits[0].BlogId);
                Assert.AreEqual(1, audits.Count);
                Assert.AreEqual("Blog", audits[0].EntityType);
                Assert.AreEqual(blog.Id, audits[0].EntityId);
                Assert.AreEqual("TestBlog", audits[0].Title);
                Assert.AreEqual(Environment.UserName, audits[0].AuditUser);
            }
        }

        [Test]
        public void Test_EFFailureLogging()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<BlogsContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Blog, BlogAudit>((blog, blogAudit) =>
                        {
                            blogAudit.BlogId = blog.Id;
                        })
                        .Map<Post, PostAudit>((post, postAudit) =>
                        {
                            postAudit.PostId = post.Id;
                        })
                        .AuditEntityAction<IAuditEntity>((ev, entry, entity) =>
                        {
                            entity.AuditAction = entry.Action;
                            entity.AuditDate = DateTime.Now;
                            entity.AuditUser = Environment.UserName;
                            entity.Exception = ev.GetEntityFrameworkEvent().ErrorMessage;
                        })));

            var guid = Guid.NewGuid().ToString();
            var longText = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            using (var context = new BlogsContext())
            {
                context.Blogs.Add(new Blog { BloggerName = guid, Title = "Test_EFFailureLogging" });
                context.SaveChanges();
               
                try
                {
                    context.Blogs.Add(new Blog { BloggerName = guid, Title = longText });
                    // This fails because of the long title
                    context.SaveChanges();
                    Assert.Fail("Should have thrown DbUpdateException");
                }
                catch (DbUpdateException)
                {
                }

                var audits = context.BlogsAudits.Where(a => a.BloggerName == guid).OrderBy(a => a.AuditDate).ToList();

                Assert.AreEqual(2, audits.Count);
                Assert.AreEqual("Test_EFFailureLogging", audits[0].Title);
                Assert.AreEqual(longText, audits[1].Title);
                Assert.IsTrue(audits[1].Exception.Length > 5);
            }


        }

#if NETCOREAPP2_1
        [Test]
        public void Test_EFTransactionScope()
        {
            var list = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                    {
                        list.Add(ev.GetEntityFrameworkEvent());
                    }));
            
            var guid = Guid.NewGuid().ToString();
            Blog blog1;
            using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew,
                new TransactionOptions {IsolationLevel = IsolationLevel.ReadCommitted}))
            {

                using (var connection = new SqlConnection(BlogsContext.CnnString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM dbo.Blogs Where Title = '1' OR Title = '2'";
                    command.ExecuteNonQuery();
                }

                using (var context = new BlogsContext())
                {
                    context.Blogs.Add(new Blog {BloggerName = guid, Title = "1"});
                    context.SaveChanges();

                    context.Blogs.Add(new Blog {BloggerName = guid, Title = "2"});
                    context.SaveChanges();

                    blog1 = context.Blogs
                        .FirstOrDefault(b => b.BloggerName == guid);

                    scope.Complete();

                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        context.Blogs.FirstOrDefault();
                    }, "Should have been thrown InvalidOperationException since scope is completed");
                }
            }
            
            Assert.AreEqual(2, list.Count);
            Assert.IsNotNull(blog1);
            Assert.IsTrue(list[0].AmbientTransactionId.Length > 2);
            Assert.AreEqual(list[0].AmbientTransactionId, list[1].AmbientTransactionId);
        }
#endif

    }
}