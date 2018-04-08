#if NET451
using Audit.Core;
using Audit.Core.Providers;
using Audit.EntityFramework;
using Audit.EntityFramework.Providers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.IntegrationTest
{
    [TestFixture(Category ="EF")]
    public class EntityFrameworkTests_Net45
    {
        [SetUp]
        public void Setup()
        {
            Audit.EntityFramework.Configuration.Setup()
                .ForAnyContext().Reset();
        }

        [Test]
        public void Test_ManyToManyNoJoinEntity()
        {
            using (var context = new Issue78Context())
            {
                context.Database.ExecuteSqlCommand(@"
if not exists (select * from sysobjects where name = 'Events')
create table [Events]
(
	[EventId] int not null primary key
)

if not exists (select * from sysobjects where name = 'EventLocations')
create table EventLocations
(
	EventLocationId int identity(1,1) not null primary key,
	[Location] NVARCHAR(MAX) not null,
	[EventId] int not null,
	CONSTRAINT FK_EventLocations_ev FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId])

)

if not exists (select * from sysobjects where name = 'EventAvailabilityResponses')
create table EventAvailabilityResponses
(
	EventAvailabilityResponseId NVARCHAR(36) NOT NULL PRIMARY KEY,
	Comment NVARCHAR(4000) NOT NULL
)

if not exists (select * from sysobjects where name = 'EventAvailabilityResponseEventLocations')
create table EventAvailabilityResponseEventLocations
(
	EventLocationId int not null,
	EventAvailabilityResponseId NVARCHAR(36) NOT NULL,
	CONSTRAINT PK_EventAvailabilityResponseEventLocations PRIMARY KEY (EventLocationId, EventAvailabilityResponseId),
	CONSTRAINT FK_EventAvailabilityResponseEventLocations_Loc FOREIGN KEY (EventLocationId) REFERENCES EventLocations (EventLocationId),
	CONSTRAINT FK_EventAvailabilityResponseEventLocations_Res FOREIGN KEY (EventAvailabilityResponseId) REFERENCES EventAvailabilityResponses (EventAvailabilityResponseId)
)
if not exists ( select * from [Events] where [EventId] = 1)
    insert into [events] values (1)
");
            }

            var events = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsertAndReplace(ev =>
                {
                    events.Add(ev.GetEntityFrameworkEvent());
                }));
            var guid = Guid.NewGuid().ToString();
            using (var context = new Issue78Context())
            {

                context.EventAvailabilityResponse.Add(new EventAvailabilityResponse()
                {
                    EventAvailabilityResponseId = guid,
                    Comment = "test100",
                    PreferredEventLocations = new List<EventLocation>() { new EventLocation() { EventLocationId = 1234, EventId = 1, Location = "loc from code" } }
                });
                context.SaveChanges();
            }
            using (var context = new Issue78Context())
            {
                var r = context.EventAvailabilityResponse.First(x => x.EventAvailabilityResponseId == guid);
                r.PreferredEventLocations.Clear();
                context.SaveChanges();
            }

            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(1, events[0].Associations.Count);
            Assert.AreEqual(2, events[0].Entries.Count);
            Assert.AreEqual(1, events[1].Associations.Count);
            Assert.AreEqual(0, events[1].Entries.Count);
            Assert.AreEqual("Insert", events[0].Associations[0].Action);
            Assert.AreEqual("Delete", events[1].Associations[0].Action);
            Assert.AreEqual(events[0].Associations[0].Records[0].PrimaryKey["EventLocationId"], events[1].Associations[0].Records[0].PrimaryKey["EventLocationId"]);
            Assert.AreEqual(events[0].Associations[0].Records[1].PrimaryKey["EventAvailabilityResponseId"], events[1].Associations[0].Records[1].PrimaryKey["EventAvailabilityResponseId"]);
            Assert.AreNotEqual(1234, (int)events[0].Associations[0].Records[0].PrimaryKey["EventLocationId"]);
        }

        [Test]
        public void Test_EFDataProvider()
        {
            Audit.Core.Configuration.Setup()
                .UseEntityFramework(x => x
                    .AuditTypeExplicitMapper(_ => _
                        .Map<Order, OrderAudit>((order, auditOrder) => { auditOrder.Status = "Order-" + order.Status; })
                        .Map<Orderline, OrderlineAudit>()
                        .AuditEntityAction<AuditBase>((ev, ent, auditEntity) =>
                        {
                            auditEntity.AuditDate = DateTime.UtcNow;
                            auditEntity.UserName = ev.Environment.UserName;
                            auditEntity.AuditStatus = ent.Action;
                        })));

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
                ctx.Set(typeof(Order)).Add(o);
                ctx.SaveChanges();
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var orderAudit = ctx.OrderAudit.AsNoTracking().SingleOrDefault(a => a.Id.Equals(order.Id));
                Assert.NotNull(orderAudit);
                var orderlineAudits = ctx.OrderlineAudit.AsNoTracking().Where(a => a.OrderId.Equals(order.Id)).ToList();
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
                Assert.AreEqual("Order-Cancelled", orderAudits[0].Status);
                Assert.AreEqual("Order-Pending", orderAudits[1].Status);
                Assert.AreEqual("Insert", orderAudits[1].AuditStatus);
            }

            using (var ctx = new AuditPerTableContext())
            {
                var order = ctx.Order.Single(a => a.Number.Equals(id));
                var ol = ctx.Orderline.Single(a => a.OrderId.Equals(order.Id) && a.Product.StartsWith("p1"));
                ctx.Orderline.Remove(ol);
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
        public void Test_EF_Attach()
        {
            EntityFrameworkEvent efEvent = null;
            Audit.Core.Configuration.Setup().UseDynamicProvider(x => x
                .OnInsertAndReplace(ev => {
                    efEvent = ev.GetEntityFrameworkEvent();
                }));
            using (var ctx = new MyAuditedVerboseContext())
            {
                var blog = new Blog()
                {
                    Id = 1,
                    BloggerName = "def-22"
                };
                blog.Title = "Changed-" + Guid.NewGuid();
                ctx.Blogs.Attach(blog);
                ctx.Entry(blog).State = EntityState.Modified;
                ctx.SaveChanges();
            }
            Assert.AreEqual(1, efEvent.Entries.Count);
            Assert.AreEqual("Update", efEvent.Entries[0].Action);
            Assert.IsTrue(efEvent.Entries[0].Changes.Any(ch => ch.ColumnName == "Title"));
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

            using (var ctx = new MyAuditedVerboseContext())
            {
                using (var transaction = ctx.Database.BeginTransaction())
                {
                    ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 1", DateCreated = DateTime.Now, Title = "other title 1" });
                    ctx.SaveChanges();
                    ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 2", DateCreated = DateTime.Now, Title = "other title 2" });
                    ctx.SaveChanges();
                    transaction.Rollback();
                }
                ctx.Posts.Add(new Post() { BlogId = 1, Content = "other content 3", DateCreated = DateTime.Now, Title = "other title 3" });
                ctx.SaveChanges();
            }
            var ev1 = (auditEvents[0] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev2 = (auditEvents[1] as AuditEventEntityFramework).EntityFrameworkEvent;
            var ev3 = (auditEvents[2] as AuditEventEntityFramework).EntityFrameworkEvent;
            Assert.NotNull(ev1.TransactionId);
            Assert.NotNull(ev2.TransactionId);
            Assert.Null(ev3.TransactionId);
            Assert.AreEqual(ev1.TransactionId, ev2.TransactionId);

            Audit.Core.Configuration.ResetCustomActions();
        }

        [Test]
        public void Test_EF_SaveChangesSync()
        {
            Test_EF_Actions();
        }

        [Test]
        public async Task Test_EF_SaveChangesAsync()
        {
            await Test_EF_ActionsAsync();
        }

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

            Database.SetInitializer<MyAuditedVerboseContext>(new CreateDatabaseIfNotExists<MyAuditedVerboseContext>());

            using (var ctx = new MyAuditedVerboseContext())
            {
                //ctx.Database.EnsureCreated();
                ctx.Database.ExecuteSqlCommand(@"
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
                ctx.Entry(pr).State = EntityState.Deleted;

                int result = ctx.SaveChanges();

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

        public async Task Test_EF_ActionsAsync()
        {
            var provider = new Mock<AuditDataProvider>();
            AuditEvent auditEvent = null;
            provider.Setup(x => x.InsertEventAsync(It.IsAny<AuditEvent>())).ReturnsAsync((AuditEvent ev) =>
            {
                auditEvent = ev;
                return Task.FromResult(Guid.NewGuid());
            });
            provider.Setup(p => p.Serialize(It.IsAny<object>())).Returns((object obj) => obj);

            Audit.Core.Configuration.Setup()
                .UseCustomProvider(provider.Object);

            Database.SetInitializer<MyAuditedVerboseContext>(new CreateDatabaseIfNotExists<MyAuditedVerboseContext>());

            using (var ctx = new MyAuditedVerboseContext())
            {
                //ctx.Database.EnsureCreated();
                await ctx.Database.ExecuteSqlCommandAsync(@"
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

                var postsblog1 = await ctx.Blogs.Include(x => x.Posts)
                    .FirstOrDefaultAsync(x => x.Id == 1);
                postsblog1.BloggerName += "-22";

                ctx.Posts.Add(new Post() { BlogId = 1, Content = "content", DateCreated = DateTime.Now, Title = "title" });

                var ch1 = await ctx.Posts.FirstOrDefaultAsync(x => x.Id == 1);
                ch1.Content += "-code";

                var pr = await ctx.Posts.FirstOrDefaultAsync(x => x.Id == 5);
                ctx.Entry(pr).State = EntityState.Deleted;

                int result = await ctx.SaveChangesAsync();

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
        public OtherContextFromDbContext()
            : base("data source=localhost;initial catalog=Blogs;integrated security=true;")
        { }
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }
    //[AuditDbContext(IncludeEntityObjects = false, Mode = AuditOptionMode.OptOut)]
    public class MyUnauditedContext : MyBaseContext
    {
        public override bool AuditDisabled
        {
            get { return true; }
            set { }
        }
    }

    [AuditDbContext(IncludeEntityObjects = true, AuditEventType = "{database}_{context}")]
    public class MyAuditedVerboseContext : MyBaseContext
    {

    }

    [AuditDbContext(IncludeEntityObjects = false, AuditEventType = "{database}_{context}")]
    public class MyAuditedContext : MyBaseContext
    {

    }

    public class MyBaseContext : AuditDbContext
    {
        public override bool AuditDisabled { get; set; }

        public MyBaseContext()
            : base("data source=localhost;initial catalog=Blogs;integrated security=true;")
        {
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
                && auditScope.Event.GetEntityFrameworkEvent().Entries[0].ColumnValues["BloggerName"] == "ROLLBACK")
            {
                Database.CurrentTransaction.Rollback();
            }
            else
            {
                Database.CurrentTransaction.Commit();
            }
        }
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
    [AuditDbContext(IncludeEntityObjects = true)]
    public class AuditPerTableContext : AuditDbContext
    {
        public AuditPerTableContext()
            : base("data source=localhost;initial catalog=Audit;integrated security=true;")
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


    #region ManyToManyTest
    public class EventAvailabilityResponse
    {
        public EventAvailabilityResponse()
        {
            EventAvailabilityResponseId = Guid.NewGuid().ToString();
        }

        [Key]
        [StringLength(36)]
        public string EventAvailabilityResponseId { get; set; }

        [StringLength(4000)]
        public string Comment { get; set; }
        public virtual ICollection<EventLocation> PreferredEventLocations { get; set; }
    }

    public class EventLocation
    {
        public EventLocation()
        {
            Location = string.Empty;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventLocationId { get; set; }

        [Required]
        public string Location { get; set; }

        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        public virtual ICollection<EventAvailabilityResponse> EventAvailabilityResponses { get; set; }

    }

    public class Event
    {
        public int EventId { get; set; }
        //public string Text { get; set; }
    }

    internal class EventLocationConfiguration : EntityTypeConfiguration<EventLocation>
    {
        internal EventLocationConfiguration()
        {
            ToTable("EventLocations");

            HasMany(e => e.EventAvailabilityResponses)
                .WithMany(e => e.PreferredEventLocations)
                .Map(m => m.ToTable("EventAvailabilityResponseEventLocations")
                .MapLeftKey("EventLocationId").MapRightKey("EventAvailabilityResponseId"));
        }
    }


    [AuditDbContext(IncludeEntityObjects = false, IncludeIndependantAssociations = true)]
    public class Issue78Context : AuditDbContext
    {
        public Issue78Context()
            : base("data source=localhost;initial catalog=Audit;integrated security=true;")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new EventLocationConfiguration());
            Database.SetInitializer<Issue78Context>(null);
        }

        public DbSet<EventLocation> EventLocation { get; set; }
        public DbSet<EventAvailabilityResponse> EventAvailabilityResponse { get; set; }
        public DbSet<Event> Event { get; set; }
    }
    #endregion
}
#endif