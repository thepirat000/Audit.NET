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
            LogManager.Setup().LoadConfiguration(c => c.ForLogger(global::NLog.LogLevel.Debug).WriteTo(_adapter));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _adapter.Dispose();
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
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType, Is.EqualTo(nameof(Test_NLog_InsertOnEnd)));
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
            Assert.That(events.Length, Is.EqualTo(2));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType, Is.EqualTo(nameof(Test_NLog_InsertOnStartReplaceOnEnd)));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).EventType, Is.EqualTo(nameof(Test_NLog_InsertOnStartReplaceOnEnd)));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).CustomFields["EventId"].ToString(), Is.EqualTo(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).CustomFields["EventId"].ToString()));
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
            Assert.That(events.Length, Is.EqualTo(2));
            Assert.That(events[0].Level, Is.EqualTo(LogLevel.Debug));
            Assert.That(events[1].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).EventType, Is.EqualTo(nameof(Test_NLog_InsertOnStartInsertOnEnd)));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).EventType, Is.EqualTo(nameof(Test_NLog_InsertOnStartInsertOnEnd)));
            Assert.That(JsonAdapter.Deserialize<AuditEvent>(events[1].MessageObject).CustomFields["EventId"], Is.Not.EqualTo(JsonAdapter.Deserialize<AuditEvent>(events[0].MessageObject).CustomFields["EventId"]));
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
