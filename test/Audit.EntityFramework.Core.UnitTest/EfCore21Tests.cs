using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture(Category = "EF")]
    public class EfCore21Tests
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
        public void Test_EF_MapAllTypesToSameAuditType()
        {
            var guid = Guid.NewGuid().ToString();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(_ => _
                    .UseDbContext<BlogsContext>()
                    .AuditTypeMapper(t => typeof(CommonAudit))
                    .AuditEntityAction<CommonAudit>((ev, entry, entity) =>
                    {
                        entity.AuditAction = JsonConvert.SerializeObject(entry);
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
    }
}