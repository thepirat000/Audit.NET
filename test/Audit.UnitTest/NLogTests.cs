using Audit.Core;
using Audit.NLog;
using NLog;
using Newtonsoft.Json;
using NLog.Targets;
using NUnit.Framework;
using LogLevel = Audit.NLog.LogLevel;

namespace Audit.UnitTest
{
    class NLogTests
    {
        private MemoryTarget _adapter;

        [OneTimeSetUp]
        public void Setup()
        {
            _adapter = new MemoryTarget();
            global::NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(_adapter, global::NLog.LogLevel.Debug);
        }

        [Test]
        public void Test_NLog_InsertOnStartReplaceOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseNLog(_ => _
                    .LogLevel(NLog.LogLevel.Info));

            _adapter.Logs.Clear();

            using (var s = AuditScope.Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                EventType = nameof(Test_NLog_InsertOnStartReplaceOnEnd)
            }))
            {

            }

            var events = _adapter.Logs.NLogDeserialize();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartReplaceOnEnd), JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject).EventType);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartReplaceOnEnd), JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject).EventType);
            Assert.AreEqual(JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject).CustomFields["EventId"], JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject).CustomFields["EventId"]);
        }

        [Test]
        public void Test_NLog_InsertOnStartInsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseNLog(_ => _
                    .LogLevel(ev => (LogLevel)ev.CustomFields["LogLevel"])
                    .Logger(ev => ev.CustomFields["Logger"] as ILogger));

            _adapter.Logs.Clear();

            using (var s = AuditScope.Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                EventType = nameof(Test_NLog_InsertOnStartInsertOnEnd),
                ExtraFields = new
                {
                    Logger = LogManager.GetLogger(typeof(NLogTests).ToString()),
                    LogLevel = LogLevel.Debug
                }
            }))
            {
                s.Event.CustomFields["LogLevel"] = LogLevel.Error;
            }

            var events = _adapter.Logs.NLogDeserialize();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual(LogLevel.Debug, events[0].Level);
            Assert.AreEqual(LogLevel.Error, events[1].Level);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartInsertOnEnd), JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject).EventType);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartInsertOnEnd), JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject).EventType);
            Assert.AreNotEqual(JsonConvert.DeserializeObject<AuditEvent>(events[0].MessageObject).CustomFields["EventId"], JsonConvert.DeserializeObject<AuditEvent>(events[1].MessageObject).CustomFields["EventId"]);
        }
    }
}
