using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;

using NUnit.Framework;
#pragma warning disable S2925

namespace Audit.UnitTest
{
    [TestFixture]
    public class ActivityDataProviderTests
    {
        private static ActivityListener _listener;
        
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            
            ActivitySource.AddActivityListener(_listener);
        }

        [TearDown]
        public void TearDown()
        {
            if (_listener != null)
            {
                _listener.Dispose();
            }
        }

        [Test]
        public void TestConfiguration_Ctor()
        {
            // Arrange & Act
            int actionCalls = 0;

            var dataProvider = new ActivityDataProvider(cfg => cfg
                .OnActivityCreated((activity, ev) =>
                {
                    actionCalls++;
                })
                .ActivityKind(ev => ActivityKind.Producer)
                .ActivityName(ev => ev.EventType)
                .IncludeDefaultTags()
                .AdditionalTags(ev => new() { { "t", 1 } })
                .Source(ev => "name", ev => "2.0"));

            dataProvider.OnActivityCreated.Invoke(null, new AuditEvent());

            // Assert
            Assert.That(dataProvider.IncludeDefaultTags.GetValue(null), Is.True);
            Assert.That(dataProvider.ActivityKind.GetValue(null), Is.EqualTo(ActivityKind.Producer));
            Assert.That(dataProvider.ActivityName.GetValue(new AuditEvent() { EventType = "Test" }), Is.EqualTo("Test"));
            Assert.That(dataProvider.SourceName.GetValue(null), Is.EqualTo("name"));
            Assert.That(dataProvider.SourceVersion.GetValue(null), Is.EqualTo("2.0"));
            Assert.That(dataProvider.AdditionalTags.GetValue(null), Has.Count.EqualTo(1));
            Assert.That(actionCalls, Is.EqualTo(1));
        }

        [Test]
        public void TestConfiguration_FluentUseActivityProvider()
        {
            // Arrange & Act
            int actionCalls = 0;

            Audit.Core.Configuration.Setup()
                .UseActivityProvider(cfg => cfg
                    .OnActivityCreated((activity, ev) =>
                    {
                        actionCalls++;
                    })
                    .ActivityKind(ev => ActivityKind.Producer)
                    .ActivityName(ev => ev.EventType)
                    .IncludeDefaultTags()
                    .AdditionalTags(ev => new() { { "t", 1 } })
                    .Source(ev => "name", ev => "2.0"));

            var dataProvider = Audit.Core.Configuration.DataProviderAs<ActivityDataProvider>();

            dataProvider.OnActivityCreated.Invoke(null, new AuditEvent());

            Audit.Core.Configuration.Reset();

            // Assert
            Assert.That(dataProvider.IncludeDefaultTags.GetValue(null), Is.True);
            Assert.That(dataProvider.ActivityKind.GetValue(null), Is.EqualTo(ActivityKind.Producer));
            Assert.That(dataProvider.ActivityName.GetValue(new AuditEvent() { EventType = "Test" }), Is.EqualTo("Test"));
            Assert.That(dataProvider.SourceName.GetValue(null), Is.EqualTo("name"));
            Assert.That(dataProvider.SourceVersion.GetValue(null), Is.EqualTo("2.0"));
            Assert.That(dataProvider.AdditionalTags.GetValue(null), Has.Count.EqualTo(1));
            Assert.That(actionCalls, Is.EqualTo(1));
        }

        [Test]
        public void InsertEvent_ShouldCreateAndStopActivity_WhenNotReplacePolicy()
        {
            // Arrange
            var provider = new ActivityDataProvider
            {
                IncludeDefaultTags = true,
                OnActivityCreated = (activity, auditEvent) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                }
            };

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddSeconds(1),
                Environment = new AuditEventEnvironment
                {
                    UserName = "TestUser",
                    MachineName = "TestMachine"
                },
                CustomFields = new Dictionary<string, object>
                {
                    { "CustomField1", "Value1" }
                }
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
        }

        [Test]
        public void ReplaceEvent_ShouldStopActivity_WhenReplacePolicy()
        {
            // Arrange
            var provider = new ActivityDataProvider();
            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddSeconds(1)
            };

            var eventId = provider.InsertEvent(auditEvent);

            // Act
            provider.ReplaceEvent(eventId, auditEvent);

            // Assert
            // No exception should be thrown, and the activity should be stopped.
        }

        [Test]
        public void InsertEvent_ShouldNotCreateActivity_WhenNoListeners()
        {
            // Arrange
            var provider = new ActivityDataProvider
            {
                SourceName = "NonExistentSource"
            };

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
        }

        [Test]
        public void IncludeDefaultTags_ShouldSetDefaultTags()
        {
            // Arrange
            var provider = new ActivityDataProvider
            {
                IncludeDefaultTags = true
            };

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddSeconds(1),
                Environment = new AuditEventEnvironment
                {
                    UserName = "TestUser",
                    MachineName = "TestMachine"
                }
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
        }

        [Test]
        public void ExtraTags_ShouldSetCustomTags()
        {
            // Arrange
            var provider = new ActivityDataProvider
            {
                AdditionalTags = new Setting<Dictionary<string, object>>(new Dictionary<string, object>
                {
                    { "ExtraTag1", "Value1" },
                    { "ExtraTag2", "Value2" }
                })
            };

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsNotNull(eventId);
        }

        [Test]
        public void ActivityAction_ShouldInvokeCustomAction()
        {
            // Arrange
            var customTagSet = false;
            var provider = new ActivityDataProvider
            {
                OnActivityCreated = (activity, auditEvent) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                    customTagSet = true;
                }
            };

            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow
            };

            // Act
            var eventId = provider.InsertEvent(auditEvent);

            // Assert
            Assert.IsTrue(customTagSet);
            Assert.IsNotNull(eventId);
        }

        [Test]
        public void ReplaceEvent_ShouldHandleMissingActivityGracefully()
        {
            // Arrange
            var provider = new ActivityDataProvider();
            var auditEvent = new AuditEvent
            {
                EventType = "TestEvent",
                StartDate = DateTime.UtcNow
            };

            // Act
            provider.ReplaceEvent("NonExistentEventId", auditEvent);

            // Assert
            Assert.Pass("No exception thrown");
        }
        
        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        [TestCase(EventCreationPolicy.Manual)]
        public void Test_ActivityCreation_WithEventCreationPolicy(EventCreationPolicy eventCreationPolicy)
        {
            // Arrange
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

            var dataProvider = new ActivityDataProvider()
            {
                IncludeDefaultTags = true,
                ActivityKind = ActivityKind.Consumer,
                ActivityName = new(ev => ev.EventType),
                SourceName = "Audit.NET.Test",
                SourceVersion = "2.0.0",
                AdditionalTags = new(ev => new Dictionary<string, object>() { { "domain.tag", ev.Environment.DomainName } }),
                OnActivityCreated = (activity, ev) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                    activity.SetCustomProperty("event", ev);
                }
            };

            ActivityDataProvider.DefaultTagCustomFieldFormat = "audit.custom.field.{0}";

            var expectedCount = eventCreationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd ? 2 : 1;

            var minSleepMs = 10;

            // Act
            using (var scope = AuditScope.Create(new AuditScopeOptions()
                   {
                       CreationPolicy = eventCreationPolicy,
                       DataProvider = dataProvider,
                       ExtraFields = new { Field1 = 1 },
                       EventType = "Test.EventType"
                   }))
            {
                Thread.Sleep(minSleepMs);
                scope.SetCustomField("Field2", 2);
                if (eventCreationPolicy == EventCreationPolicy.Manual)
                {
                    scope.Save();
                }
            }

            // Assert
            Assert.That(started, Has.Count.EqualTo(expectedCount));
            Assert.That(stopped, Has.Count.EqualTo(expectedCount));
            Assert.That(stopped[stopped.Count - 1], Is.EqualTo(started[stopped.Count - 1]));
            var activity = stopped[stopped.Count - 1];
            var auditEvent = activity.GetCustomProperty("event") as AuditEvent;
            Assert.That(auditEvent, Is.Not.Null);
            Assert.That(auditEvent.EventType, Is.EqualTo("Test.EventType"));
            var tags = activity.TagObjects.ToList();
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field1" && t.Value!.ToString() == 1.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field2" && t.Value!.ToString() == 2.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagStartTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagDurationMs), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEndTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEventType), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagMachine), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagUser), Is.True);
            Assert.That(tags.Exists(t => t.Key == "domain.tag" && t.Value!.ToString() == auditEvent.Environment.DomainName), Is.True);
            Assert.That(tags.Exists(t => t.Key == "custom.tag" && t.Value!.ToString() == "custom.value"), Is.True);

            Assert.That(activity.StartTimeUtc, Is.EqualTo(auditEvent.StartDate));
            Assert.That(activity.Duration.TotalMilliseconds, Is.InRange(auditEvent.Duration - 1, auditEvent.Duration + 1).And.GreaterThanOrEqualTo(minSleepMs));

            Assert.That(activity.OperationName, Is.EqualTo(auditEvent.EventType));
            Assert.That(activity.Source.Name, Is.EqualTo("Audit.NET.Test"));
            Assert.That(activity.Kind, Is.EqualTo(ActivityKind.Consumer));
            Assert.That(activity.Source.Version, Is.EqualTo("2.0.0"));
        }

        [Test]
        public void Test_ActivityCreation_WithCreateAndSave()
        {
            // Arrange
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

            var dataProvider = new ActivityDataProvider()
            {
                IncludeDefaultTags = true,
                ActivityKind = ActivityKind.Consumer,
                ActivityName = new(ev => ev.EventType),
                SourceName = "Audit.NET.Test",
                SourceVersion = "2.0.0",
                AdditionalTags = new(ev => new Dictionary<string, object>() { { "domain.tag", ev.Environment.DomainName } }),
                OnActivityCreated = (activity, ev) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                    activity.SetCustomProperty("event", ev);
                }
            };

            ActivityDataProvider.DefaultTagCustomFieldFormat = "audit.custom.field.{0}";

            // Act
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                IsCreateAndSave = true,
                DataProvider = dataProvider,
                ExtraFields = new { Field1 = 1 },
                EventType = "Test.EventType"
            });

            scope.Dispose();

            // Assert
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(stopped, Has.Count.EqualTo(1));
            Assert.That(stopped[0], Is.EqualTo(started[0]));
            var activity = stopped[0];
            var auditEvent = activity.GetCustomProperty("event") as AuditEvent;
            Assert.That(auditEvent, Is.Not.Null);
            Assert.That(auditEvent.EventType, Is.EqualTo("Test.EventType"));
            var tags = activity.TagObjects.ToList();
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field1" && t.Value!.ToString() == 1.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagStartTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagDurationMs), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEndTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEventType), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagMachine), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagUser), Is.True);
            Assert.That(tags.Exists(t => t.Key == "domain.tag" && t.Value!.ToString() == auditEvent.Environment.DomainName), Is.True);
            Assert.That(tags.Exists(t => t.Key == "custom.tag" && t.Value!.ToString() == "custom.value"), Is.True);

            Assert.That(activity.StartTimeUtc, Is.EqualTo(auditEvent.StartDate));
            Assert.That(activity.Duration.TotalMilliseconds, Is.InRange(auditEvent.Duration - 2, auditEvent.Duration + 2));

            Assert.That(activity.OperationName, Is.EqualTo(auditEvent.EventType));
            Assert.That(activity.Source.Name, Is.EqualTo("Audit.NET.Test"));
            Assert.That(activity.Kind, Is.EqualTo(ActivityKind.Consumer));
            Assert.That(activity.Source.Version, Is.EqualTo("2.0.0"));
        }

        [TestCase(EventCreationPolicy.InsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd)]
        [TestCase(EventCreationPolicy.Manual)]
        public async Task Test_ActivityCreation_WithEventCreationPolicyAsync(EventCreationPolicy eventCreationPolicy)
        {
            // Arrange
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

            var dataProvider = new ActivityDataProvider()
            {
                IncludeDefaultTags = true,
                ActivityKind = ActivityKind.Consumer,
                ActivityName = new(ev => ev.EventType),
                SourceName = "Audit.NET.Test",
                SourceVersion = "2.0.0",
                AdditionalTags = new(ev => new Dictionary<string, object>() { { "domain.tag", ev.Environment.DomainName } }),
                OnActivityCreated = (activity, ev) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                    activity.SetCustomProperty("event", ev);
                }
            };

            ActivityDataProvider.DefaultTagCustomFieldFormat = "audit.custom.field.{0}";

            var expectedCount = eventCreationPolicy == EventCreationPolicy.InsertOnStartInsertOnEnd ? 2 : 1;

            var minSleepMs = 10;

            // Act
            await using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                CreationPolicy = eventCreationPolicy,
                DataProvider = dataProvider,
                ExtraFields = new { Field1 = 1 },
                EventType = "Test.EventType"
            }))
            {
                await Task.Delay(minSleepMs);
                scope.SetCustomField("Field2", 2);
                if (eventCreationPolicy == EventCreationPolicy.Manual)
                {
                    await scope.SaveAsync();
                }
            }

            // Assert
            Assert.That(started, Has.Count.EqualTo(expectedCount));
            Assert.That(stopped, Has.Count.EqualTo(expectedCount));
            Assert.That(stopped[stopped.Count - 1], Is.EqualTo(started[stopped.Count - 1]));
            var activity = stopped[stopped.Count - 1];
            var auditEvent = activity.GetCustomProperty("event") as AuditEvent;
            Assert.That(auditEvent, Is.Not.Null);
            Assert.That(auditEvent.EventType, Is.EqualTo("Test.EventType"));
            var tags = activity.TagObjects.ToList();
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field1" && t.Value!.ToString() == 1.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field2" && t.Value!.ToString() == 2.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagStartTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagDurationMs), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEndTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEventType), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagMachine), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagUser), Is.True);
            Assert.That(tags.Exists(t => t.Key == "domain.tag" && t.Value!.ToString() == auditEvent.Environment.DomainName), Is.True);
            Assert.That(tags.Exists(t => t.Key == "custom.tag" && t.Value!.ToString() == "custom.value"), Is.True);

            Assert.That(activity.StartTimeUtc, Is.EqualTo(auditEvent.StartDate));
            Assert.That(activity.Duration.TotalMilliseconds, Is.InRange(auditEvent.Duration - 2, auditEvent.Duration + 2).And.GreaterThanOrEqualTo(minSleepMs - 2));

            Assert.That(activity.OperationName, Is.EqualTo(auditEvent.EventType));
            Assert.That(activity.Source.Name, Is.EqualTo("Audit.NET.Test"));
            Assert.That(activity.Kind, Is.EqualTo(ActivityKind.Consumer));
            Assert.That(activity.Source.Version, Is.EqualTo("2.0.0"));
        }

        [Test]
        public async Task Test_ActivityCreation_WithCreateAndSaveAsync()
        {
            // Arrange
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

            var dataProvider = new ActivityDataProvider()
            {
                IncludeDefaultTags = true,
                ActivityKind = ActivityKind.Consumer,
                ActivityName = new(ev => ev.EventType),
                SourceName = "Audit.NET.Test",
                SourceVersion = "2.0.0",
                AdditionalTags = new(ev => new Dictionary<string, object>() { { "domain.tag", ev.Environment.DomainName } }),
                OnActivityCreated = (activity, ev) =>
                {
                    activity.SetTag("custom.tag", "custom.value");
                    activity.SetCustomProperty("event", ev);
                }
            };

            ActivityDataProvider.DefaultTagCustomFieldFormat = "audit.custom.field.{0}";

            // Act
            var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                IsCreateAndSave = true,
                DataProvider = dataProvider,
                ExtraFields = new { Field1 = 1 },
                EventType = "Test.EventType"
            });

            await scope.DisposeAsync();

            // Assert
            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(stopped, Has.Count.EqualTo(1));
            Assert.That(stopped[0], Is.EqualTo(started[0]));
            var activity = stopped[0];
            var auditEvent = activity.GetCustomProperty("event") as AuditEvent;
            Assert.That(auditEvent, Is.Not.Null);
            Assert.That(auditEvent.EventType, Is.EqualTo("Test.EventType"));
            var tags = activity.TagObjects.ToList();
            Assert.That(tags.Exists(t => t.Key == "audit.custom.field.Field1" && t.Value!.ToString() == 1.ToString()), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagStartTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagDurationMs), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEndTime), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagEventType), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagMachine), Is.True);
            Assert.That(tags.Exists(t => t.Key == ActivityDataProvider.DefaultTagUser), Is.True);
            Assert.That(tags.Exists(t => t.Key == "domain.tag" && t.Value!.ToString() == auditEvent.Environment.DomainName), Is.True);
            Assert.That(tags.Exists(t => t.Key == "custom.tag" && t.Value!.ToString() == "custom.value"), Is.True);

            Assert.That(activity.StartTimeUtc, Is.EqualTo(auditEvent.StartDate));
            Assert.That(activity.Duration.TotalMilliseconds, Is.InRange(auditEvent.Duration - 1, auditEvent.Duration + 1));

            Assert.That(activity.OperationName, Is.EqualTo(auditEvent.EventType));
            Assert.That(activity.Source.Name, Is.EqualTo("Audit.NET.Test"));
            Assert.That(activity.Kind, Is.EqualTo(ActivityKind.Consumer));
            Assert.That(activity.Source.Version, Is.EqualTo("2.0.0"));
        }

        [Test]
        public void Test_ActivityCreation_NoListenerShouldNotCreateActivity()
        {
            // Arrange
            _listener.Dispose();

            var dataProvider = new ActivityDataProvider()
            {
                OnActivityCreated = (activity, ev) =>
                {
                    Assert.Fail("Should not call the action");
                },
                IncludeDefaultTags = new(ev =>
                {
                    Assert.Fail("Should not call the default tags");
                    return new();
                }),
                AdditionalTags = new(ev =>
                {
                    Assert.Fail("Should not call the extra tags");
                    return new();
                })
            };

            var auditEvent = new AuditEvent();

            // Act
            var eventId = dataProvider.InsertEvent(auditEvent);

            // Assert
            Assert.That(eventId, Is.Not.Null);
        }

        [Test]
        public async Task Test_ActivityCreation_NoListenerShouldNotCreateActivityAsync()
        {
            // Arrange
            _listener.Dispose();

            var dataProvider = new ActivityDataProvider()
            {
                OnActivityCreated = (activity, ev) =>
                {
                    Assert.Fail("Should not call the action");
                },
                IncludeDefaultTags = new(ev =>
                {
                    Assert.Fail("Should not call the default tags");
                    return new();
                }),
                AdditionalTags = new(ev =>
                {
                    Assert.Fail("Should not call the extra tags");
                    return new();
                })
            };

            var auditEvent = new AuditEvent();

            // Act
            var eventId = await dataProvider.InsertEventAsync(auditEvent);

            // Assert
            Assert.That(eventId, Is.Not.Null);
        }

        [Test]
        public void Test_ActivityCreation_NoAllData_CreatesNoTags()
        {
            // Arrange
            _listener.Dispose();

            var started = new List<Activity>();

            _listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.PropagationData,
                ActivityStarted = activity => started.Add(activity),
            };

            ActivitySource.AddActivityListener(_listener);

            var dataProvider = new ActivityDataProvider()
            {
                IncludeDefaultTags = true,
                AdditionalTags = new Dictionary<string, object>()
                {
                    { "test", 1 }
                }
            };

            var auditEvent = new AuditEvent()
            {
                StartDate = new DateTime(2025, 1, 1, 10, 20, 30, DateTimeKind.Utc)
            };

            // Act
            var eventId = dataProvider.InsertEvent(auditEvent);

            // Assert
            Assert.That(eventId, Is.Not.Null);
            Assert.That(started, Has.Count.EqualTo(1));
            var activity = started[0];
            Assert.That(activity, Is.Not.Null);
            Assert.That(activity.StartTimeUtc, Is.EqualTo(auditEvent.StartDate));

            var tags = activity.TagObjects.ToList();
            Assert.That(tags, Has.Count.Zero);
        }
    }
}
