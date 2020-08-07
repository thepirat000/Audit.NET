using Audit.Core;
using Audit.log4net;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class Log4netTests
    {
        private MemoryAppender _adapter;

        [OneTimeSetUp]
        public void Setup()
        {
            _adapter = new MemoryAppender();
            var repository =
                (global::log4net.Repository.Hierarchy.Hierarchy) LogManager.GetRepository(typeof(AuditEvent).Assembly);
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
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual("Test_log4net_InsertOnStartReplaceOnEnd", JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject.ToString()).EventType);
            Assert.AreEqual("Test_log4net_InsertOnStartReplaceOnEnd", JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject.ToString()).EventType);
            Assert.AreEqual(JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject.ToString()).CustomFields["EventId"], JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject.ToString()).CustomFields["EventId"]);
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
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual(Level.Debug, events[0].Level);
            Assert.AreEqual(Level.Error, events[1].Level);
            Assert.AreEqual("Test_log4net_InsertOnStartInsertOnEnd", JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject.ToString()).EventType);
            Assert.AreEqual("Test_log4net_InsertOnStartInsertOnEnd", JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject.ToString()).EventType);
            Assert.AreNotEqual(JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject.ToString()).CustomFields["EventId"], JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject.ToString()).CustomFields["EventId"]);
        }
    }
}
