using Audit.Core;
using Audit.NET.Serilog;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.InMemory;
using Serilog.Sinks.InMemory.Assertions;

namespace Audit.UnitTest
{
    public class SerilogTests
    {
        private Logger _logger;

        [SetUp]
        public void Setup()
        {
            _logger?.Dispose();

            _logger = new LoggerConfiguration()
                .WriteTo.InMemory()
                .CreateLogger();
        }

        [Test]
        public void Test_SeriLog_InsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseSerilog(_ => _
                    .LogLevel(ev => (LogLevel)ev.CustomFields["LogLevel"])
                    .Logger(ev => _logger)
                    .Message((ev, id) => $"{ev.EventType}-{ev.Environment.UserName}-{ev.StartDate}-{ev.Duration}"));

            AuditEvent auditEvent;
            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
                   {
                       CreationPolicy = EventCreationPolicy.InsertOnEnd,
                       EventType = nameof(Test_SeriLog_InsertOnEnd),
                       ExtraFields = new
                       {
                           LogLevel = LogLevel.Debug
                       }
                   }))
            {
                s.Event.CustomFields["LogLevel"] = LogLevel.Error;
                auditEvent = s.Event;
            }

            InMemorySink.Instance.Should()
                .HaveMessage("{Value}")
                .Appearing()
                .Once()
                .WithLevel(global::Serilog.Events.LogEventLevel.Error)
                .WithProperty("Value")
                .WithValue($"{auditEvent.EventType}-{auditEvent.Environment.UserName}-{auditEvent.StartDate}-{auditEvent.Duration}");
        }

        [Test]
        public void Test_SeriLog_InsertOnStartReplaceOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseSerilog(_ => _
                    .Logger(ev => _logger)
                    .Message((ev, id) => $"{ev.EventType}-{ev.CustomFields["p1"]}"));

            AuditEvent auditEvent;
            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
                   {
                       CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                       EventType = nameof(Test_SeriLog_InsertOnStartReplaceOnEnd),
                       ExtraFields = new
                       {
                           p1 = "pre"
                       }
                   }))
            {
                s.Event.CustomFields["p1"] = "post";
                auditEvent = s.Event;
            }

            InMemorySink.Instance.Should()
                .HaveMessage("{Value}")
                .Appearing()
                .Times(2)
                .WithProperty("Value")
                .WithValues($"{auditEvent.EventType}-pre", $"{auditEvent.EventType}-post");
        }

        [Test]
        public void Test_SeriLog_InsertOnStartInsertOnEnd()
        {
            Audit.Core.Configuration.Setup()
                .UseSerilog(_ => _
                    .Logger(ev => _logger)
                    .Message((ev, id) => $"{ev.EventType}-{ev.CustomFields["p1"]}"));

            AuditEvent auditEvent;
            using (var s = new AuditScopeFactory().Create(new AuditScopeOptions()
                   {
                       CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                       EventType = nameof(Test_SeriLog_InsertOnStartInsertOnEnd),
                       ExtraFields = new
                       {
                           p1 = "pre"
                       }
                   }))
            {
                s.Event.CustomFields["p1"] = "post";
                auditEvent = s.Event;
            }

            InMemorySink.Instance.Should()
                .HaveMessage("{Value}")
                .Appearing()
                .Times(2)
                .WithProperty("Value")
                .WithValues($"{auditEvent.EventType}-pre", $"{auditEvent.EventType}-post");
        }
    }
}