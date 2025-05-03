using Audit.Core;
using System;
using Moq;
using Audit.Core.Providers;
using System.Collections.Generic;
using Audit.Core.Extensions;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reflection;
using Configuration = Audit.Core.Configuration;
using InMemoryDataProvider = Audit.Core.Providers.InMemoryDataProvider;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Audit.Core.Providers;

namespace Audit.UnitTest
{
    public class UnitTest
    {
        private static IJsonAdapter JsonAdapter = Audit.Core.Configuration.JsonAdapter;

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_Configuration_Reset()
        {
            Configuration.IncludeTypeNamespaces = true;
            Configuration.IncludeStackTrace = true;
            Configuration.IncludeActivityTrace = true;
            Configuration.StartActivityTrace = true;
            Configuration.DataProvider = new InMemoryDataProvider();
            Configuration.AuditDisabled = true;
            Configuration.AddOnCreatedAction(s => { });
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            
            Configuration.Reset();

            Assert.That(Configuration.IncludeTypeNamespaces, Is.EqualTo(false));
            Assert.That(Configuration.IncludeStackTrace, Is.EqualTo(false));
            Assert.That(Configuration.IncludeActivityTrace, Is.EqualTo(false));
            Assert.That(Configuration.StartActivityTrace, Is.EqualTo(false));
            Assert.IsInstanceOf<FileDataProvider>(Configuration.DataProvider);
            Assert.That(Configuration.AuditDisabled, Is.EqualTo(false));
            Assert.That(Configuration.AuditScopeActions[ActionType.OnScopeCreated].Count, Is.EqualTo(0));
            Assert.That(Configuration.CreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnEnd));
        }

        [Test]
        public void Test_AsyncCustomAction_Fluent()
        {
            var evs = new List<AuditEvent>();
            bool saved = false;
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(AuditEvent.FromJson(ev.ToJson()));
                }))
                .WithManualCreationPolicy()
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

            using (var scope = AuditScope.Create("test", null))
            {
                scope.Save();
            }

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Comments.Contains("OnScopeCreated"), Is.True);
            Assert.That(evs[0].Comments.Contains("OnEventSaving"), Is.True);
            Assert.That(saved, Is.True);
        }

        [Test]
        public void Test_AsyncCustomAction()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev =>
                {
                    evs.Add(AuditEvent.FromJson(ev.ToJson()));
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

            using (var scope = AuditScope.Create("test", null))
            {
            }

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Comments.Contains("OnScopeCreated"), Is.True);
            Assert.That(evs[0].Comments.Contains("OnEventSaving"), Is.True);
            Assert.That(saved, Is.True);
        }

        [Test]
        public void Test_AuditScopeCreation_WithExistingAuditEvent_WithCustomFields()
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
            using (var scope = AuditScope.Create(options))
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
        public void Test_AuditScopeCreation_WithExistingAuditEvent_WithEventType()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(false)
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(AuditEvent.FromJson(ev.ToJson()));
                    }))
                .WithInsertOnEndCreationPolicy();

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
            using (var scope = AuditScope.Create(options))
            {
            }
            // scope with event type to override
            options.EventType = "override";
            using (var scope = AuditScope.Create(options))
            {
            }
            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].EventType, Is.EqualTo("test"));
            Assert.That(evs[1].EventType, Is.EqualTo("override"));
        }

        [Test]
        public void Test_DynamicDataProvider_FluentApi()
        {
            int ins = 0;
            int upd = 0;
            var dyn = new DynamicDataProvider(_ => _
                .OnInsert(ev => ins++)
                .OnReplace((id, ev) => upd++));

            using (var scope = AuditScope.Create(_ => _
                .CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .DataProvider(dyn)))
            {
            }

            Assert.That(ins, Is.EqualTo(1));
            Assert.That(upd, Is.EqualTo(1));
        }

        [Test]
        public void Test_AuditScope_Factory_FluentApi()
        {
            var mb = typeof(UnitTest).GetTypeInfo().GetMethods().First();

            var scope = new AuditScopeFactory().Create(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEvent())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(mb));

            Assert.That(scope.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.CustomFields.ContainsKey("f"), Is.True);
            Assert.That(scope.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
            Assert.That(scope.Event.GetType(), Is.EqualTo(typeof(AuditEvent)));
            Assert.That(scope.DataProvider.GetType(), Is.EqualTo(typeof(DynamicDataProvider)));
            Assert.That(scope.SaveMode, Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(scope.Event.Target.Old.ToString(), Is.EqualTo("1"));
            Assert.That(scope.Event.Environment.CallingMethodName.Contains(mb.Name), Is.True);
        }

        [Test]
        public void Test_AuditScope_Create_FluentApi()
        {
            var mb = typeof(UnitTest).GetTypeInfo().GetMethods().First();

            var scope = AuditScope.Create(_ => _
                .EventType("event type")
                .ExtraFields(new { f = 1 })
                .CreationPolicy(EventCreationPolicy.Manual)
                .AuditEvent(new AuditEvent())
                .DataProvider(new DynamicDataProvider())
                .IsCreateAndSave(true)
                .SkipExtraFrames(1)
                .Target(() => 1)
                .CallingMethod(mb));

            Assert.That(scope.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.EventType, Is.EqualTo("event type"));
            Assert.That(scope.Event.CustomFields.ContainsKey("f"), Is.True);
            Assert.That(scope.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
            Assert.That(scope.Event.GetType(), Is.EqualTo(typeof(AuditEvent)));
            Assert.That(scope.DataProvider.GetType(), Is.EqualTo(typeof(DynamicDataProvider)));
            Assert.That(scope.SaveMode, Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(scope.Event.Target.Old.ToString(), Is.EqualTo("1"));
            Assert.That(scope.Event.Environment.CallingMethodName.Contains(mb.Name), Is.True);
        }

        [Test]
        public void Test_AuditScope_Log()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x.OnInsertAndReplace(ev => { evs.Add(ev); }))
                .WithInsertOnEndCreationPolicy();
            AuditScope.Log("test", new { field1 = "one" });

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].EventType, Is.EqualTo("test"));
            Assert.That(evs[0].CustomFields["field1"].ToString(), Is.EqualTo("one"));
            Assert.That(evs[0].Environment.CallingMethodName.Contains("Test_AuditScope_Log"), Is.True);
        }

        [Test]
        public void Test_AuditScope_CallingMethod()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x.OnInsertAndReplace(ev => { evs.Add(ev); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            using (var scope = AuditScope.Create("test", () => "target"))
            {
            }
            using (var scope = new AuditScopeFactory().Create("test", () => "target"))
            {
            }

            Assert.That(evs.Count, Is.EqualTo(2));
            Assert.That(evs[0].Environment.CallingMethodName.Contains("Test_AuditScope_CallingMethod"), Is.True);
            Assert.That(evs[1].Environment.CallingMethodName.Contains("Test_AuditScope_CallingMethod"), Is.True);
        }

        [Test]
        public void Test_AuditScope_CustomSystemClock()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            using (var scope = new AuditScopeFactory().Create("Test_AuditScope_CustomSystemClock", () => new { someProp = true }))
            {
                scope.SetCustomField("test", 123);
            }
            Audit.Core.Configuration.SystemClock = new DefaultSystemClock();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Duration, Is.EqualTo(10000));
            Assert.That(evs[0].StartDate, Is.EqualTo(new DateTime(2020, 1, 1, 0, 0, 0)));
            Assert.That(evs[0].EndDate, Is.EqualTo(new DateTime(2020, 1, 1, 0, 0, 10)));
        }

        [Test]
        public void Test_AuditScope_SetTargetGetter()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var obj = new SomeClass() { Id = 1, Name = "Test" };

            using (var scope = new AuditScopeFactory().Create("Test", () => new { ShouldNotUseThisObject = true }))
            {
                scope.SetTargetGetter(() => obj);
                obj.Id = 2;
                obj.Name = "NewTest";
            }
            obj.Id = 3;
            obj.Name = "X";

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Target.Type, Is.EqualTo("SomeClass"));

            Assert.That(JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.Old)).Id, Is.EqualTo(1));
            Assert.That(JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.Old)).Name, Is.EqualTo("Test"));
            Assert.That(JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.New)).Id, Is.EqualTo(2));
            Assert.That(JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.New)).Name, Is.EqualTo("NewTest"));
        }

        [Test]
        public void Test_AuditScope_SetTargetGetter_ReturnsNull()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            using (var scope = new AuditScopeFactory().Create("Test", () => new { ShouldNotUseThisObject = true }))
            {
                scope.SetTargetGetter(() => null);
            }

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Target.Type, Is.EqualTo("Object"));
            Assert.That(evs[0].Target.Old, Is.Null);
            Assert.That(evs[0].Target.New, Is.Null);
        }

        [Test]
        public void Test_AuditScope_SetTargetGetter_IsNull()
        {
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        evs.Add(ev);
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);


            using (var scope = new AuditScopeFactory().Create("Test", () => new { ShouldNotUseThisObject = true }))
            {
                scope.SetTargetGetter(null);
            }

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Target, Is.Null);
        }

        [Test]
        public void Test_AuditEvent_CustomSerializer_SystemJson()
        {
            var listEv = new List<AuditEvent>();
            var listJson = new List<string>();
            Audit.Core.Configuration.Setup()
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        listEv.Add(ev);
                        listJson.Add(ev.ToJson());
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);

            var prevSettings = Core.Configuration.JsonSettings;
            Core.Configuration.JsonSettings = new JsonSerializerOptions() { WriteIndented = true };

            using (var scope = new AuditScopeFactory().Create("TEST", null, null, null, null))
            {
            }

            Assert.That(listEv.Count, Is.EqualTo(1));

            var manualJson = listEv[0].ToJson();

            Assert.That(listJson.Count, Is.EqualTo(1));

            var jsonExpected = JsonSerializer.Serialize(listEv[0], Core.Configuration.JsonSettings);
            Assert.That(jsonExpected.Count(c => c == '\n') > 5, Is.True);
            Assert.That(listJson[0], Is.EqualTo(jsonExpected));
            Assert.That(manualJson, Is.EqualTo(jsonExpected));
            Assert.That(listEv[0].ToJson(), Is.EqualTo(JsonAdapter.Serialize(listEv[0])));
            Core.Configuration.JsonSettings = prevSettings;
        }

        [Test]
        public void Test_AuditDisable_AllDisabled()
        {
            var list = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .AuditDisabled(true)
                .Use(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        list.Add(ev);
                    }))
                .WithInsertOnStartReplaceOnEndCreationPolicy();

            using (var scope = new AuditScopeFactory().Create("", null, null, null, null))
            {
                scope.Save();
                scope.SaveAsync().Wait();
            }
            Audit.Core.Configuration.AuditDisabled = false;
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_AuditDisable_OnAction()
        {
            var list = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsertAndReplace(ev =>
                    {
                        list.Add(ev);
                    }))
                .WithInsertOnStartReplaceOnEndCreationPolicy()
                .WithAction(_ => _.OnEventSaving(scope =>
                {
                    Audit.Core.Configuration.AuditDisabled = true;
                }));

            using (var scope = new AuditScopeFactory().Create("", null, null, null, null))
            {
                scope.Save();
                scope.SaveAsync().Wait();
            }
            Audit.Core.Configuration.AuditDisabled = false;
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_FileLog_HappyPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "Test_FileLog_HappyPath");
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
            using (var scope = new AuditScopeFactory().Create("evt", () => target, new {X = 1}, null, null))
            {
                target = "end";
            }
            var fileFromProvider = Core.Configuration.DataProviderAs<FileDataProvider>().GetEvent($@"{dir}\evt-1.json");

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
        public void Test_ScopeSaveMode_CreateAndSave()
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

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { IsCreateAndSave = true }))
            {
                scope.Save();
            }

            Assert.That(modes.Count, Is.EqualTo(1));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
        }

        [Test]
        public void Test_ScopeSaveMode_InsertOnStartReplaceOnEnd()
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

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { }))
            {
                scope.Save();
            }

            Assert.That(modes.Count, Is.EqualTo(3));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(modes[1], Is.EqualTo(SaveMode.ReplaceOnEnd));
            Assert.That(modes[2], Is.EqualTo(SaveMode.ReplaceOnEnd));
        }

        [Test]
        public void Test_ScopeSaveMode_InsertOnStartInsertOnEnd()
        {
            var modes = new List<SaveMode>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => { })
                    .OnReplace((id, ev) => { }))
                .WithInsertOnStartInsertOnEndCreationPolicy()
                .WithAction(a => a
                    .OnEventSaving(scope =>
                    {
                        modes.Add(scope.SaveMode);
                    }));

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { }))
            {
                scope.Save();
            }

            Assert.That(modes.Count, Is.EqualTo(3));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnStart));
            Assert.That(modes[1], Is.EqualTo(SaveMode.InsertOnEnd));
            Assert.That(modes[2], Is.EqualTo(SaveMode.InsertOnEnd));
        }

        [Test]
        public void Test_ScopeSaveMode_InsertOnEnd()
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

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { }))
            {
                scope.Save();
            }

            Assert.That(modes.Count, Is.EqualTo(2));
            Assert.That(modes[0], Is.EqualTo(SaveMode.InsertOnEnd));
            Assert.That(modes[1], Is.EqualTo(SaveMode.InsertOnEnd));
        }

        [Test]
        public void Test_ScopeSaveMode_Manual()
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

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { }))
            {
                scope.Save();
            }

            Assert.That(modes.Count, Is.EqualTo(1));
            Assert.That(modes[0], Is.EqualTo(SaveMode.Manual));
        }

        [Test]
        public void Test_ScopeActionsStress()
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
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    factory.Log("LoginSuccess", new { username = "federico", id = i });
                    Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, ev =>
                    {
                        //do nothing, just bother
                        var d = ev.Event.Duration * 1234567;
                    });
                    factory.Create(new AuditScopeOptions() { EventType = "LoginFailed", ExtraFields = new { username = "adriano", id = i * -1 }, IsCreateAndSave = true });
                }));
            }
            Task.WaitAll(tasks.ToArray());
            Assert.That(counter, Is.EqualTo(MAX * 2));
            Assert.That(counter2, Is.EqualTo(MAX * 2));
            Assert.That(counter3, Is.EqualTo(MAX * 2));
        }



        [Test]
        public void Test_DynamicDataProvider()
        {
            int onInsertCount = 0, onReplaceCount = 0, onInsertOrReplaceCount = 0;
            Core.Configuration.Setup()
                .UseDynamicProvider(config => config
                    .OnInsert(ev => onInsertCount++)
                    .OnReplace((obj, ev) => onReplaceCount++)
                    .OnInsertAndReplace(ev => onInsertOrReplaceCount++));

            var scope = new AuditScopeFactory().Create("et1", null, EventCreationPolicy.Manual, null);
            scope.Save();
            scope.SetCustomField("field", "value");
            Assert.That(onInsertCount, Is.EqualTo(1));
            Assert.That(onReplaceCount, Is.EqualTo(0));
            Assert.That(onInsertOrReplaceCount, Is.EqualTo(1));
            scope.Save();
            Assert.That(onInsertCount, Is.EqualTo(1));
            Assert.That(onReplaceCount, Is.EqualTo(1));
            Assert.That(onInsertOrReplaceCount, Is.EqualTo(2));
        }

        [Test]
        public void Test_TypeExtension()
        {
            Core.Configuration.IncludeTypeNamespaces = false;
            var s = new List<Dictionary<HashSet<string>, KeyValuePair<int, decimal>>>();
            var fullname = s.GetType().GetFullTypeName();
            Assert.That(fullname, Is.EqualTo("List<Dictionary<HashSet<String>,KeyValuePair<Int32,Decimal>>>"));
            Core.Configuration.IncludeTypeNamespaces = true;
            fullname = s.GetType().GetFullTypeName();
            Assert.That(fullname, Is.EqualTo("System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.Collections.Generic.HashSet<System.String>,System.Collections.Generic.KeyValuePair<System.Int32,System.Decimal>>>"));
            Core.Configuration.IncludeTypeNamespaces = false;
            fullname = typeof(AuditEvent).GetFullTypeName();
            Assert.That(fullname, Is.EqualTo("AuditEvent"));
            Core.Configuration.IncludeTypeNamespaces = true;
            fullname = typeof(AuditEvent).GetFullTypeName();
            Assert.That(fullname, Is.EqualTo("Audit.Core.AuditEvent"));
            Core.Configuration.IncludeTypeNamespaces = false;
            var anon1 = (new { anon = true, str = "", inti = 1, otro = new { boo = true, str = "sdhdh" } }).GetType().GetFullTypeName();
            Core.Configuration.IncludeTypeNamespaces = true;
            var anon2 = (new { anon = true, str = "", inti = 1, otro = new { boo = true, str = "sdhdh" } }).GetType().GetFullTypeName();
            Assert.That(anon1, Is.EqualTo("AnonymousType<Boolean,String,Int32,AnonymousType<Boolean,String>>"));
            Assert.That(anon2, Is.EqualTo("AnonymousType<System.Boolean,System.String,System.Int32,AnonymousType<System.Boolean,System.String>>"));
        }

        [Test]
        public void Test_FluentConfig_FileLog()
        {
            int x = 0;
            Core.Configuration.Setup()
                .UseFileLogProvider(config => config.Directory(@"C:\").FilenamePrefix("prefix"))
                .WithCreationPolicy(EventCreationPolicy.Manual)
                .WithAction(action => action.OnScopeCreated(s => x++));
            var scope = new AuditScopeFactory().Create("test", null);
            scope.Dispose();
            Assert.That(Core.Configuration.DataProvider.GetType(), Is.EqualTo(typeof(FileDataProvider)));
            Assert.That(Core.Configuration.DataProviderAs<FileDataProvider>().FilenamePrefix.GetDefault(), Is.EqualTo("prefix"));
            Assert.That(Core.Configuration.DataProviderAs<FileDataProvider>().DirectoryPath.GetDefault(), Is.EqualTo(@"C:\"));
            Assert.That(Core.Configuration.CreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
            Assert.True(Core.Configuration.AuditScopeActions.ContainsKey(ActionType.OnScopeCreated));
            Assert.That(x, Is.EqualTo(1));
        }

#if NET462 || NET472
        [Test]
        public void Test_FluentConfig_EventLog()
        {
            Core.Configuration.Setup()
                .UseEventLogProvider(config => config.LogName("LogName").SourcePath("SourcePath").MachineName("MachineName"))
                .WithCreationPolicy(EventCreationPolicy.Manual);
            var scope = new AuditScopeFactory().Create("test", null);
            scope.Dispose();
            Assert.That(Core.Configuration.DataProvider.GetType(), Is.EqualTo(typeof(EventLogDataProvider)));
            Assert.That(Configuration.DataProviderAs<EventLogDataProvider>().LogName.GetDefault(), Is.EqualTo("LogName"));
            Assert.That(Configuration.DataProviderAs<EventLogDataProvider>().SourcePath.GetDefault(), Is.EqualTo("SourcePath"));
            Assert.That(Configuration.DataProviderAs<EventLogDataProvider>().MachineName.GetDefault(), Is.EqualTo("MachineName"));
            Assert.That(Core.Configuration.CreationPolicy, Is.EqualTo(EventCreationPolicy.Manual));
        }
#endif

        [Test]
        public void Test_StartAndSave()
        {
            var provider = new Mock<InMemoryDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();

            var eventType = "event type";

            new AuditScopeFactory().Create(new AuditScopeOptions() { EventType = eventType, ExtraFields = new { Extra1 = new { SubExtra1 = "test1" }, Extra2 = "test2" }, DataProvider = provider.Object, IsCreateAndSave = true });
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public void Test_CustomAction_OnCreating()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
            
            var eventType = "event type 1";
            var target = "test";
            Core.Configuration.AddOnCreatedAction(scope =>
            {
                scope.SetCustomField("custom field", "test");
                if (scope.EventType == eventType)
                {
                    scope.Discard();
                }
            });
            Core.Configuration.AddOnSavingAction(scope =>
            {
                Assert.True(false, "This should not be executed");
            });

            AuditEvent ev;
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.InsertOnStartInsertOnEnd, provider.Object))
            {
                ev = scope.Event;
            }
            Core.Configuration.ResetCustomActions();
            Assert.True(ev.CustomFields.ContainsKey("custom field"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public void Test_CustomAction_OnSaving()
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
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
                scope.Save();
            }
            Core.Configuration.ResetCustomActions();
            Assert.True(ev.Comments.Contains(comment));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_CustomAction_OnSaved()
        {
            object id = null;
            Core.Configuration.ResetCustomActions();
            Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev => ev.EventType))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                id = scope.EventId;
            });
            using (var scope = AuditScope.Create("eventType as id", null))
            {
                scope.Discard();
            }
            Core.Configuration.ResetCustomActions();

            Assert.That(id, Is.EqualTo("eventType as id"));
        }

        [Test]
        public void Test_CustomAction_OnSaving_Discard()
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
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
                scope.Save();
            }
            Core.Configuration.ResetCustomActions();
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public void Test_CustomAction_OnCreating_Double()
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
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.Manual, provider.Object))
            {
                ev = scope.Event;
            }
            Core.Configuration.ResetCustomActions();
            Assert.False(ev.CustomFields.ContainsKey(key1));
            Assert.True(ev.CustomFields.ContainsKey(key2));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public void TestSave()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
            Core.Configuration.DataProvider = provider.Object;
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.InsertOnEnd, null))
            {
                ev = scope.Event;
                scope.Comment("test");
                scope.SetCustomField<string>("custom", "value");
                target = "final";
                scope.Save(); // this should do nothing because of the creation policy (this no more true since v4.6.2)
                provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            }
            Assert.That(ev.EventType, Is.EqualTo(eventType));
            Assert.True(ev.Comments.Contains("test"));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [Test]
        public void Test_Dispose()
        {
            var provider = new Mock<AuditDataProvider>();

            using (var scope = new AuditScopeFactory().Create(null, null, EventCreationPolicy.InsertOnEnd, provider.Object))
            {
            }

            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(1));
        }

        [Test]
        public void TestDiscard()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<string>(), It.IsAny<AuditEvent>())).CallBase();
            Core.Configuration.DataProvider = provider.Object;
            var target = "initial";
            var eventType = "SomeEvent";
            AuditEvent ev;
            using (var scope = new AuditScopeFactory().Create(eventType, () => target, EventCreationPolicy.InsertOnEnd, null))
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
        }

        [Test]
        public void Test_EventCreationPolicy_InsertOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = new AuditScopeFactory().Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnEnd, null))
            {
                scope.Comment("test");
                scope.Save(); // this should do nothing because of the creation policy (this is no more true, since v 4.6.2)
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [Test]
        public void Test_EventCreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = new AuditScopeFactory().Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_EventCreationPolicy_InsertOnStartInsertOnEnd()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = new AuditScopeFactory().Create("SomeEvent", () => "target", EventCreationPolicy.InsertOnStartInsertOnEnd, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [Test]
        public void Test_EventCreationPolicy_Manual()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            Core.Configuration.DataProvider = provider.Object;
            using (var scope = new AuditScopeFactory().Create("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
            {
                scope.Comment("test");
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Never);

            using (var scope = new AuditScopeFactory().Create("SomeEvent", () => "target", EventCreationPolicy.Manual, null))
            {
                scope.Comment("test");
                scope.Save();
                scope.Comment("test2");
                scope.Save();
            }
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
        }

        [Test]
        public void Test_ExtraFields()
        {
            Core.Configuration.DataProvider = new FileDataProvider();
            var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { EventType = "SomeEvent", ExtraFields = new { @class = "class value", DATA = 123 }, CreationPolicy = EventCreationPolicy.Manual });
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.That(ev.CustomFields["DATA"].ToString(), Is.EqualTo("123"));
            Assert.That(ev.CustomFields["class"].ToString(), Is.EqualTo("class value"));
        }

        [Test]
        public void Test_TwoScopes()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            Core.Configuration.DataProvider = provider.Object;
            var scope1 = new AuditScopeFactory().Create(new AuditScopeOptions() { EventType = "SomeEvent1", ExtraFields = new { @class = "class value1", DATA = 111 }, CreationPolicy = EventCreationPolicy.Manual });
            scope1.Save();
            var scope2 = new AuditScopeFactory().Create(new AuditScopeOptions() { EventType = "SomeEvent2", ExtraFields = new { @class = "class value2", DATA = 222 }, CreationPolicy = EventCreationPolicy.Manual });
            scope2.Save();
            Assert.NotNull(scope1.EventId);
            Assert.NotNull(scope2.EventId);
            Assert.That(scope2.EventId, Is.Not.EqualTo(scope1.EventId));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
        }

        [Test]
        public void AuditActivityTrace_Serialization()
        {
            // Arrange
            var trace = new AuditActivityTrace() { SpanId = "span" };
            var json = trace.ToJson();

            // Act
            var traceRead = AuditActivityTrace.FromJson(json);

            // Assert
            Assert.That(traceRead.SpanId, Is.EqualTo(trace.SpanId).And.EqualTo("span"));
        }

        [Test]
        public void AuditEventEnvironment_Serialization()
        {
            // Arrange
            var env = new AuditEventEnvironment() { MachineName = "machine"};
            var json = env.ToJson();

            // Act
            var envRead = AuditEventEnvironment.FromJson(json);
            
            // Assert
            Assert.That(envRead.MachineName, Is.EqualTo(env.MachineName).And.EqualTo("machine"));
        }

        [Test]
        public void Test_ScopeDisposed_Action()
        {
            var disposed = 0;
            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithInsertOnEndCreationPolicy()
                .WithAction(actions => actions
                    .OnScopeDisposed(scope =>
                    {
                        disposed++;
                    }));

            AuditScope.Log("Test1", null);
            AuditScope.Log("Test2", null);

            using (var scope = AuditScope.Create("Test", null))
            {
                Assert.That(disposed, Is.EqualTo(2));
            }

            Assert.That(disposed, Is.EqualTo(3));
        }

        [Test]
        public void Test_ScopeDisposed_Order()
        {
            var actions = new List<string>();

            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider()
                .WithInsertOnEndCreationPolicy()
                .WithAction(a => a.OnScopeCreated(scope => actions.Add("Created")))
                .WithAction(a => a.OnScopeDisposed(scope => actions.Add("Disposed")))
                .WithAction(a => a.OnEventSaved(scope => actions.Add("Saved")))
                .WithAction(a => a.OnEventSaving(scope => actions.Add("Saving")));

            AuditScope.Log("Test1", null);

            Assert.That(actions.Count, Is.EqualTo(4));
            Assert.That(actions[0], Is.EqualTo("Created"));
            Assert.That(actions[1], Is.EqualTo("Saving"));
            Assert.That(actions[2], Is.EqualTo("Saved"));
            Assert.That(actions[3], Is.EqualTo("Disposed"));
        }

        public class SomeClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
