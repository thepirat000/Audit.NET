using Audit.Core;
using System;
using Moq;
using Audit.Core.Providers;
using Audit.EntityFramework;
using System.Collections.Generic;
using Audit.Core.Extensions;
using System.Diagnostics;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Audit.UnitTest
{
    public class UnitTestAsync
    {
        [Test]
        public async Task Test_DynamicAsyncProvider_Async()
        {
            var insertEvs = new List<AuditEvent>();
            var replaceEvs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _
                    .OnInsert(async ev =>
                    {
                        await Task.Delay(1);
                        insertEvs.Add(JsonConvert.DeserializeObject<AuditEvent>(JsonConvert.SerializeObject(ev)));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonConvert.DeserializeObject<AuditEvent>(JsonConvert.SerializeObject(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var target = "x1";
            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions(){ TargetGetter = () => target }))
            {
                target = "x2";
                await scope.SaveAsync();
                target = "x3";
            }

            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(2, replaceEvs.Count);
            Assert.AreEqual("x1", insertEvs[0].Target.Old);
            Assert.AreEqual("x2", replaceEvs[0].Target.New);
            Assert.AreEqual("x3", replaceEvs[1].Target.New);
        }


        [Test]
        public async Task Test_FileLog_HappyPath_Async()
        {
            var dir = Path.Combine(Path.GetTempPath(), "Test_FileLog_HappyPath_Async");
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            Audit.Core.Configuration.Setup()
                .UseFileLogProvider(x => x
                    .Directory(dir)
                    .FilenameBuilder(_ => $"{_.EventType}-{_.CustomFields["X"]}.json"))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var target = "start";
            using (var scope = await AuditScope.CreateAsync("evt", () => target, new { X = 1 }))
            {
                target = "end";
                await scope.DisposeAsync();
            }

            var fileFromProvider = await (Audit.Core.Configuration.DataProvider as FileDataProvider).GetEventAsync($@"{dir}\evt-1.json");

            var ev = JsonConvert.DeserializeObject<AuditEvent>(File.ReadAllText(Path.Combine(dir, "evt-1.json")));
            var fileCount = Directory.EnumerateFiles(dir).Count();
            Directory.Delete(dir, true);

            Assert.AreEqual(1, fileCount);
            Assert.AreEqual(JsonConvert.SerializeObject(ev), JsonConvert.SerializeObject(fileFromProvider));
            Assert.AreEqual("evt", ev.EventType);
            Assert.AreEqual("start", ev.Target.Old);
            Assert.AreEqual("end", ev.Target.New);
            Assert.AreEqual("1", ev.CustomFields["X"].ToString());
        }

        [Test]
        public async Task Test_ScopeSaveMode_CreateAndSave_Async()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithCreationPolicy(EventCreationPolicy.Manual)
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { IsCreateAndSave = true }))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(1, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
        }

        [Test]
        public async Task Test_ScopeSaveMode_InsertOnStartReplaceOnEnd_Async()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { }))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(3, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
            Assert.AreEqual(SaveMode.ReplaceOnEnd, modes[1]);
            Assert.AreEqual(SaveMode.ReplaceOnEnd, modes[2]);
        }

        [Test]
        public async Task Test_ScopeSaveMode_InsertOnStartInsertOnEnd_Async()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd)
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { }))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(3, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[1]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[2]);
        }

        [Test]
        public async Task Test_ScopeSaveMode_InsertOnEnd_Async()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { }))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(2, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[0]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[1]);
        }

        [Test]
        public async Task Test_ScopeSaveMode_Manual_Async()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithCreationPolicy(EventCreationPolicy.Manual)
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = await AuditScope.CreateAsync(new AuditScopeOptions() { }))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(1, modes.Count);
            Assert.AreEqual(SaveMode.Manual, modes[0]);
        }

        [Test]
        public async Task Test_ScopeActionsStress_Async()
        {
            int counter = 0;
            int counter2 = 0;
            int counter3 = 0;
            int MAX = 200;
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    System.Threading.Interlocked.Increment(ref counter);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnEventSaving(ev =>
                {
                    System.Threading.Interlocked.Increment(ref counter2);
                }))
                .WithAction(_ => _.OnScopeCreated(ev =>
                {
                    System.Threading.Interlocked.Increment(ref counter3);
                }));

            var tasks = new List<Task>();
            for (int i = 0; i < MAX; i++)
            {
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    await AuditScope.LogAsync("LoginSuccess", new { username = "federico", id = i });
                    Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, ev =>
                    {
                        //do nothing, just bother
                        var d = ev.Event.Duration * 1234567;
                    });
                    await AuditScope.CreateAndSaveAsync("LoginFailed", new { username = "adriano", id = i * -1 });
                }));
            }
            await Task.WhenAll(tasks.ToArray());
            await Task.Delay(1000);
            Assert.AreEqual(MAX * 2, counter);
            Assert.AreEqual(MAX * 2, counter2);
            Assert.AreEqual(MAX * 2, counter3);
        }



        [Test]
        public async Task Test_DynamicDataProvider_Async()
        {
            int onInsertCount = 0, onReplaceCount = 0, onInsertOrReplaceCount = 0;
            Core.Configuration.Setup()
                .UseDynamicProvider(config => config
                    .OnInsert(ev => onInsertCount++)
                    .OnReplace((obj, ev) => onReplaceCount++)
                    .OnInsertAndReplace(ev => onInsertOrReplaceCount++));

            var scope = await AuditScope.CreateAsync("et1", null, EventCreationPolicy.Manual);
            await scope.SaveAsync();
            scope.SetCustomField("field", "value");
            Assert.AreEqual(1, onInsertCount);
            Assert.AreEqual(0, onReplaceCount);
            Assert.AreEqual(1, onInsertOrReplaceCount);
            await scope.SaveAsync();
            Assert.AreEqual(1, onInsertCount);
            Assert.AreEqual(1, onReplaceCount);
            Assert.AreEqual(2, onInsertOrReplaceCount);
        }



        [Test]
        public async Task Test_StartAndSave_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();

            var eventType = "event type";
            await AuditScope.LogAsync(eventType, new { ExtraField = "extra value" });

            await AuditScope.CreateAndSaveAsync(eventType, new { Extra1 = new { SubExtra1 = "test1" }, Extra2 = "test2" }, provider.Object);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);

        }

        [Test]
        public async Task Test_CustomAction_OnCreating_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();

            var eventType = "event type 1";
            var target = "test";
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                scope.SetCustomField("custom field", "test");
                if (scope.EventType == eventType)
                {
                    scope.Discard();
                }
            });
            Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                Assert.True(false, "This should not be executed");
            });

            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnStartInsertOnEnd, provider.Object))
            {
                ev = scope.Event;
            }
            Core.Configuration.ResetCustomActions();
            Assert.True(ev.CustomFields.ContainsKey("custom field"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public async Task Test_CustomAction_OnSaving_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            //provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent e) => e.Comments);
            var eventType = "event type 1";
            var target = "test";
            var comment = "comment test";
            Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                scope.Comment(comment);
            });
            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
                await scope.SaveAsync();
            }
            Core.Configuration.ResetCustomActions();
            Assert.True(ev.Comments.Contains(comment));
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public async Task Test_CustomAction_OnSaving_Discard_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            var eventType = "event type 1";
            var target = "test";
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                scope.Discard();
            });
            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
                await scope.SaveAsync();
            }
            Core.Configuration.ResetCustomActions();
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public async Task Test_CustomAction_OnCreating_Double_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            var eventType = "event type 1";
            var target = "test";
            var key1 = "key1";
            var key2 = "key2";
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                scope.SetCustomField(key1, "test");
            });
            Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
            {
                scope.Event.CustomFields.Remove(key1);
                scope.SetCustomField(key2, "test");
            });
            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
            }
            Core.Configuration.ResetCustomActions();
            Assert.False(ev.CustomFields.ContainsKey(key1));
            Assert.True(ev.CustomFields.ContainsKey(key2));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public async Task TestSave_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            Core.Configuration.DataProvider = provider.Object;
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                await scope.SaveAsync(); // this should do nothing because of the creation policy (this no more true since v4.6.2)
                provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            }
            Assert.AreEqual(eventType, ev.EventType);
            Assert.True(ev.Comments.Contains("test"));
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

#if NETCOREAPP2_0 || NETCOREAPP3_0
        [Test]
        public async Task Test_Dispose_Async()
        {
            var provider = new Mock<AuditDataProvider>();

            await using (var scope = await AuditScope.CreateAsync(null, null, EventCreationPolicy.InsertOnEnd, dataProvider: provider.Object))
            {               
            }

            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }
#endif

        [Test]
        public async Task TestDiscard_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            Core.Configuration.DataProvider = provider.Object;
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = await AuditScope.CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnEnd))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Discard();
            }
            Assert.AreEqual(eventType, ev.EventType);
            Assert.True(ev.Comments.Contains("test"));
            Assert.Null(ev.Target.New);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public async Task Test_EventCreationPolicy_InsertOnEnd_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await AuditScope.CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnEnd))
            {
                scope.Comment("test");
                await scope.SaveAsync(); // this should do nothing because of the creation policy (this is no more true, since v 4.6.2)
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public async Task Test_EventCreationPolicy_InsertOnStartReplaceOnEnd_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await AuditScope.CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd))
            {
                scope.Comment("test");
                await scope.DisposeAsync();
            }
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public async Task Test_EventCreationPolicy_InsertOnStartInsertOnEnd_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await AuditScope.CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public async Task Test_EventCreationPolicy_Manual_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await AuditScope.CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);

            using (var scope = await AuditScope.CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual))
            {
                scope.Comment("test");
                await scope.SaveAsync();
                scope.Comment("test2");
                await scope.SaveAsync();
            }
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public async Task Test_ExtraFields_Async()
        {
            Core.Configuration.DataProvider = new FileDataProvider();
            var scope = await AuditScope.CreateAsync("SomeEvent", null, new { @class = "class value", DATA = 123 }, EventCreationPolicy.Manual);
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.AreEqual("123", ev.CustomFields["DATA"].ToString());
            Assert.AreEqual("class value", ev.CustomFields["class"].ToString());
        }

        [Test]
        public async Task Test_TwoScopes_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            var scope1 = await AuditScope.CreateAsync("SomeEvent1", null, new { @class = "class value1", DATA = 111 }, EventCreationPolicy.Manual);
            await scope1.SaveAsync();
            var scope2 = await AuditScope.CreateAsync("SomeEvent2", null, new { @class = "class value2", DATA = 222 }, EventCreationPolicy.Manual);
            await scope2.SaveAsync();
            Assert.NotNull(scope1.EventId);
            Assert.NotNull(scope2.EventId);
            Assert.AreNotEqual(scope1.EventId, scope2.EventId);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [AuditDbContext(AuditEventType = "FromAttr")]
        public class MyContext : AuditDbContext
        {
            public override string AuditEventType { get { return base.AuditEventType; } }
            public override bool IncludeEntityObjects { get { return base.IncludeEntityObjects; } }
            public override AuditOptionMode Mode { get { return base.Mode; } }
        }
        public class AnotherContext : AuditDbContext
        {
            public override string AuditEventType { get { return base.AuditEventType; } }
            public override bool IncludeEntityObjects { get { return base.IncludeEntityObjects; } }
            public override AuditOptionMode Mode { get { return base.Mode; } }
        }
    }
}
