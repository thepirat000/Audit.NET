using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    [Category("Integration-SqlServer")]
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
            public static string CnnString = TestHelper.GetConnectionString("TransactionTestEfCore");

            public DbSet<Message> Messages { get; set; }
            public DbSet<MessageAudit> MessageAudits { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
                => options.UseSqlServer(CnnString);
        }

        [OneTimeSetUp]
        public async Task SetUp()
        {
            using (var context = new TestContext())
            {
                await context.Database.EnsureCreatedAsync();
            }
        }


        [Test]
        public async Task EarlySavingAudit_Enabled()
        {
            var inserted = new List<EntityFrameworkEvent>();
            var updated = new List<EntityFrameworkEvent>();
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>(_ => _.EarlySavingAudit(true));

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson())))
                    .OnReplace((id, ev) => updated.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson()))));

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, updated.Count);
            Assert.AreEqual(0, inserted[0].Result);
            Assert.AreEqual(1, updated[0].Result);
            Assert.AreEqual(1, inserted[0].Entries.Count);
            Assert.AreEqual(1, updated[0].Entries.Count);
        }

        [Test]
        public async Task EarlySavingAudit_Disabled()
        {
            var inserted = new List<EntityFrameworkEvent>();
            var updated = new List<EntityFrameworkEvent>();
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>(_ => _.EarlySavingAudit(false));

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson())))
                    .OnReplace((id, ev) => updated.Add(EntityFrameworkEvent.FromJson(ev.GetEntityFrameworkEvent().ToJson()))));

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                await context.SaveChangesAsync();
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, updated.Count);
            Assert.AreEqual(1, inserted[0].Result);
        }

        [Test]
        public async Task ExceptionInAuditEntity()
        {
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>(_ => _.EarlySavingAudit(true));

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
                );

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
                Assert.AreEqual(1, await context.Messages.CountAsync(e => e.MessageId == messageId));
                Assert.AreEqual(1, await context.MessageAudits.CountAsync(e => e.MessageId == messageId));
            }
        }

        [Test]
        public async Task ExceptionInAudit()
        {
            const string exceptionMessage = "test";
            var messageId = Guid.NewGuid();

            Audit.EntityFramework.Configuration.Setup()
                .ForContext<TestContext>(_ => _.EarlySavingAudit());

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .UseDbContext<TestContext>()
                    .AuditTypeExplicitMapper(m => m
                        .Map<Message, MessageAudit>()
                        .AuditEntityAction<MessageAudit>((auditEvent, eventEntry, entity) =>
                        {
                            throw new Exception(exceptionMessage);
                            return Task.FromResult(true);
                        })
                    )
                );

            using (var context = new TestContext())
            {
                Message message = new Message
                {
                    MessageId = messageId
                };
                await context.AddAsync(message);
                Exception exception = Assert.CatchAsync<Exception>(async () => await context.SaveChangesAsync());
                Assert.AreEqual(exceptionMessage, exception.Message);
            }

            using (var context = new TestContext())
            {
                Assert.AreEqual(0, await context.Messages.CountAsync(e => e.MessageId == messageId));
                Assert.AreEqual(0, await context.MessageAudits.CountAsync(e => e.MessageId == messageId));
            }
        }
    }
}