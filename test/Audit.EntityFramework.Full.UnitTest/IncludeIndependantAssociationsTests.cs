using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using Audit.IntegrationTest;

namespace Audit.EntityFramework.Full.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
    public class IncludeIndependantAssociationsTests
    {
        [Test]
        public void Test_ManyToManyNoJoinEntity()
        {
            using (var context = new IndependantAssociationsContext())
            {
                context.AuditDisabled = true;
                context.Database.CreateIfNotExists();
                var event1 = context.Event.FirstOrDefault(e => e.EventId == 1);
                if (event1 == null)
                {
                    event1 = new Event() { EventId = 1 };
                    context.Event.Add(event1);
                    context.SaveChanges();
                }
            }

            var events = new List<EntityFrameworkEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(c => c.OnInsertAndReplace(ev =>
                {
                    events.Add(ev.GetEntityFrameworkEvent());
                }));
            var guid = Guid.NewGuid().ToString();
            using (var context = new IndependantAssociationsContext())
            {
                context.EventAvailabilityResponse.Add(new EventAvailabilityResponse()
                {
                    EventAvailabilityResponseId = guid,
                    Comment = "test100",
                    PreferredEventLocations = new List<EventLocation>() { new EventLocation() { EventLocationId = 1234, EventId = 1, Location = "loc from code" } }
                });
                context.SaveChanges();
            }
            using (var context = new IndependantAssociationsContext())
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

        public class IndependantAssociationsContext : AuditDbContext
        {
            public IndependantAssociationsContext() : base(TestHelper.GetConnectionString("IndependantAssociations"))
            {
                this.IncludeIndependantAssociations = true;
            }

            protected override void OnModelCreating(DbModelBuilder modelBuilder)
            {
                modelBuilder.Configurations.Add(new EventLocationConfiguration());
                Database.SetInitializer<IndependantAssociationsContext>(null);
            }

            public DbSet<EventLocation> EventLocation { get; set; }
            public DbSet<EventAvailabilityResponse> EventAvailabilityResponse { get; set; }
            public DbSet<Event> Event { get; set; }
        }

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

    }
}
