using Audit.Core;
using System;
using Moq;
using Audit.Core.Providers;
using System.Collections.Generic;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using InMemoryDataProvider = Audit.Core.Providers.InMemoryDataProvider;
using Audit.Core.Providers;

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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Comments.Contains("OnScopeCreated"), Is.True);
            Assert.That(evs[0].Comments.Contains("OnEventSaving"), Is.True);
            Assert.That(saved, Is.True);
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Comments.Contains("OnScopeCreated"), Is.True);
            Assert.That(evs[0].Comments.Contains("OnEventSaving"), Is.True);
            Assert.That(saved, Is.True);
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

            Assert.That(evs_onScopeCreated.Count, Is.EqualTo(1));
            Assert.That(evs_onScopeCreated[0].CustomFields.Count, Is.EqualTo(2));
            Assert.That(evs_onScopeCreated[0].CustomFields.ContainsKey("FromCustomField"), Is.True);
            Assert.That(evs_onScopeCreated[0].CustomFields.ContainsKey("FromAnon"), Is.True);
            Assert.That(evs_onScopeCreated[0].CustomFields["FromCustomField"].ToString(), Is.EqualTo("1"));
            Assert.That(evs_onScopeCreated[0].CustomFields["FromAnon"].ToString(), Is.EqualTo("2"));

            Assert.That(evs_Provider.Count, Is.EqualTo(1));
            Assert.That(evs_Provider[0].CustomFields.Count, Is.EqualTo(3));
            Assert.That(evs_Provider[0].CustomFields.ContainsKey("FromCustomField"), Is.True);
            Assert.That(evs_Provider[0].CustomFields.ContainsKey("FromAnon"), Is.True);
            Assert.That(evs_Provider[0].CustomFields.ContainsKey("FromScope"), Is.True);
            Assert.That(evs_Provider[0].CustomFields["FromCustomField"].ToString(), Is.EqualTo("1"));
            Assert.That(evs_Provider[0].CustomFields["FromAnon"].ToString(), Is.EqualTo("2"));
            Assert.That(evs_Provider[0].CustomFields["FromScope"].ToString(), Is.EqualTo("3"));
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
            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].EventType, Is.EqualTo("test"));
            Assert.That(evs[1].EventType, Is.EqualTo("override"));
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

            Assert.That(ins, Is.EqualTo(1));
            Assert.That(upd, Is.EqualTo(1));
        }

        [Test]
        public async Task Test_AuditScope_Factory_FluentApi_Async()
        {
            var someMethod = typeof(UnitTestAsync).GetTypeInfo().GetMethods().First();
            var scope = await new AuditScopeFactory().CreateAsync(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEvent())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(someMethod));

            Assert.That(scope.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.CustomFields.ContainsKey("f"), Is.True);
            Assert.That(scope.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
            Assert.That(scope.Event.GetType(), Is.EqualTo(typeof(AuditEvent)));
            Assert.That(scope.DataProvider.GetType(), Is.EqualTo(typeof(DynamicDataProvider)));
            Assert.That(scope.SaveMode, Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(scope.Event.Target.Old.ToString(), Is.EqualTo("1"));
            Assert.That(scope.Event.Environment.CallingMethodName.Contains(someMethod.Name), Is.True);
        }

        [Test]
        public async Task Test_AuditScope_Create_FluentApi_Async()
        {
            var someMethod = typeof(UnitTestAsync).GetTypeInfo().GetMethods().First();
            var scope = await AuditScope.CreateAsync(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEvent())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(someMethod));

            Assert.That(scope.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.CustomFields.ContainsKey("f"), Is.True);
            Assert.That(scope.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
            Assert.That(scope.Event.GetType(), Is.EqualTo(typeof(AuditEvent)));
            Assert.That(scope.DataProvider.GetType(), Is.EqualTo(typeof(DynamicDataProvider)));
            Assert.That(scope.SaveMode, Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(scope.Event.Target.Old.ToString(), Is.EqualTo("1"));
            Assert.That(scope.Event.Environment.CallingMethodName.Contains(someMethod.Name), Is.True);
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

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].EventType, Is.EqualTo("test"));
            Assert.That(evs[0].CustomFields["field1"].ToString(), Is.EqualTo("one"));
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

            Assert.That(evs.Count, Is.EqualTo(2));
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

            Assert.That(insertEvs.Count, Is.EqualTo(1));
            Assert.That(replaceEvs.Count, Is.EqualTo(2));
            Assert.That(insertEvs[0].Target.Old.ToString(), Is.EqualTo("x1"));
            Assert.That(replaceEvs[0].Target.New.ToString(), Is.EqualTo("x2"));
            Assert.That(replaceEvs[1].Target.New.ToString(), Is.EqualTo("x3"));
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

            var fileFromProvider = await Core.Configuration.DataProviderAs<FileDataProvider>().GetEventAsync($@"{dir}\evt-1.json");

            var ev = JsonAdapter.Deserialize<AuditEvent>(File.ReadAllText(Path.Combine(dir, "evt-1.json")));
            var fileCount = Directory.EnumerateFiles(dir).Count();
            Directory.Delete(dir, true);

            Assert.That(fileCount, Is.EqualTo(1));
            Assert.That(JsonAdapter.Serialize(fileFromProvider), Is.EqualTo(JsonAdapter.Serialize(ev)));
            Assert.That(ev.EventType, Is.EqualTo("evt"));
            Assert.That(ev.Target.Old.ToString(), Is.EqualTo("start"));
            Assert.That(ev.Target.New.ToString(), Is.EqualTo("end"));
            Assert.That(ev.CustomFields["X"].ToString(), Is.EqualTo("1"));
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

            Assert.That(modes.Count, Is.EqualTo(1));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
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

            Assert.That(modes.Count, Is.EqualTo(3));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(modes[1], Is.EqualTo(SaveMode.ReplaceOnEnd));
            Assert.That(modes[2], Is.EqualTo(SaveMode.ReplaceOnEnd));
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

            Assert.That(modes.Count, Is.EqualTo(3));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(modes[1], Is.EqualTo(SaveMode.InsertOnEnd));
            Assert.That(modes[2], Is.EqualTo(SaveMode.InsertOnEnd));
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

            Assert.That(modes.Count, Is.EqualTo(2));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnEnd));
            Assert.That(modes[1], Is.EqualTo(SaveMode.InsertOnEnd));
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

            Assert.That(modes.Count, Is.EqualTo(1));
            Assert.That(modes[0], Is.EqualTo(SaveMode.Manual));
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
                    await factory.CreateAsync(new AuditScopeOptions() { EventType = "LoginFailed", ExtraFields = new { username = "adriano", id = i * -1 }, IsCreateAndSave = true });
                }));
            }
            await Task.WhenAll(tasks.ToArray());
            await Task.Delay(1000);
            Assert.That(counter, Is.EqualTo(MAX * 2));
            Assert.That(counter2, Is.EqualTo(MAX * 2));
            Assert.That(counter3, Is.EqualTo(MAX * 2));
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
            Assert.That(onInsertCount, Is.EqualTo(1));
            Assert.That(onReplaceCount, Is.EqualTo(0));
            Assert.That(onInsertOrReplaceCount, Is.EqualTo(1));
            await scope.SaveAsync();
            Assert.That(onInsertCount, Is.EqualTo(1));
            Assert.That(onReplaceCount, Is.EqualTo(1));
            Assert.That(onInsertOrReplaceCount, Is.EqualTo(2));
        }



        [Test]
        public async Task Test_StartAndSave_Async()
        {
            var provider = new Mock<InMemoryDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();

            var eventType = "event type";

            await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = eventType, ExtraFields = new { Extra1 = new { SubExtra1 = "test1" }, Extra2 = "test2" }, DataProvider = provider.Object, IsCreateAndSave = true });
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_CustomAction_OnCreating_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();

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
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_CustomAction_OnSaving_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
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
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Test_CustomAction_OnSaving_Discard_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
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
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Test_CustomAction_OnCreating_Double_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
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
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task TestSave_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
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
                provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            }
            Assert.That(ev.EventType, Is.EqualTo(eventType));
            Assert.True(ev.Comments.Contains("test"));
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public async Task Test_Dispose_Async()
        {
            var provider = new Mock<AuditDataProvider>();

            await using (var scope = await new AuditScopeFactory().CreateAsync(null, null, EventCreationPolicy.InsertOnEnd, dataProvider: provider.Object))
            {               
            }

            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task TestDiscard_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
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
            Assert.That(ev.EventType, Is.EqualTo(eventType));
            Assert.True(ev.Comments.Contains("test"));
            Assert.Null(ev.Target.New);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
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
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public async Task Test_EventCreationPolicy_InsertOnStartReplaceOnEnd_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            {
                scope.Comment("test");
                await scope.DisposeAsync();
            }
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Test_EventCreationPolicy_InsertOnStartInsertOnEnd_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public async Task Test_EventCreationPolicy_Manual_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);

            using (var scope = await new AuditScopeFactory().CreateAsync("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
            {
                scope.Comment("test");
                await scope.SaveAsync();
                scope.Comment("test2");
                await scope.SaveAsync();
            }
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
            provider.Verify(p => p.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Test_ExtraFields_Async()
        {
            Core.Configuration.DataProvider = new FileDataProvider();
            var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = "SomeEvent", ExtraFields = new { @class = "class value", DATA = 123 }, CreationPolicy = EventCreationPolicy.Manual });
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.That(ev.CustomFields["DATA"].ToString(), Is.EqualTo("123"));
            Assert.That(ev.CustomFields["class"].ToString(), Is.EqualTo("class value"));
        }

        [Test]
        public async Task Test_TwoScopes_Async()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            provider.Setup(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>())).Returns(() => Task.FromResult((object)Guid.NewGuid()));
            Core.Configuration.DataProvider = provider.Object;
            var scope1 = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = "SomeEvent1", ExtraFields = new { @class = "class value1", DATA = 111 }, CreationPolicy = EventCreationPolicy.Manual });
            await scope1.SaveAsync();
            var scope2 = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = "SomeEvent2", ExtraFields = new { @class = "class value2", DATA = 222 }, CreationPolicy = EventCreationPolicy.Manual });
            await scope2.SaveAsync();
            Assert.NotNull(scope1.EventId);
            Assert.NotNull(scope2.EventId);
            Assert.That(scope2.EventId, Is.Not.EqualTo(scope1.EventId));
            provider.Verify(p => p.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}
