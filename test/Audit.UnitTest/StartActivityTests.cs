#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;

namespace Audit.UnitTest
{
    [TestFixture]
    public class StartActivityTraceTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test, Explicit]
        public void Test_StartActivityTrace_NoActivityCreated_When_NoListeners()
        {
            var parentSource = new ActivitySource("Parent", "1.0");
            
            var parentActivity = parentSource.StartActivity();

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .UseInMemoryProvider();

            Activity childActivity;

            using (var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() }))
            {
                childActivity = Activity.Current;
            }

            Assert.That(parentActivity, Is.Null, () => $"Not NULL with parent: {parentActivity!.Parent?.OperationName}");
            Assert.That(childActivity, Is.Null);

            parentSource.Dispose();
        }

        [Test]
        public void Test_StartActivityTrace_CreatesChildActivity()
        {
            var started = new List<string>();
            var stopped = new List<string>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => started.Add($"{activity.ParentId}:{activity.Id}"),
                ActivityStopped = activity => stopped.Add($"{activity.ParentId}:{activity.Id}")
            };

            ActivitySource.AddActivityListener(listener);
            
            var parentSource = new ActivitySource("Parent", "1.0");
            var parentActivity = parentSource.StartActivity();

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .UseInMemoryProvider();

            Activity childActivity;

            using (var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() }))
            {
                childActivity = Activity.Current;

                Assert.That(childActivity.OperationName, Is.EqualTo(nameof(TestAuditEvent)));
                Assert.That(childActivity.Parent, Is.Not.Null);
                Assert.That(childActivity.Parent.OperationName, Is.EqualTo(nameof(Test_StartActivityTrace_CreatesChildActivity)));
                Assert.That(childActivity.Parent.Parent, Is.Null);
#if NET7_0_OR_GREATER
                Assert.That(childActivity.IsStopped, Is.False);
                Assert.That(childActivity.Parent.IsStopped, Is.False);
#endif
            }

            parentActivity!.Dispose();
            
#if NET7_0_OR_GREATER
            Assert.That(parentActivity.IsStopped, Is.True);
            Assert.That(childActivity.IsStopped, Is.True);
#endif
            Assert.That(parentActivity.Source.HasListeners(), Is.True);
            Assert.That(childActivity.Source.HasListeners(), Is.False);
            Assert.That(started.Count, Is.EqualTo(2));
            Assert.That(stopped.Count, Is.EqualTo(2));
            parentSource.Dispose();
            Assert.That(parentActivity.Source.HasListeners(), Is.False);
        }

        [Test]
        public void Test_StartActivityTrace_NoActivityCreated_When_False()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);
            var parentSource = new ActivitySource("Parent", "1.0");
            var parentActivity = parentSource.StartActivity();

            Audit.Core.Configuration.Setup()
                .StartActivityTrace(false)
                .UseInMemoryProvider();

            Activity childActivity;

            using (var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() }))
            {
                childActivity = Activity.Current;
            }

            parentSource.Dispose();
            
            Assert.That(childActivity, Is.Not.Null);
            Assert.That(parentActivity, Is.EqualTo(childActivity));
        }

        [Test]
        public void Test_StartActivityTrace_NoActivityCreated_When_AuditScope_Overrides_GlobalSetting()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);
            var parentSource = new ActivitySource("Parent", "1.0");
            var parentActivity = parentSource.StartActivity();

            Audit.Core.Configuration.Setup()
                .StartActivityTrace(true)
                .UseInMemoryProvider();

            Activity childActivity;

            using (var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent(), StartActivityTrace = false }))
            {
                childActivity = Activity.Current;
            }

            parentSource.Dispose();

            Assert.That(childActivity, Is.Not.Null);
            Assert.That(parentActivity, Is.EqualTo(childActivity));
        }

        [Test]
        public void Test_StartActivityTrace_IncludeActivity()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };

            ActivitySource.AddActivityListener(listener);
            
            var parentSource = new ActivitySource("Parent", "1.0");

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .IncludeActivityTrace()
                .UseInMemoryProvider();

            Activity activity;
            
            using (var scope = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() }))
            {
                activity = Activity.Current;
            }
            
            parentSource.Dispose();

            var tags = activity.TagObjects.ToDictionary(k => k.Key, v => v.Value);

            var evs = Audit.Core.Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEvents();
            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Activity.Operation, Is.EqualTo(nameof(TestAuditEvent)));
            Assert.That(tags, Contains.Key(nameof(AuditEvent)));
            Assert.That(tags[nameof(AuditEvent)] as TestAuditEvent, Is.Not.Null);
            Assert.That((tags[nameof(AuditEvent)] as TestAuditEvent)!.Environment.MachineName, Is.Not.Empty);
        }

        [Test]
        public void Test_StartActivityTrace_ParentChildScope()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
            };

            ActivitySource.AddActivityListener(listener);

            Audit.Core.Configuration.Setup()
                .StartActivityTrace(true)
                .UseInMemoryProvider();

            Activity parentActivity;
            Activity childActivity;

            using (var scope1 = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() { EventType = "parent" } }))
            {
                parentActivity = Activity.Current;
                using (var scope2 = AuditScope.Create(new AuditScopeOptions() { AuditEvent = new TestAuditEvent() { EventType = "child" } }))
                {
                    childActivity = Activity.Current;
                }
            }

            Assert.That(parentActivity, Is.Not.Null);
            Assert.That(childActivity, Is.Not.Null);

            Assert.That(parentActivity.SpanId, Is.EqualTo(childActivity.ParentSpanId));
            Assert.That(childActivity.TagObjects.Count(), Is.EqualTo(1));
            Assert.That(childActivity.TagObjects.First().Key, Is.EqualTo(nameof(AuditEvent)));
            Assert.That((childActivity.TagObjects.First().Value as AuditEvent)?.EventType, Is.EqualTo("child"));
            Assert.That((parentActivity.TagObjects.First().Value as AuditEvent)?.EventType, Is.EqualTo("parent"));
        }

        public class TestAuditEvent : AuditEvent
        {
        }
    }
}
#endif