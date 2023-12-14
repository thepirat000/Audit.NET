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

            Assert.IsNotNull(scope.Event.Activity);
            Assert.IsNotEmpty(scope.Event.Activity.SpanId);
            Assert.IsNotEmpty(scope.Event.Activity.ParentId);
            Assert.IsNotEmpty(scope.Event.Activity.TraceId);
            Assert.IsTrue(scope.Event.Activity.StartTimeUtc <= DateTime.UtcNow);
            Assert.IsNotNull(scope.Event.Activity.Tags);
            Assert.AreEqual(1, scope.Event.Activity.Tags.Count);
            Assert.AreEqual("TAG1", scope.Event.Activity.Tags[0].Key);
            Assert.AreEqual("VALUE1", scope.Event.Activity.Tags[0].Value);
            Assert.AreEqual("Test1", scope.Event.Activity.Operation);
            Assert.IsNotNull(scope.Event.Activity.Events);
            Assert.AreEqual(1, scope.Event.Activity.Events.Count);
            Assert.AreEqual("Event1", scope.Event.Activity.Events[0].Name);
            Assert.IsTrue(scope.Event.Activity.Events[0].Timestamp <= DateTime.UtcNow);
        }

        [Test]
        public void Test_Activity_Trace_No_Activity()
        {
            Activity.Current = null;
            var scope = AuditScope.Create(new AuditScopeOptions() { IncludeActivityTrace = true });

            Assert.IsNull(scope.Event.Activity);
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

            Assert.IsNull(scope.Event.Activity);
        }
    }
}
#endif