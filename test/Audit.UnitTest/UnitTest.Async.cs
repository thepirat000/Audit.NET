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
using System.Reflection;

namespace Audit.UnitTest
{
    public class UnitTestAsync
    {
        private static JsonAdapter JsonAdapter = new JsonAdapter();

        [Test]
        public async Task Test_AsyncCustomAction_Fluent_Async()
        {
            var evs = new List<AuditEvent>();
            bool saved = false;
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    evs.Add(AuditEvent.FromJson(ev.ToJson()));
                    await Task.Delay(0);
                }))
                .WithCreationPolicy(EventCreationPolicy.Manual)
                .ResetActions()
                .WithAction(action => action
                    .OnEventSaved(async scope =>
                    {
                        await Task.Delay(500);
                        saved = true;
                    }))
                .WithAction(action => action
                    .OnEventSaving(async scope =>
                    {
                        await Task.Delay(500);
                        scope.Comment("OnEventSaving");
                    }))
                .WithAction(action => action
                    .OnScopeCreated(async scope =>
                    {
                        await Task.Delay(500);
                        scope.Comment("OnScopeCreated");
                    }));

            using (var scope = await AuditScope.CreateAsync("test", null))
            {
                await scope.SaveAsync();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Comments.Contains("OnScopeCreated"));
            Assert.IsTrue(evs[0].Comments.Contains("OnEventSaving"));
            Assert.IsTrue(saved);
        }

        [Test]
        public async Task Test_AsyncCustomAction_Async()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicAsyncProvider(_ => _.OnInsert(async ev =>
                {
                    evs.Add(AuditEvent.FromJson(ev.ToJson()));
                    await Task.Delay(0);
                }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            bool saved = false;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, async scope =>
            {
                await Task.Delay(500);
                scope.Comment("OnScopeCreated");
            });
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, async scope =>
            {
                await Task.Delay(500);
                scope.Comment("OnEventSaving");
            });
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, async scope =>
            {
                await Task.Delay(500);
                saved = true;
            });

            using (var scope = await AuditScope.CreateAsync("test", null))
            {
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Comments.Contains("OnScopeCreated"));
            Assert.IsTrue(evs[0].Comments.Contains("OnEventSaving"));
            Assert.IsTrue(saved);
        }

        [Test]
        public async Task Test_AuditScopeCreation_WithExistingAuditEvent_WithCustomFields_Async()
        {
            var evs_onScopeCreated = new List<AuditEvent>();
            var evs_Provider = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs_Provider.Add(AuditEvent.FromJson(ev.ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                .WithAction(_ => _.OnScopeCreated(scope =>
                {
                    evs_onScopeCreated.Add(AuditEvent.FromJson(scope.Event.ToJson()));
                }));

            var auditEvent = new AuditEvent()
            {
                EventType = "test",
                CustomFields = new Dictionary<string, object>()
                {
                    {"FromCustomField", 1 }
                }
            };
            var options = new AuditScopeOptions()
            {
                AuditEvent = auditEvent,
                ExtraFields = new { FromAnon = 2 }
            };
            using(var scope = await AuditScope.CreateAsync(options))
            {
                scope.SetCustomField("FromScope", 3);
            }

            Assert.AreEqual(1, evs_onScopeCreated.Count);
            Assert.AreEqual(2, evs_onScopeCreated[0].CustomFields.Count);
            Assert.IsTrue(evs_onScopeCreated[0].CustomFields.ContainsKey("FromCustomField"));
            Assert.IsTrue(evs_onScopeCreated[0].CustomFields.ContainsKey("FromAnon"));
            Assert.AreEqual("1", evs_onScopeCreated[0].CustomFields["FromCustomField"].ToString());
            Assert.AreEqual("2", evs_onScopeCreated[0].CustomFields["FromAnon"].ToString());

            Assert.AreEqual(1, evs_Provider.Count);
            Assert.AreEqual(3, evs_Provider[0].CustomFields.Count);
            Assert.IsTrue(evs_Provider[0].CustomFields.ContainsKey("FromCustomField"));
            Assert.IsTrue(evs_Provider[0].CustomFields.ContainsKey("FromAnon"));
            Assert.IsTrue(evs_Provider[0].CustomFields.ContainsKey("FromScope"));
            Assert.AreEqual("1", evs_Provider[0].CustomFields["FromCustomField"].ToString());
            Assert.AreEqual("2", evs_Provider[0].CustomFields["FromAnon"].ToString());
            Assert.AreEqual("3", evs_Provider[0].CustomFields["FromScope"].ToString());
        }

        [Test]
        public async Task Test_AuditScopeCreation_WithExistingAuditEvent_WithEventType_Async()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(AuditEvent.FromJson(ev.ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var auditEvent = new AuditEvent()
            {
                EventType = "test"
            };
            var options = new AuditScopeOptions()
            {
                EventType = null, // NULL means do not override eventtype
                AuditEvent = auditEvent
            };
            // scope with pre-assigned event type
            using (var scope = await AuditScope.CreateAsync(options))
            {
            }
            // scope with event type to override
            options.EventType = "override";
            using (var scope = await AuditScope.CreateAsync(options))
            {
            }
            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual("test", evs[0].EventType);
            Assert.AreEqual("override", evs[1].EventType);
        }

        [Test]
        public async Task Test_AsyncDynamicDataProvider_FluentApi_Async()
        {
            int ins = 0;
            int upd = 0;
            var dyn = new DynamicAsyncDataProvider(_ => _
                .OnInsert(async ev => { ins++; await Task.Delay(0); })
                .OnReplace(async (id, ev) => { upd++; await Task.Delay(0); }));

            using (var scope = await AuditScope.CreateAsync(_ => _
                .CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .DataProvider(dyn)).ConfigureAwait(false))
            {
            }

            Assert.AreEqual(1, ins);
            Assert.AreEqual(1, upd);
        }

        [Test]
        public async Task Test_AuditScope_Factory_FluentApi_Async()
        {
            var someMethod = typeof(UnitTestAsync).GetTypeInfo().GetMethods().First();
            var scope = await new AuditScopeFactory().CreateAsync(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEventEntityFramework())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(someMethod));

            Assert.AreEqual("event type", scope.EventType);
            Assert.AreEqual("event type", scope.Event.EventType);
            Assert.IsTrue(scope.Event.CustomFields.ContainsKey("f"));
            Assert.AreEqual(EventCreationPolicy.Manual, scope.EventCreationPolicy);
            Assert.AreEqual(typeof(AuditEventEntityFramework), scope.Event.GetType());
            Assert.AreEqual(typeof(DynamicDataProvider), scope.DataProvider.GetType());
            Assert.AreEqual(SaveMode.InsertOnStart, scope.SaveMode);
            Assert.AreEqual("1", scope.Event.Target.Old.ToString());
            Assert.IsTrue(scope.Event.Environment.CallingMethodName.Contains(someMethod.Name));
        }

        [Test]
        public async Task Test_AuditScope_Create_FluentApi_Async()
        {
            var someMethod = typeof(UnitTestAsync).GetTypeInfo().GetMethods().First();
            var scope = await AuditScope.CreateAsync(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEventEntityFramework())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(someMethod));

            Assert.AreEqual("event type", scope.EventType);
            Assert.AreEqual("event type", scope.Event.EventType);
            Assert.IsTrue(scope.Event.CustomFields.ContainsKey("f"));
            Assert.AreEqual(EventCreationPolicy.Manual, scope.EventCreationPolicy);
            Assert.AreEqual(typeof(AuditEventEntityFramework), scope.Event.GetType());
            Assert.AreEqual(typeof(DynamicDataProvider), scope.DataProvider.GetType());
            Assert.AreEqual(SaveMode.InsertOnStart, scope.SaveMode);
            Assert.AreEqual("1", scope.Event.Target.Old.ToString());
            Assert.IsTrue(scope.Event.Environment.CallingMethodName.Contains(someMethod.Name));
        }

        [Test]
        public async Task Test_AuditScope_Log_Async()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x.OnInsertAndReplace(ev => { evs.Add(ev); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            await AuditScope.LogAsync("test", new { field1 = "one" });

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("test", evs[0].EventType);
            Assert.AreEqual("one", evs[0].CustomFields["field1"].ToString());
        }

        [Test]
        public async Task Test_AuditScope_CallingMethod_Async()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x.OnInsertAndReplace(ev => { evs.Add(ev); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            using (var scope = await AuditScope.CreateAsync("test", () => "target"))
            {
            }
            using (var scope = await new AuditScopeFactory().CreateAsync("test", () => "target"))
            {
            }

            Assert.AreEqual(2, evs.Count);
        }

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
                        insertEvs.Add(JsonAdapter.Deserialize<AuditEvent>(JsonAdapter.Serialize(ev)));
                        return Guid.NewGuid();
                    })
                    .OnReplace(async (id, ev) =>
                    {
                        await Task.Delay(1);
                        replaceEvs.Add(JsonAdapter.Deserialize<AuditEvent>(JsonAdapter.Serialize(ev)));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var target = "x1";
            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions(){ TargetGetter = () => target }))
            {
                target = "x2";
                await scope.SaveAsync();
                target = "x3";
            }

            Assert.AreEqual(1, insertEvs.Count);
            Assert.AreEqual(2, replaceEvs.Count);
            Assert.AreEqual("x1", insertEvs[0].Target.Old.ToString());
            Assert.AreEqual("x2", replaceEvs[0].Target.New.ToString());
            Assert.AreEqual("x3", replaceEvs[1].Target.New.ToString());
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
            using (var scope = await new AuditScopeFactory().CreateAsync("evt", () => target, new { X = 1 }, null, null))
            {
                target = "end";
                await scope.DisposeAsync();
            }

            var fileFromProvider = await (Audit.Core.Configuration.DataProvider as FileDataProvider).GetEventAsync($@"{dir}\evt-1.json");

            var ev = JsonAdapter.Deserialize<AuditEvent>(File.ReadAllText(Path.Combine(dir, "evt-1.json")));
            var fileCount = Directory.EnumerateFiles(dir).Count();
            Directory.Delete(dir, true);

            Assert.AreEqual(1, fileCount);
            Assert.AreEqual(JsonAdapter.Serialize(ev), JsonAdapter.Serialize(fileFromProvider));
            Assert.AreEqual("evt", ev.EventType);
            Assert.AreEqual("start", ev.Target.Old.ToString());
            Assert.AreEqual("end", ev.Target.New.ToString());
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

            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { IsCreateAndSave = true }))
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

            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { }))
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

            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { }))
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

            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { }))
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

            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { }))
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
            var factory = new AuditScopeFactory();
            for (int i = 0; i < MAX; i++)
            {
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    await factory.LogAsync("LoginSuccess", new { username = "federico", id = i });
                    Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, ev =>
                    {
                        //do nothing, just bother
                        var d = ev.Event.Duration * 1234567;
                    });
                    await factory.CreateAsync(new AuditScopeOptions("LoginFailed", extraFields: new { username = "adriano", id = i * -1 }, isCreateAndSave: true));
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

            var scope = await new AuditScopeFactory().CreateAsync("et1", null, EventCreationPolicy.Manual, null);
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
            await new AuditScopeFactory().LogAsync(eventType, new { ExtraField = "extra value" });

            await new AuditScopeFactory().CreateAsync(new AuditScopeOptions(eventType, extraFields: new { Extra1 = new { SubExtra1 = "test1" }, Extra2 = "test2" }, dataProvider: provider.Object, isCreateAndSave: true));
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnStartInsertOnEnd, provider.Object))
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnEnd, null))
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

#if NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0
        [Test]
        public async Task Test_Dispose_Async()
        {
            var provider = new Mock<AuditDataProvider>();

            await using (var scope = await new AuditScopeFactory().CreateAsync(null, null, EventCreationPolicy.InsertOnEnd, dataProvider: provider.Object))
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
            using (var scope = await new AuditScopeFactory().CreateAsync(eventType, () => target, EventCreationPolicy.InsertOnEnd, null))
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
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnEnd, null))
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
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
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
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd, null))
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
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>()), Times.Never);

            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
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
            var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions("SomeEvent", null, new { @class = "class value", DATA = 123 }, null, EventCreationPolicy.Manual));
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
            var scope1 = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions("SomeEvent1", null, new { @class = "class value1", DATA = 111 }, null, EventCreationPolicy.Manual));
            await scope1.SaveAsync();
            var scope2 = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions("SomeEvent2", null, new { @class = "class value2", DATA = 222 }, null, EventCreationPolicy.Manual));
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
