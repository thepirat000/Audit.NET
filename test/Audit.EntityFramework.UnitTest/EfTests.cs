using Audit.Core;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audit.EntityFramework.UnitTest
{
    [TestFixture]
    public class EfTests
    {
        [SetUp]
        public void SetUp()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>().Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<Entities>().Reset();
        }

        [Test]
        public void Test_FunctionMapping()
        {
            AuditEventEntityFramework auditEvent = null;
            Audit.Core.Configuration.Setup()
               .UseDynamicProvider(x => x.OnInsertAndReplace(ev =>
               {
                   auditEvent = ev as AuditEventEntityFramework;
               }))
               .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<BlogsEntities>(config => config
                    .IncludeEntityObjects(false)
                    .AuditEventType("{context}:{database}"))
                .Reset()
                .UseOptOut();
            var title = Guid.NewGuid().ToString();
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

            Assert.AreEqual(1, auditEvent.EntityFrameworkEvent.Entries.Count);
            Assert.AreEqual("Insert", auditEvent.EntityFrameworkEvent.Entries[0].Action);
            Assert.AreEqual("Blogs", auditEvent.EntityFrameworkEvent.Entries[0].Table);
            Assert.AreEqual(title, auditEvent.EntityFrameworkEvent.Entries[0].ColumnValues["Title"]);
            Assert.IsTrue(auditEvent.Environment.CallingMethodName.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
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
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        var p = ev.GetEntityFrameworkEvent().Entries[0].Entity as Post;
                        p = JsonConvert.DeserializeObject<Post>(JsonConvert.SerializeObject(p, new JsonSerializerSettings() {  ReferenceLoopHandling = ReferenceLoopHandling.Ignore } ));
                        entities.Add(p);
                        evs.Add(ev);
                    }));
            using (var ctx = new BlogsEntities())
            {
                var post = new Post()
                {
                    Id = 1,
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

            Assert.AreEqual("Posts", evs[0].GetEntityFrameworkEvent().Entries[0].Table);
            Assert.AreEqual("Posts", evs[1].GetEntityFrameworkEvent().Entries[0].Table);

            Assert.AreEqual(2, entities.Count);

            var p1 = entities[0];
            var p2 = entities[1];

            Assert.AreEqual("test-content", p1.Content);
            Assert.AreEqual(guid, p2.Content);
            Assert.IsTrue(evs[0].Environment.CallingMethodName.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
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
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }));
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
            Assert.IsTrue(evs[0].Environment.CallingMethodName.Contains(new System.Diagnostics.StackTrace().GetFrame(0).GetMethod().Name));
        }
    }
}
