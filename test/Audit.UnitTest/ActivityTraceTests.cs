using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;

using NUnit.Framework;
using NUnit.Framework.Internal;

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
        public async Task Test_CurrentActivityInCustomAction_Async()
        {
            var started = new List<Activity>();
            var stopped = new List<Activity>();

            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = activity => started.Add(activity),
                ActivityStopped = activity => stopped.Add(activity)
            };

            ActivitySource.AddActivityListener(listener);

            Audit.Core.Configuration.Setup()
                .StartActivityTrace()
                .UseNullProvider();

            Audit.Core.Configuration.AddOnCreatedAction(scope => scope.GetActivity().SetTag("onCreated", 123));
            Audit.Core.Configuration.AddOnSavingAction(scope => scope.GetActivity().SetTag("onSaving", 456));

            var scope = await AuditScope.CreateAsync("Test", null, new { Field = 1 });
            
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(stopped, Has.Count.EqualTo(0));

            await scope.DisposeAsync();

            Assert.That(stopped, Has.Count.EqualTo(1));
            Assert.That(stopped[0].GetTagItem("onCreated"), Is.EqualTo(123));
            Assert.That(stopped[0].GetTagItem("onSaving"), Is.EqualTo(456));
        }

        [Test]
        public void Test_Activity_Taqs_ExistingActivity()
        {
            // Enabling Non‑Zero Span IDs
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            Audit.Core.Configuration.AddOnSavingAction(scope =>
            {
                var activity = Activity.Current;

                if (activity?.Tags.Any() == true)
                {
                    scope.Event.Activity.Tags = [];
                    scope.Event.Activity.Tags.AddRange(activity.Tags.Select(tag => new AuditActivityTag() { Key = tag.Key, Value = tag.Value }));
                }
            });

            ActivitySource.AddActivityListener(new ActivityListener()
            {
                ShouldListenTo = f => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });

            Configuration.Setup().UseInMemoryProvider(out var dp);

            var auditScopeFactory = new AuditScopeFactory();

            var activitySource = new ActivitySource("test", "1.2.3");
            var activity = activitySource.StartActivity("TEST", ActivityKind.Internal, null);

            Activity activity1;
            using (var scope = auditScopeFactory.Create(new AuditScopeOptions() { IncludeActivityTrace = true, StartActivityTrace = true }))
            {
                Activity.Current!.SetTag("Tag1", "1");
                activity1 = Activity.Current;
            }

            // Assert
            Assert.That(dp.GetAllEvents().Count, Is.EqualTo(1));
            var auditEvent = dp.GetAllEvents()[0];
            Assert.That(auditEvent.Activity, Is.Not.Null);
            Assert.That(activity1, Is.Not.Null);
            Assert.That(auditEvent.GetScope().GetActivity(), Is.EqualTo(activity1));
            Assert.That(activity1.ParentId, Is.EqualTo(activity!.Id));
        }

        [Test]
        public void Test_Activity_Trace_MultipleScopes()
        {
            // Enabling Non‑Zero Span IDs
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            ActivitySource.AddActivityListener(new ActivityListener()
            {
                ShouldListenTo = f => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            });

            Configuration.Setup().UseInMemoryProvider(out var dp);

            var auditScopeFactory = new AuditScopeFactory();

            var activitySource = new ActivitySource("test", "1.2.3");
            var activity = activitySource.StartActivity("TEST", ActivityKind.Internal, null);

            Activity activity1;
            Activity activity2;

            using (var scope = auditScopeFactory.Create(new AuditScopeOptions() { IncludeActivityTrace = true, StartActivityTrace = true , ExtraFields = new { test = 1 }}))
            {
                activity1 = scope.GetActivity();
            }

            using (var scope = auditScopeFactory.Create(new AuditScopeOptions() { IncludeActivityTrace = true, StartActivityTrace = true, ExtraFields = new { test = 2 } }))
            {
                activity2 = scope.GetActivity();
            }

            Assert.That(activity1, Is.Not.Null);
            Assert.That(activity2, Is.Not.Null);
            Assert.That(activity1, Is.Not.EqualTo(activity2));
            Assert.That(activity1.SpanId, Is.Not.EqualTo(activity2.SpanId));
            Assert.That(activity1.TraceId, Is.EqualTo(activity2.TraceId));
            Assert.That(activity1.ParentId, Is.EqualTo(activity2.ParentId));
            Assert.That(activity1.ParentId, Is.EqualTo(activity.Id));
            var auditEvents = dp.GetAllEvents();
            Assert.That(auditEvents, Has.Count.EqualTo(2));
            Assert.That(auditEvents[0].Activity.SpanId, Is.EqualTo(activity1.SpanId.ToString()));
            Assert.That(auditEvents[1].Activity.SpanId, Is.EqualTo(activity2.SpanId.ToString()));
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
