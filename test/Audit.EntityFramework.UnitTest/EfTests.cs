using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataBaseService;

namespace Audit.EntityFramework.UnitTest
{

    [TestFixture]
    [Category("LocalDb")]
    public class EfTests
    {
        [SetUp]
        public void SetUp()
        {

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<Entities>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_EF_CustomFields()
        {
            var guid = Guid.NewGuid().ToString().Substring(0, 6);
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(x => x.
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

            using (var ctx = new BlogsEntities())
            {
                var blog = new Blog()
                {
                    Title = guid
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }

            
            var ev2 = AuditEvent.FromJson<AuditEventEntityFramework>(evs[0].ToJson());
            var sub1 = EntityFrameworkEvent.FromJson(evs[0].GetEntityFrameworkEvent().ToJson());
            var sub2 = EventEntry.FromJson(evs[0].GetEntityFrameworkEvent().Entries[0].ToJson());

            var settings = new JsonSerializerOptions() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
            Assert.AreEqual(JsonSerializer.Serialize(evs[0].GetEntityFrameworkEvent(), settings), JsonSerializer.Serialize(sub1, settings));

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
                .ForContext<BlogsEntities>(config => config
                    .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext(config => config
                    .ForEntity<Blog>(_ => _.Format(b => b.Title, t => t + "X")));

            var title = Guid.NewGuid().ToString().Substring(1, 25);
            using (var ctx = new BlogsEntities())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                ctx.SaveChanges();
            }
            using (var ctx = new BlogsEntities())
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
        public class User
        {
            public string Password;
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
                .ForContext<BlogsEntities>(config => config
                    .ForEntity<Blog>(_ => _.Ignore(blog => blog.BloggerName)));
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<DataBaseContext>(config => config
                    .ForEntity<Blog>(_ => _.Override<string>(b => b.Title, ent => null)));

            var title = Guid.NewGuid().ToString().Substring(0, 25);
            using (var ctx = new BlogsEntities())
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
              .ForContext<BlogsEntities>(config => config
                  .ForEntity<Blog>(_ => _.Ignore("BloggerName")
                                         .Override(blog => blog.Title, "******")));

            var title = Guid.NewGuid().ToString().Substring(1, 25);
            using (var ctx = new BlogsEntities())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                // this will execute via SP
                ctx.SaveChanges();
            }

            using (var ctx = new BlogsEntities())
            {
                var blog = ctx.Blogs.First(b => b.Title == title);
                blog.BloggerName = "another";
                blog.Title = "x";
                // this will execute via SP
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, list.Count);
            var entries = list[0].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Insert", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual("******", entries[0].ColumnValues["Title"]);
            entries = list[1].EntityFrameworkEvent.Entries;
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual("Update", entries[0].Action);
            Assert.IsFalse(entries[0].ColumnValues.ContainsKey("BloggerName"));
            Assert.AreEqual("******", entries[0].ColumnValues["Title"]);
            Assert.AreEqual(1, entries[0].Changes.Count);
            Assert.AreEqual("Title", entries[0].Changes[0].ColumnName);
            Assert.AreEqual("******", entries[0].Changes[0].NewValue);
            Assert.AreEqual("******", entries[0].Changes[0].OriginalValue);
        }
        


        [Test]
        public void Test_FunctionMapping()
        {
            AuditEventEntityFramework auditEvent = null;
            Audit.Core.Configuration.Setup()
               .IncludeStackTrace(true)
               .UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
               {
                   auditEvent = ev as AuditEventEntityFramework;
               }))
               .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(config => config
                    .IncludeEntityObjects(true)
                    .AuditEventType("{context}:{database}"))
                .Reset()
                .UseOptOut();

            var title = Guid.NewGuid().ToString().Substring(1, 25);
            using (var ctx = new BlogsEntities())
            {
                var blog = new Blog()
                {
                    Title = title,
                    BloggerName = "test"
                };
                ctx.Blogs.Add(blog);
                // this will execute via SP
                ctx.SaveChanges();
            }

            Audit.Core.Configuration.Setup()
                .IncludeStackTrace(false);

            Assert.AreEqual(1, auditEvent.EntityFrameworkEvent.Entries.Count);
            // PK is zero because the insertion via SP
            Assert.IsTrue((int)(auditEvent.EntityFrameworkEvent.Entries[0].PrimaryKey["Id"]) == 0);
            Assert.IsTrue((int)(auditEvent.EntityFrameworkEvent.Entries[0].ColumnValues["Id"]) == 0);
            Assert.AreEqual("Insert", auditEvent.EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual("Blogs", auditEvent.EntityFrameworkEvent.Entries[0].Table);
            Assert.AreEqual(title, auditEvent.EntityFrameworkEvent.Entries[0].ColumnValues["Title"]);
            Assert.IsTrue(auditEvent.Environment.StackTrace.Contains("Test_FunctionMapping"));
        }

        [Test]
        public void Test_Delete_Ignored()
        {
            bool neverTrue = false;
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
                {
                    neverTrue = true;
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(x => x.OnScopeCreated(sc =>
                {
                    var efEvent = sc.Event.GetEntityFrameworkEvent();
                }));

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(config => config
                    .IncludeEntityObjects(false)
                    .AuditEventType("{context}:{database}"))
                .UseOptOut()
                .IgnoreAny(x =>
                {
                    Assert.AreEqual("Post", x.Name);
                    return x.Name == "Post";
                });

            using (var ctx = new BlogsEntities())
            {
                var post = new Post()
                {
                    DateCreated = DateTime.Now,
                    Content = "test-content-x",
                    BlogId = 1
                };
                ctx.Posts.Add(post);
                ctx.SaveChanges();
            }

            Assert.False(neverTrue);
        }

        [Test]
        public void Test_General()
        {
            var guid = Guid.NewGuid().ToString();
            var evs = new List<AuditEvent>();
            var entities = new List<Post>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(x => x.
                    IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .IncludeStackTrace()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        var p = ev.GetEntityFrameworkEvent().Entries[0].Entity as Post;
                        p = JsonSerializer.Deserialize<Post>(JsonSerializer.Serialize(p, new JsonSerializerOptions() {  ReferenceHandler = ReferenceHandler.IgnoreCycles } ));
                        entities.Add(p);
                        evs.Add(ev);
                    }));
            using (var ctx = new BlogsEntities())
            {
                var post = new Post()
                {
                    //Id = 1,
                    DateCreated = DateTime.Now,
                    Content = "test-content",
                    BlogId = 1
                };
                ctx.Posts.Add(post);
                ctx.SaveChanges();

                var postProxy = ctx.Posts.First();
                postProxy.Content = guid;
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);

            Assert.IsTrue((int)(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"]) > 0);
            Assert.IsTrue((int)(evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"]) > 0);
            Assert.AreEqual(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"], evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"]);
            Assert.AreEqual("test-content", evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Content"]);
            Assert.AreEqual(guid, evs[1].GetEntityFrameworkEvent().Entries[0].ColumnValues["Content"]);

            Assert.AreEqual("Posts", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("Posts", evs[1].GetEntityFrameworkEvent().Entries[0].Table);

            Assert.AreEqual(2, entities.Count);

            var p1 = entities[0];
            var p2 = entities[1];

            Assert.AreEqual("test-content", p1.Content);
            Assert.AreEqual(guid, p2.Content);
            Assert.IsTrue(evs[0].Environment.StackTrace.Contains(nameof(Test_General)));
        }


        [Test]
        public void Test_General_IdentityContext()
        {
            System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<Entities>());

            var guid = Guid.NewGuid().ToString();
            var evs = new List<AuditEvent>();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<Entities>(x => x.
                    IncludeEntityObjects(true));
            Audit.Core.Configuration.Setup()
                .IncludeStackTrace()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            using (var ctx = new Entities())
            {
                var ev = new Event()
                {
                    EventId = 123,
                    Data = "test-content",
                    InsertedDate = DateTime.Now,
                    LastUpdatedDate = DateTime.Now
                };
                ctx.Events.Add(ev);
                ctx.SaveChanges();

                var evProxy = ctx.Events.First();
                evProxy.Data = guid;
                ctx.SaveChanges();
            }

            Assert.AreEqual(2, evs.Count);

            Assert.AreEqual("Events", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("Events", evs[1].GetEntityFrameworkEvent().Entries[0].Table);

            var p1 = evs[0].GetEntityFrameworkEvent().Entries[0].Entity as Event;
            var p2 = evs[1].GetEntityFrameworkEvent().Entries[0].Entity as Event;

            Assert.AreEqual("test-content", p1.Data);
            Assert.AreEqual(guid, p2.Data);
            Assert.IsTrue(evs[0].Environment.StackTrace.Contains(nameof(Test_General_IdentityContext)));
        }
    }
}
