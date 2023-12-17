using Audit.Core;
using Audit.log4net;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using NUnit.Framework;
using System.Reflection;

namespace Audit.UnitTest
{
    [TestFixture]
    public class Log4netTests
    {
        private MemoryAppender _adapter;
        private static JsonAdapter JsonAdapter = new JsonAdapter();

        [OneTimeSetUp]
        public void Setup()
        {
            _adapter = new MemoryAppender();
            var repository =
                (global::log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository(typeof(AuditEvent).GetTypeInfo().Assembly);
            repository.Root.AddAppender(_adapter);
            BasicConfigurator.Configure(repository);
        }

        [Test]
        public void Test_log4net_InsertOnStartReplaceOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseLog4net(_ => _
                    .LogLevel(LogLevel.Info));

            _adapter.Clear();

            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                EventType = "Test_log4net_InsertOnStartReplaceOnEnd"
            }))
            {
                
            }

            var events = _adapter.PopAllEvents();
            Assert.That(events.Length, Is.EqualTo(2));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject.ToString()).EventType, Is.EqualTo("Test_log4net_InsertOnStartReplaceOnEnd"));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject.ToString()).EventType, Is.EqualTo("Test_log4net_InsertOnStartReplaceOnEnd"));
            var jsonAdapter = new JsonAdapter();
            Assert.That(jsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject.ToString()).CustomFields["EventId"].ToString(), Is.EqualTo(jsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject.ToString()).CustomFields["EventId"].ToString()));
        }

        [Test]
        public void Test_log4net_InsertOnStartInsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseLog4net(_ => _
                    .LogLevel(ev => (LogLevel)ev.CustomFields["LogLevel"])
                    .Logger(ev => ev.CustomFields["Logger"] as ILog));

            _adapter.Clear();

            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                EventType = "Test_log4net_InsertOnStartInsertOnEnd",
                ExtraFields = new
                {
                    Logger = LogManager.GetLogger(typeof(Log4netTests)),
                    LogLevel = LogLevel.Debug
                }
            }))
            {
                s.Event.CustomFields["LogLevel"] = LogLevel.Error;
            }

            var events = _adapter.PopAllEvents();
            Assert.That(events.Length, Is.EqualTo(2));
            Assert.That(events[0].Level, Is.EqualTo(Level.Debug));
            Assert.That(events[1].Level, Is.EqualTo(Level.Error));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject.ToString()).EventType, Is.EqualTo("Test_log4net_InsertOnStartInsertOnEnd"));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject.ToString()).EventType, Is.EqualTo("Test_log4net_InsertOnStartInsertOnEnd"));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject.ToString()).CustomFields["EventId"].ToString(), Is.Not.EqualTo(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject.ToString()).CustomFields["EventId"].ToString()));
        }
    }
}
