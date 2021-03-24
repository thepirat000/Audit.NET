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
    public class EfCoreTests
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

#if EF_CORE_5
        [Test]
        public void Test_EF_Core_ManyToMany_NoJoinEntity()
        {
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(_ => _.OnInsert(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));
            using (var context = new ManyToManyContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                var tag1 = new ManyToManyContext.Tag() { Id = 101, Text = "tag 1" };
                var tag2 = new ManyToManyContext.Tag() { Id = 102, Text = "tag 2" };
                var post = new ManyToManyContext.Post()
                {
                    Id = "10",
                    Name = "test",
                    Tags = new List<ManyToManyContext.Tag>() { tag1, tag2 }
                };
                context.Posts.Add(post);
                context.SaveChanges();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(5, evs[0].Entries.Count);
            Assert.IsTrue(evs[0].Entries.Any(e => e.Table == "Tags" && e.Action == "Insert" && e.ColumnValues["Text"]?.ToString() == "tag 1"));
            Assert.IsTrue(evs[0].Entries.Any(e => e.Table == "Tags" && e.Action == "Insert" && e.ColumnValues["Text"]?.ToString() == "tag 2"));
            Assert.IsTrue(evs[0].Entries.Any(e => e.Table == "Posts" && e.Action == "Insert" && e.ColumnValues["Name"]?.ToString() == "test"));
            Assert.IsTrue(evs[0].Entries.Any(e => e.Table == "PostTag" && e.Action == "Insert" && e.ColumnValues["PostsId"]?.ToString() == "10" && e.ColumnValues["TagsId"]?.ToString() == "101"));
            Assert.IsTrue(evs[0].Entries.Any(e => e.Table == "PostTag" && e.Action == "Insert" && e.ColumnValues["PostsId"]?.ToString() == "10" && e.ColumnValues["TagsId"]?.ToString() == "102"));
        }

        [Test]
        public void Test_EF_Core_ManyToMany_NoJoinEntity_EFProvider()
        {
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .AuditTypeExplicitMapper(map => map
                        .Map<ManyToManyContext.Post, ManyToManyContext.Audit_Post>((post, auditPost) =>
                        {
                            auditPost.Id = "test";
                            auditPost.PostId = int.Parse(post.Id);
                            auditPost.Name = post.Name;
                        })
                        .Map<ManyToManyContext.Tag, ManyToManyContext.Audit_Tag>((tag, auditTag) =>
                        {
                            auditTag.TagId = tag.Id;
                            auditTag.Text = tag.Text;
                        })
                        .MapTable("PostTag", (EventEntry ent, ManyToManyContext.Audit_PostTag auditPostTag) =>
                        {
                            auditPostTag.Extra = "extra";
                        })
                        .AuditEntityAction((ev, ent, obj) =>
                        {
                            ((dynamic)obj).Action = ent.Action;
                        }))
                    .IgnoreMatchedProperties(t => t == typeof(ManyToManyContext.Tag) || t == typeof(ManyToManyContext.Post)));



            using (var context = new ManyToManyContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                // Add Post 10 related to tag 101 and 102
                var tag1 = new ManyToManyContext.Tag() { Id = 101, Text = "tag 1" };
                var tag2 = new ManyToManyContext.Tag() { Id = 102, Text = "tag 2" };
                var post = new ManyToManyContext.Post()
                {
                    Id = "10",
                    Name = "test",
                    Tags = new List<ManyToManyContext.Tag>() { tag1, tag2 }
                };
                context.Posts.Add(post);
                context.SaveChanges();

                // Remove Post 10 relation with tag 101
                post = context.Posts.Include(p => p.Tags).FirstOrDefault(p => p.Id == "10");
                post.Tags.Remove(post.Tags.First(t => t.Id == 101));
                context.SaveChanges();
            }

            // Assert
            using (var context = new ManyToManyContext())
            {
                Assert.True(context.Audit_Posts.Any(p => p.PostId == 10 && p.Action == "Insert"));
                Assert.True(context.Audit_Tags.Any(t => t.TagId == 101 && t.Action == "Insert"));
                Assert.True(context.Audit_Tags.Any(t => t.TagId == 102 && t.Action == "Insert"));
                Assert.True(context.Audit_PostTags.Any(pt => pt.TagsId == 101 && pt.PostsId == "10" && pt.Action == "Insert" && pt.Extra == "extra"));
                Assert.True(context.Audit_PostTags.Any(pt => pt.TagsId == 102 && pt.PostsId == "10" && pt.Action == "Insert" && pt.Extra == "extra"));
                Assert.True(context.Audit_PostTags.Any(pt => pt.TagsId == 101 && pt.PostsId == "10" && pt.Action == "Delete" && pt.Extra == "extra"));
            }

        }
#endif

#if EF_CORE_3 || EF_CORE_5
        [Test]
        public void Test_EF_Core_OwnedSingleMultiple()
        {
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(_ => _.OnInsert(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));

            using (var context = new OwnedSingleMultiple_Context())
            {

                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();

                context.Departments.Add(new OwnedSingleMultiple_Context.Department()
                {
                    Name = "deparment test",
                    Address = new OwnedSingleMultiple_Context.Address { City = "Vienna1", Street = "First Street" },
                });

				context.Persons.Add(new OwnedSingleMultiple_Context.Person()
				{
					Name = "person test",
					Addresses = new List<OwnedSingleMultiple_Context.Address> { new OwnedSingleMultiple_Context.Address { City = "Vienna2", Street = "First Street" } },
				});

                context.SaveChanges();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(4, evs[0].Entries.Count); 
            Assert.AreEqual("deparment test", evs[0].Entries.First(e => e.Table == "Department" && e.ColumnValues.ContainsKey("Name")).ColumnValues["Name"]);
            Assert.AreEqual("person test", evs[0].Entries.First(e => e.Table == "Person" && e.ColumnValues.ContainsKey("Name")).ColumnValues["Name"]);
            Assert.AreEqual("Vienna1", evs[0].Entries.First(e => e.Table == "Department" && e.ColumnValues.ContainsKey("Address_City")).ColumnValues["Address_City"]);
            Assert.AreEqual("Vienna2", evs[0].Entries.First(e => e.Table == "Person_Addresses" && e.ColumnValues.ContainsKey("City")).ColumnValues["City"]);
        }
#endif

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
                context.ExcludeTransactionId = false;
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

        [Test]
        public void Test_EF_MapProxyTypes()
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

                context.Posts.Add(new ProxyPost() { BlogId = blog.Id, Blog = blog, Title = "TestPost", Content = guid });
                context.SaveChanges();

                var post = context.Posts.First(b => b.Content == guid);

                var audits = context.CommonAudits.Where(a => a.Group == guid).OrderBy(a => a.AuditDate).ToList();

                Assert.AreEqual(2, audits.Count);
                Assert.AreEqual("Blog", audits[0].EntityType);
                Assert.AreEqual(blog.Id, audits[0].EntityId);
                Assert.AreEqual(blog.Title, audits[0].Title);

                Assert.AreEqual("Post", audits[1].EntityType);
                Assert.AreEqual(post.Id, audits[1].EntityId);
                Assert.AreEqual($"F:TestPost", audits[1].Title);

                Assert.AreEqual(Environment.UserName, audits[0].AuditUser);
                Assert.AreEqual(Environment.UserName, audits[1].AuditUser);
            }
        }

        public class ProxyPost : Post 
        {
           public override string Title { get => base.Title; set => base.Title = $"F:{value}"; }
        }

    }
}