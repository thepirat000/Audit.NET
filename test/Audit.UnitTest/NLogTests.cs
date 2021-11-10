using Audit.Core;
using NLog;
using NLog.Targets;
using NUnit.Framework;
using LogLevel = Audit.NLog.LogLevel;
using System;
using System.Linq;

namespace Audit.UnitTest
{
    public class NLogTests
    {
        private MemoryTarget _adapter;
        private static JsonAdapter JsonAdapter = new JsonAdapter();

        [OneTimeSetUp]
        public void Setup()
        {
            _adapter = new MemoryTarget();
            global::NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(_adapter, global::NLog.LogLevel.Debug);
        }

        [Test]
        public void Test_NLog_InsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseNLog(_ => _
                    .LogLevel(ev => (LogLevel)ev.CustomFields["LogLevel"])
                    .Logger(ev => LogManager.GetLogger(typeof(NLogTests).ToString())));

            _adapter.Logs.Clear();

            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = nameof(Test_NLog_InsertOnEnd),
                ExtraFields = new
                {
                    LogLevel = LogLevel.Debug
                }
            }))
            {
                s.Event.CustomFields["LogLevel"] = LogLevel.Error;
            }

            var events = _adapter.Logs.Select(l => new NLogObject(l)).ToArray();
            Assert.AreEqual(1, events.Length);
            Assert.AreEqual(LogLevel.Error, events[0].Level);
            Assert.AreEqual(nameof(Test_NLog_InsertOnEnd), JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType);
        }
        [Test]
        public void Test_NLog_InsertOnStartReplaceOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseNLog(_ => _
                    .LogLevel(NLog.LogLevel.Info));

            _adapter.Logs.Clear();

            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                EventType = nameof(Test_NLog_InsertOnStartReplaceOnEnd)
            }))
            {

            }

            var events = _adapter.Logs.Select(l => new NLogObject(l)).ToArray();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartReplaceOnEnd), JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartReplaceOnEnd), JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).EventType);
            Assert.AreEqual(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).CustomFields["EventId"].ToString(), JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).CustomFields["EventId"].ToString());
        }

        [Test]
        public void Test_NLog_InsertOnStartInsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseNLog(_ => _
                    .LogLevel(ev => (LogLevel)ev.CustomFields["LogLevel"])
                    .Logger(ev => LogManager.GetLogger(typeof(NLogTests).ToString())));

            _adapter.Logs.Clear();

            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
            {
                CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                EventType = nameof(Test_NLog_InsertOnStartInsertOnEnd),
                ExtraFields = new
                {
                    LogLevel = LogLevel.Debug
                }
            }))
            {
                s.Event.CustomFields["LogLevel"] = LogLevel.Error;
            }

            var events = _adapter.Logs.Select(l => new NLogObject(l)).ToArray();
            Assert.AreEqual(2, events.Length);
            Assert.AreEqual(LogLevel.Debug, events[0].Level);
            Assert.AreEqual(LogLevel.Error, events[1].Level);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartInsertOnEnd), JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType);
            Assert.AreEqual(nameof(Test_NLog_InsertOnStartInsertOnEnd), JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).EventType);
            Assert.AreNotEqual(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).CustomFields["EventId"], JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).CustomFields["EventId"]);
        }
    }

    public class NLogObject
    {
        public DateTime DateTimeObject { get; }
        public LogLevel Level { get; }
        public string LoggerNameObject { get; }
        public string MessageObject { get; }

        public NLogObject(string logEntry)
        {
            var div = logEntry.Split(new[] { '|' }, 4);
            DateTimeObject = DateTime.Parse(div[0]);
            LoggerNameObject = div[2];
            MessageObject = div[3];

            switch (div[1].ToLower())
            {
                case "debug":
                    Level = LogLevel.Debug;
                    break;
                case "warn":
                    Level = LogLevel.Warn;
                    break;
                case "error":
                    Level = LogLevel.Error;
                    break;
                case "fatal":
                    Level = LogLevel.Fatal;
                    break;
                case "info":
                default:
                    Level = LogLevel.Info;
                    break;
            }
        }
    }

}
