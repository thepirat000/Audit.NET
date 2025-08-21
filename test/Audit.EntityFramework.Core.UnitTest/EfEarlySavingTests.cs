using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Audit.Core;
using Audit.IntegrationTest;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.SqlServer)]
    public class EfEarlySavingTests
    {
        class Message
        {
            public Guid MessageId { get; set; }

            public string Sender { get; set; }

            public string Receiver { get; set; }

            public string Content { get; set; }
        }

        class MessageAudit
        {
            [Key] public Guid AuditLogId { get; set; }

            [Required] public string AuditData { get; set; }

            public DateTimeOffset AuditTimestamp { get; set; }

            public string AuditAction { get; set; }

            public Guid MessageId { get; set; }
        }

        class TestContext : AuditDbContext
        {
            private static string CnnString = TestHelper.GetConnectionString("TransactionTestEfCore");

            public DbSet<Message> Messages { get; set; }
            public DbSet<MessageAudit> MessageAudits { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
                => optionsBuilder.UseSqlServer(CnnString);
        }

        [OneTimeSetUp]
        public async Task SetUp()
        {
            using (var context = new TestContext())
            {
                await context.Database.EnsureCreatedAsync();
            }
        }


        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd)]
        [TestCase(EventCreationPolicy.Manual)]
        public async Task SavingAudit_ByCreationPolicy(EventCreationPolicy eventCreationPolicy)
        {
            // InsertOnEnd, will call InsertEvent once
            // InsertOnStartInsertOnEnd, will call InsertEvent twice
            // InsertOnStartReplaceOnEnd, will call InsertEvent once and ReplaceEvent once
            // Manual, will never call InsertEvent or ReplaceEvent

            var inserted = new List<EntityFrameworkEvent>();
            var updated = new List<EntityFrameworkEvent>();
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson())))
                    .OnReplace((id, ev) => updated.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson()))))
                .WithCreationPolicy(eventCreationPolicy);

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                await context.SaveChangesAsync();
            }

            var expectedInserted = eventCreationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd || eventCreationPolicy == EventCreationPolicy.InsertOnEnd 
                ? 1 
                : eventCreationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd 
                    ? 2 
                    : 0;
            var expectedUpdated = eventCreationPolicy == EventCreationPolicy.InsertOnStartReplaceOnEnd ? 1 : 0;

            Assert.That(inserted.Count, Is.EqualTo(expectedInserted));
            Assert.That(updated.Count, Is.EqualTo(expectedUpdated));
            
            switch (eventCreationPolicy)
            {
                case EventCreationPolicy.InsertOnStartReplaceOnEnd:
                    Assert.That(inserted[0].Result, Is.EqualTo(0));
                    Assert.That(updated[0].Result, Is.EqualTo(1));
                    break;
                case EventCreationPolicy.InsertOnStartInsertOnEnd:
                    Assert.That(inserted[0].Result, Is.EqualTo(0));
                    Assert.That(inserted[1].Result, Is.EqualTo(1));
                    break;
                case EventCreationPolicy.InsertOnEnd:
                    Assert.That(inserted[0].Result, Is.EqualTo(1));
                    Assert.That(inserted[0].Entries.Count, Is.EqualTo(1));
                    break;
            }

        }

        [Test]
        public async Task ExceptionInAuditEntity_InsertOnEnd()
        {
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .AuditTypeExplicitMapper(m => m
                        .Map<Message, MessageAudit>()
                        .AuditEntityAction<MessageAudit>((auditEvent, eventEntry, entity) =>
                        {
                            entity.AuditData = eventEntry.ToJson();
                            entity.AuditTimestamp = DateTimeOffset.UtcNow;
                            entity.AuditAction = eventEntry.Action;
                        })
                    )
                )
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                await context.SaveChangesAsync();
            }

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                Assert.CatchAsync<Exception>(async () => await context.SaveChangesAsync());
            }

            using (var context = new TestContext())
            {
                Assert.That(await context.Messages.CountAsync(e => e.MessageId == messageId), Is.EqualTo(1));
                Assert.That(await context.MessageAudits.CountAsync(e => e.MessageId == messageId), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task ExceptionInAudit()
        {
            const string exceptionMessage = "test";
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .UseDbContext<TestContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Message, MessageAudit>()
                        .AuditEntityAction<MessageAudit>((auditEvent, eventEntry, entity) =>
                        {
                            throw new Exception(exceptionMessage);
#pragma warning disable CS0162
                            return Task.FromResult(true);
#pragma warning restore CS0162
                        })
                    )
                )
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                Exception exception = Assert.CatchAsync<Exception>(async () => await context.SaveChangesAsync());
                Assert.That(exception.Message, Is.EqualTo(exceptionMessage));
            }

            using (var context = new TestContext())
            {
                Assert.That(await context.Messages.CountAsync(e => e.MessageId == messageId), Is.EqualTo(0));
                Assert.That(await context.MessageAudits.CountAsync(e => e.MessageId == messageId), Is.EqualTo(0));
            }
        }
    }
}