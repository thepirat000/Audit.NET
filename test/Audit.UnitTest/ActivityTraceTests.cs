#if NET6_0_OR_GREATER
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Audit.Core;

namespace Audit.UnitTest
{
    public class ActivityTraceTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_Activity_Trace()
        {
            var source = new ActivitySource("Test", "1.0.0");
            ActivitySource.AddActivityListener(new ActivityListener()
            {
                ShouldListenTo = f => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });

            var activity_1 = source.StartActivity("Test1", ActivityKind.Internal, null);
            activity_1.AddTag("TAG1", "VALUE1");
            activity_1.AddEvent(new ActivityEvent("Event1"));

            var scope = AuditScope.Create(new AuditScopeOptions() { IncludeActivityTrace = true });
            scope.Dispose();

            Assert.That(scope.Event.Activity, Is.Not.Null);
            Assert.That(scope.Event.Activity.SpanId, Is.Not.Empty);
            Assert.That(scope.Event.Activity.ParentId, Is.Not.Empty);
            Assert.That(scope.Event.Activity.TraceId, Is.Not.Empty);
            Assert.That(scope.Event.Activity.StartTimeUtc <= DateTime.UtcNow, Is.True);
            Assert.That(scope.Event.Activity.Tags, Is.Not.Null);
            Assert.That(scope.Event.Activity.Tags.Count, Is.EqualTo(1));
            Assert.That(scope.Event.Activity.Tags[0].Key, Is.EqualTo("TAG1"));
            Assert.That(scope.Event.Activity.Tags[0].Value, Is.EqualTo("VALUE1"));
            Assert.That(scope.Event.Activity.Operation, Is.EqualTo("Test1"));
            Assert.That(scope.Event.Activity.Events, Is.Not.Null);
            Assert.That(scope.Event.Activity.Events.Count, Is.EqualTo(1));
            Assert.That(scope.Event.Activity.Events[0].Name, Is.EqualTo("Event1"));
            Assert.That(scope.Event.Activity.Events[0].Timestamp <= DateTime.UtcNow, Is.True);
        }

        [Test]
        public void Test_Activity_Trace_No_Activity()
        {
            Activity.Current = null;
            var scope = AuditScope.Create(new AuditScopeOptions() { IncludeActivityTrace = true });

            Assert.That(scope.Event.Activity, Is.Null);
        }

        [Test]
        public void Test_Activity_Trace_Disabled()
        {
            var source = new ActivitySource("Test", "1.0.0");
            ActivitySource.AddActivityListener(new ActivityListener()
            {
                ShouldListenTo = f => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });

            var activity_1 = source.StartActivity("Test1", ActivityKind.Internal, null);

            var scope = AuditScope.Create(new AuditScopeOptions() { IncludeActivityTrace = false });

            Assert.That(scope.Event.Activity, Is.Null);
        }
    }
}
#endif