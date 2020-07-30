using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Audit.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Audit.EntityFramework.Core.UnitTest
{
    [TestFixture]
    public class EfTransactionTests
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
            public DbSet<Message> Messages { get; set; }
            public DbSet<MessageAudit> MessageAudits { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
                => options.UseSqlServer("data source=localhost;initial catalog=TransactionTestEfCore;integrated security=true;");
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
        public async Task ExceptionInAuditEntity()
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
                .ForContext<TestContext>();

            Audit.Core.Configuration.Setup()
                .UseEntityFramework(ef => ef
                    .AuditTypeExplicitMapper(m => m
                        .Map<Message, MessageAudit>()
                        .AuditEntityAction<MessageAudit>((auditEvent, eventEntry, entity) =>
                        {
                            throw new Exception(exceptionMessage);
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