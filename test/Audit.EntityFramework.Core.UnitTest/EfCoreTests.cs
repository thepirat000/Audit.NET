using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Threading.Tasks;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category("Integration")]
    [Category("SqlServer")]
    public class EfCoreTests
    {
        [OneTimeSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
            new BlogsContext().Database.EnsureCreated();
            new DemoContext().Database.EnsureCreated();
        }

#if EF_CORE_8_OR_GREATER
        [Test]
        public void Test_EF_ComplexType()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForContext<Context_ComplexTypes>(c => c
                    .ForEntity<Context_ComplexTypes.Address>(a => a.Format(p => p.City, city => $"*{city}*"))
                    .ForEntity<Context_ComplexTypes.Country>(a => a.Format(p => p.Alias, alias => alias?.ToUpperInvariant())));

            using var context = new Context_ComplexTypes();
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(ev.GetEntityFrameworkEvent());
                }));

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var person = new Context_ComplexTypes.Person()
            {
                Id = 1,
                Name = "Development",
                Address = new Context_ComplexTypes.Address()
                {
                    Country = new Context_ComplexTypes.Country() { Name = "Austria", Alias = "Au" },
                    City = "Vienna",
                    Line1 = "Street",
                    PostCode = "1234"
                }
            };

            context.People.Add(person);
            context.SaveChanges();

            person.Name = "New Name";
            person.Address = person.Address with { City = "NewCity", Country = person.Address.Country with { Alias = "newalias" } };
            context.SaveChanges();

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(1));
            Assert.That(evs[1].Entries.Count, Is.EqualTo(1));

            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[1].Entries[0].Action, Is.EqualTo("Update"));

            Assert.That(evs[0].Entries[0].ColumnValues["Name"], Is.EqualTo("Development"));
            Assert.That(evs[1].Entries[0].ColumnValues["Name"], Is.EqualTo("New Name"));

            Assert.That(evs[0].Entries[0].ColumnValues["Address_City"], Is.EqualTo("*Vienna*"));
            Assert.That(evs[1].Entries[0].ColumnValues["Address_City"], Is.EqualTo("*NewCity*"));

            Assert.That(evs[0].Entries[0].ColumnValues["Address_Country_Alias"], Is.EqualTo("AU"));
            Assert.That(evs[1].Entries[0].ColumnValues["Address_Country_Alias"], Is.EqualTo("NEWALIAS"));

            Assert.That(evs[1].Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Address_Country_Alias")?.OriginalValue, Is.EqualTo("AU"));
            Assert.That(evs[1].Entries[0].Changes.FirstOrDefault(ch => ch.ColumnName == "Address_Country_Alias")?.NewValue, Is.EqualTo("NEWALIAS"));
        }
#endif

#if EF_CORE_5_OR_GREATER

        [Test]
        public void Test_EF_Core_TablePerTypeConfig()
        {
            var evs = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup().UseDynamicProvider(_ => _.OnInsert(ev =>
            {
                evs.Add(ev.GetEntityFrameworkEvent());
            }));

            var cfg = new DbContextOptionsBuilder<TptConfigContext>().UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=TptConfigTests;Trusted_Connection=True;ConnectRetryCount=0");
            var ctx = new TptConfigContext(cfg.Options);
            var guid = Guid.NewGuid().ToString();

            // INSERT
            ctx.ReservationRequests.Add(new ReservationRequest()
            {
                LocationId = guid,
                UserId = "u",
                ReservationComments = "test",
                ReservationTo = DateTime.UtcNow
            });

            ctx.SaveChanges();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Insert"));
            Assert.That(evs[0].Entries[0].Table, Is.EqualTo("ReservationRequests"));
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "Id"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "LocationId" && (string)cv.Value == guid), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "Uid" && (string)cv.Value == "u"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "ReservationComments" && (string)cv.Value == "test"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "ReservationTo"), Is.True);

            evs.Clear();

            // UPDATE
            var r = ctx.ReservationRequests.FirstOrDefault(r => r.LocationId == guid);
            var newGuid = Guid.NewGuid().ToString();
            r.LocationId = newGuid;
            r.UserId = "u2";
            ctx.SaveChanges();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries[0].Action, Is.EqualTo("Update"));
            Assert.That(evs[0].Entries[0].Table, Is.EqualTo("ReservationRequests"));
            Assert.That(evs[0].Entries[0].Changes.Count, Is.EqualTo(2));
            Assert.That(evs[0].Entries[0].Changes.Any(ch => ch.ColumnName == "Uid" && (string)ch.OriginalValue == "u" && (string)ch.NewValue == "u2"), Is.True);
            Assert.That(evs[0].Entries[0].Changes.Any(ch => ch.ColumnName == "LocationId" && (string)ch.OriginalValue == guid && (string)ch.NewValue == newGuid), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "Id"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "LocationId" && (string)cv.Value == newGuid), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "Uid" && (string)cv.Value == "u2"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "ReservationComments" && (string)cv.Value == "test"), Is.True);
            Assert.That(evs[0].Entries[0].ColumnValues.Any(cv => cv.Key == "ReservationTo"), Is.True);
        }

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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(5));
            Assert.That(evs[0].Entries.Any(e => e.Table == "Tags" && e.Action == "Insert" && e.ColumnValues["Text"]?.ToString() == "tag 1"), Is.True);
            Assert.That(evs[0].Entries.Any(e => e.Table == "Tags" && e.Action == "Insert" && e.ColumnValues["Text"]?.ToString() == "tag 2"), Is.True);
            Assert.That(evs[0].Entries.Any(e => e.Table == "Posts" && e.Action == "Insert" && e.ColumnValues["Name"]?.ToString() == "test"), Is.True);
            Assert.That(evs[0].Entries.Any(e => e.Table == "PostTag" && e.Action == "Insert" && e.ColumnValues["PostsId"]?.ToString() == "10" && e.ColumnValues["TagsId"]?.ToString() == "101"), Is.True);
            Assert.That(evs[0].Entries.Any(e => e.Table == "PostTag" && e.Action == "Insert" && e.ColumnValues["PostsId"]?.ToString() == "10" && e.ColumnValues["TagsId"]?.ToString() == "102"), Is.True);
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Entries.Count, Is.EqualTo(4));
            Assert.That(evs[0].Entries.First(e => e.Table == "Department" && e.ColumnValues.ContainsKey("Name")).ColumnValues["Name"], Is.EqualTo("deparment test"));
            Assert.That(evs[0].Entries.First(e => e.Table == "Person" && e.ColumnValues.ContainsKey("Name")).ColumnValues["Name"], Is.EqualTo("person test"));
            Assert.That(evs[0].Entries.First(e => e.Table == "Department" && e.ColumnValues.ContainsKey("Address_City")).ColumnValues["Address_City"], Is.EqualTo("Vienna1"));
            Assert.That(evs[0].Entries.First(e => e.Table == "Person_Addresses" && e.ColumnValues.ContainsKey("City")).ColumnValues["City"], Is.EqualTo("Vienna2"));
        }
#endif

        [Test]
        public void Test_EF_StackTrace()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsert(ev =>
                {
                    evs.Add(ev);
                }));
            Audit.Core.Configuration.IncludeStackTrace = true;

            var id1 = Guid.NewGuid().ToString().Substring(0, 8);
            var id2 = Guid.NewGuid().ToString().Substring(0, 8);
            using (var context = new BlogsContext())
            {
                var blog1 = context.Blogs.Add(new Blog() { Title = id1 });
                context.SaveChanges();
            }
            Audit.Core.Configuration.IncludeStackTrace = false;

            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(evs[0].Environment.StackTrace.Contains(nameof(Test_EF_StackTrace)), Is.True, $"Expected contains {nameof(Test_EF_StackTrace)} but was {evs[0].Environment.StackTrace}");
            
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
                context.ExcludeTransactionId = false;
                using (var tran = context.Database.BeginTransaction())
                {
                    var blog = context.Blogs.First();
                    blog.Title = id;
                    context.SaveChanges();
                    tran.Commit();
                }
            }

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(!string.IsNullOrWhiteSpace(evs[0].GetEntityFrameworkEvent().TransactionId), Is.True);
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().TransactionId, Is.Null);
            Assert.That(evs[0].GetEntityFrameworkEvent().AmbientTransactionId, Is.Null);
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().TransactionId, Is.Null);
            Assert.That(evs[0].GetEntityFrameworkEvent().AmbientTransactionId, Is.Null);

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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].Entity.GetType().FullName.StartsWith("Castle.Proxies."), Is.True);
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"], Is.EqualTo(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"]));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Title"], Is.EqualTo(guid));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].Table, Is.EqualTo("Blogs"));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].Schema.ToLower(), Is.EqualTo("dbo"));
            Assert.AreEqual(1, (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).x);
            Assert.AreEqual("two", (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).y);
            Assert.AreEqual("one", (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).y);
            Assert.AreEqual(2, (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).x);
            Assert.That(ev2.EntityFrameworkEvent.CustomFields["Additional Field On event"], Is.Not.Null);
            Assert.That(ev2.EntityFrameworkEvent.Entries[0].CustomFields["Additional Field On entry"], Is.Not.Null);
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

            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Id"], Is.EqualTo(evs[0].GetEntityFrameworkEvent().Entries[0].PrimaryKey["Id"]));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].ColumnValues["Title"], Is.EqualTo(guid));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].Table, Is.EqualTo("Blogs"));
            Assert.That(evs[0].GetEntityFrameworkEvent().Entries[0].Schema.ToLower(), Is.EqualTo("dbo"));
            Assert.AreEqual(1, (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).x);
            Assert.AreEqual("two", (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).y);
            Assert.AreEqual("one", (evs[0].GetEntityFrameworkEvent().CustomFields["Additional Field On event"] as dynamic).y);
            Assert.AreEqual(2, (evs[0].GetEntityFrameworkEvent().Entries[0].CustomFields["Additional Field On entry"] as dynamic).x);
            Assert.That(ev2.EntityFrameworkEvent.CustomFields["Additional Field On event"], Is.Not.Null);
            Assert.That(ev2.EntityFrameworkEvent.Entries[0].CustomFields["Additional Field On entry"], Is.Not.Null);
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

            Assert.That(events.Count, Is.EqualTo(4));
            Assert.That(events[0].Entries.Count, Is.EqualTo(1));
            Assert.That(events[0].Entries[0].ColumnValues["Id"], Is.EqualTo(id));
            Assert.That(events[0].Entries[0].ColumnValues["Name"], Is.EqualTo("tenant"));
            Assert.That(events[0].Entries[0].PrimaryKey["Id"], Is.EqualTo(id));

            Assert.That(events[1].Entries.Count, Is.EqualTo(2));
            Assert.That(events[1].Entries[0].ColumnValues["Id"], Is.EqualTo(id + 1));
            Assert.That(events[1].Entries[0].ColumnValues["Name"], Is.EqualTo("test1"));
            Assert.That(events[1].Entries[0].ColumnValues["TenantId"], Is.EqualTo(id));
            Assert.That(events[1].Entries[0].PrimaryKey.Count, Is.EqualTo(2));
            Assert.That(events[1].Entries[0].PrimaryKey["Id"], Is.EqualTo(id + 1));
            Assert.That(events[1].Entries[0].PrimaryKey["TenantId"], Is.EqualTo(id));

            Assert.That(events[1].Entries[1].ColumnValues["Id"], Is.EqualTo(id + 2));
            Assert.That(events[1].Entries[1].ColumnValues["Name"], Is.EqualTo("test2"));
            Assert.That(events[1].Entries[1].ColumnValues["TenantId"], Is.EqualTo(id));
            Assert.That(events[1].Entries[1].PrimaryKey.Count, Is.EqualTo(2));
            Assert.That(events[1].Entries[1].PrimaryKey["Id"], Is.EqualTo(id + 2));
            Assert.That(events[1].Entries[1].PrimaryKey["TenantId"], Is.EqualTo(id));

            Assert.That(events[2].Entries.Count, Is.EqualTo(1));
            Assert.That(events[2].Entries[0].ColumnValues["Id"], Is.EqualTo(id + 3));
            Assert.That(events[2].Entries[0].ColumnValues["EmployeeId"], Is.EqualTo(id + 1));
            Assert.That(events[2].Entries[0].ColumnValues["TenantId"], Is.EqualTo(id));
            Assert.That(events[2].Entries[0].ColumnValues["TrusteeId"], Is.EqualTo(id + 2));
            Assert.That(events[2].Entries[0].PrimaryKey["Id"], Is.EqualTo(id + 3));

            Assert.That(events[3].Entries.Count, Is.EqualTo(1));
            Assert.That(events[3].Entries[0].Changes.Count, Is.EqualTo(1));
            Assert.That(events[3].Entries[0].Changes[0].ColumnName, Is.EqualTo("Name"));
            Assert.That(events[3].Entries[0].Changes[0].OriginalValue, Is.EqualTo("test1"));
            Assert.That(events[3].Entries[0].Changes[0].NewValue, Is.EqualTo("test1-updated"));

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
                            entity.AuditUser = "test user";
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

                Assert.That(audits.Count, Is.EqualTo(2));
                Assert.That(audits[0].EntityType, Is.EqualTo("Blog"));
                Assert.That(audits[0].EntityId, Is.EqualTo(blog.Id));
                Assert.That(audits[0].Title, Is.EqualTo(blog.Title));

                Assert.That(audits[1].EntityType, Is.EqualTo("Post"));
                Assert.That(audits[1].EntityId, Is.EqualTo(post.Id));
                Assert.That(audits[1].Title, Is.EqualTo(post.Title));

                Assert.That(audits[0].AuditUser, Is.EqualTo("test user"));
                Assert.That(audits[1].AuditUser, Is.EqualTo("test user"));
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
                        entity.AuditAction = "test";
                        entity.Group = guid;
                        entity.EntityId = (int)entry.PrimaryKey.First().Value;
                        entity.EntityType = entry.EntityType.Name;
                        entity.AuditDate = DateTime.Now;
                        entity.AuditUser = "test user";
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

                Assert.That(audits.Count, Is.EqualTo(2));
                Assert.That(audits[0].EntityType, Is.EqualTo("Blog"));
                Assert.That(audits[0].EntityId, Is.EqualTo(blog.Id));
                Assert.That(audits[0].Title, Is.EqualTo(blog.Title));

                Assert.That(audits[1].EntityType, Is.EqualTo("Post"));
                Assert.That(audits[1].EntityId, Is.EqualTo(post.Id));
                Assert.That(audits[1].Title, Is.EqualTo(post.Title));

                Assert.That(audits[0].AuditUser, Is.EqualTo("test user"));
                Assert.That(audits[1].AuditUser, Is.EqualTo("test user"));
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
                                Assert.That(entity.GetType(), Is.EqualTo(typeof(BlogAudit)));
                                var ba = entity as BlogAudit;
                                ba.BlogId = (int)entry.PrimaryKey.First().Value;
                            }
                            else
                            {
                                Assert.That(entity.GetType(), Is.EqualTo(typeof(CommonAudit)));
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
                            entity.AuditUser = "test user";
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

                Assert.That(blogaudits, Is.Not.Null);
                Assert.That(blogaudits.Count, Is.EqualTo(1));
                Assert.That(blogaudits[0].BlogId, Is.EqualTo(blog.Id));
                Assert.That(audits.Count, Is.EqualTo(1));
                Assert.That(audits[0].EntityType, Is.EqualTo("Blog"));
                Assert.That(audits[0].EntityId, Is.EqualTo(blog.Id));
                Assert.That(audits[0].Title, Is.EqualTo("TestBlog"));
                Assert.That(audits[0].AuditUser, Is.EqualTo("test user"));
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
                            entity.AuditUser = "test user";
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

                Assert.That(audits.Count, Is.EqualTo(2));
                Assert.That(audits[0].Title, Is.EqualTo("Test_EFFailureLogging"));
                Assert.That(audits[1].Title, Is.EqualTo(longText));
                Assert.That(audits[1].Exception.Length > 5, Is.True);
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
                        entity.AuditAction = "test";
                        entity.Group = guid;
                        entity.EntityId = (int)entry.PrimaryKey.First().Value;
                        entity.EntityType = entry.EntityType.Name;
                        entity.AuditDate = DateTime.Now;
                        entity.AuditUser = "test user";
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

                Assert.That(audits.Count, Is.EqualTo(2));
                Assert.That(audits[0].EntityType, Is.EqualTo("Blog"));
                Assert.That(audits[0].EntityId, Is.EqualTo(blog.Id));
                Assert.That(audits[0].Title, Is.EqualTo(blog.Title));

                Assert.That(audits[1].EntityType, Is.EqualTo("Post"));
                Assert.That(audits[1].EntityId, Is.EqualTo(post.Id));
                Assert.That(audits[1].Title, Is.EqualTo($"F:TestPost"));

                Assert.That(audits[0].AuditUser, Is.EqualTo("test user"));
                Assert.That(audits[1].AuditUser, Is.EqualTo("test user"));
            }
        }

        [AuditInclude]
        public class ProxyPost : Post 
        {
           public override string Title { get => base.Title; set => base.Title = $"F:{value}"; }
        }
    }
}