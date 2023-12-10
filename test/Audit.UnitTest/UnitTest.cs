using Audit.Core;
using System;
using Moq;
using Audit.Core.Providers;
using Audit.EntityFramework;
using System.Collections.Generic;
using Audit.Core.Extensions;
using NUnit.Framework;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Audit.EntityFramework.ConfigurationApi;
using System.Reflection;
using Configuration = Audit.Core.Configuration;
using InMemoryDataProvider = Audit.Core.Providers.InMemoryDataProvider;
#if NETCOREAPP3_0_OR_GREATER || NET20_OR_GREATER
using System.Data.Entity.Infrastructure;
#endif
#if NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

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
            Configuration.DataProvider = new InMemoryDataProvider();
            Configuration.AuditDisabled = true;
            Configuration.AddOnCreatedAction(s => { });
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            
            Configuration.Reset();

            Assert.AreEqual(false, Configuration.IncludeTypeNamespaces);
            Assert.AreEqual(false, Configuration.IncludeStackTrace);
            Assert.AreEqual(false, Configuration.IncludeActivityTrace);
            Assert.IsInstanceOf<FileDataProvider>(Configuration.DataProvider);
            Assert.AreEqual(false, Configuration.AuditDisabled);
            Assert.AreEqual(0, Configuration.AuditScopeActions[ActionType.OnScopeCreated].Count);
            Assert.AreEqual(EventCreationPolicy.InsertOnEnd, Configuration.CreationPolicy);
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

            using (var scope = AuditScope.Create("test", null))
            {
                scope.Save();
            }

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Comments.Contains("OnScopeCreated"));
            Assert.IsTrue(evs[0].Comments.Contains("OnEventSaving"));
            Assert.IsTrue(saved);
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

            Assert.AreEqual(1, evs.Count);
            Assert.IsTrue(evs[0].Comments.Contains("OnScopeCreated"));
            Assert.IsTrue(evs[0].Comments.Contains("OnEventSaving"));
            Assert.IsTrue(saved);
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
            using (var scope = AuditScope.Create(options))
            {
            }
            // scope with event type to override
            options.EventType = "override";
            using (var scope = AuditScope.Create(options))
            {
            }
            Assert.AreEqual(2, evs.Count);
            Assert.AreEqual("test", evs[0].EventType);
            Assert.AreEqual("override", evs[1].EventType);
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

            Assert.AreEqual(1, ins);
            Assert.AreEqual(1, upd);
        }

        [Test]
        public void Test_AuditScope_Factory_FluentApi()
        {
            var mb = typeof(UnitTest).GetTypeInfo().GetMethods().First();

            var scope = new AuditScopeFactory().Create(_ => _
                 .EventType("event type")
                 .ExtraFields(new { f = 1 })
                 .CreationPolicy(EventCreationPolicy.Manual)
                 .AuditEvent(new AuditEventEntityFramework())
                 .DataProvider(new DynamicDataProvider())
                 .IsCreateAndSave(true)
                 .SkipExtraFrames(1)
                 .Target(() => 1)
                 .CallingMethod(mb));

            Assert.AreEqual("event type", scope.EventType);
            Assert.AreEqual("event type", scope.Event.EventType);
            Assert.IsTrue(scope.Event.CustomFields.ContainsKey("f"));
            Assert.AreEqual(EventCreationPolicy.Manual, scope.EventCreationPolicy);
            Assert.AreEqual(typeof(AuditEventEntityFramework), scope.Event.GetType());
            Assert.AreEqual(typeof(DynamicDataProvider), scope.DataProvider.GetType());
            Assert.AreEqual(SaveMode.InsertOnStart, scope.SaveMode);
            Assert.AreEqual("1", scope.Event.Target.Old.ToString());
            Assert.IsTrue(scope.Event.Environment.CallingMethodName.Contains(mb.Name));
        }

        [Test]
        public void Test_AuditScope_Create_FluentApi()
        {
            var mb = typeof(UnitTest).GetTypeInfo().GetMethods().First();

            var scope = AuditScope.Create(_ => _
                .EventType("event type")
                .ExtraFields(new { f = 1 })
                .CreationPolicy(EventCreationPolicy.Manual)
                .AuditEvent(new AuditEventEntityFramework())
                .DataProvider(new DynamicDataProvider())
                .IsCreateAndSave(true)
                .SkipExtraFrames(1)
                .Target(() => 1)
                .CallingMethod(mb));

            Assert.AreEqual("event type", scope.EventType);
            Assert.AreEqual("event type", scope.Event.EventType);
            Assert.IsTrue(scope.Event.CustomFields.ContainsKey("f"));
            Assert.AreEqual(EventCreationPolicy.Manual, scope.EventCreationPolicy);
            Assert.AreEqual(typeof(AuditEventEntityFramework), scope.Event.GetType());
            Assert.AreEqual(typeof(DynamicDataProvider), scope.DataProvider.GetType());
            Assert.AreEqual(SaveMode.InsertOnStart, scope.SaveMode);
            Assert.AreEqual("1", scope.Event.Target.Old.ToString());
            Assert.IsTrue(scope.Event.Environment.CallingMethodName.Contains(mb.Name));
        }

        [Test]
        public void Test_AuditScope_Log()
        {
            Audit.Core.Configuration.SystemClock = new MyClock();
            var evs = new List<AuditEvent>();
            Audit.Core.Configuration.Setup()
                .Use(x => x.OnInsertAndReplace(ev => { evs.Add(ev); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            AuditScope.Log("test", new { field1 = "one" });

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("test", evs[0].EventType);
            Assert.AreEqual("one", evs[0].CustomFields["field1"].ToString());
#if !NETCOREAPP1_0
            Assert.IsTrue(evs[0].Environment.CallingMethodName.Contains("Test_AuditScope_Log"));
#endif
        }

#if !NETCOREAPP1_0
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

            Assert.AreEqual(2, evs.Count);
            Assert.IsTrue(evs[0].Environment.CallingMethodName.Contains("Test_AuditScope_CallingMethod"));
            Assert.IsTrue(evs[1].Environment.CallingMethodName.Contains("Test_AuditScope_CallingMethod"));
        }
#endif

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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual(10000, evs[0].Duration);
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), evs[0].StartDate);
            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 10), evs[0].EndDate);
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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("SomeClass", evs[0].Target.Type);
            
            Assert.AreEqual(1, JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.Old)).Id);
            Assert.AreEqual("Test", JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.Old)).Name);
            Assert.AreEqual(2, JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.New)).Id);
            Assert.AreEqual("NewTest", JsonAdapter.Deserialize<SomeClass>(JsonAdapter.Serialize(evs[0].Target.New)).Name);
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

            Assert.AreEqual(1, evs.Count);
            Assert.AreEqual("Object", evs[0].Target.Type);
            Assert.IsNull(evs[0].Target.Old);
            Assert.IsNull(evs[0].Target.New);
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

            Assert.AreEqual(1, evs.Count);
            Assert.IsNull(evs[0].Target);
        }

#if NK_JSON
        [Test]
        public void Test_AuditEvent_CustomSerializer_JsonNet()
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

            Core.Configuration.JsonSettings.TypeNameHandling = TypeNameHandling.All;

            using (var scope = new AuditScopeFactory().Create("TEST", null, null, null, null))
            {
            }
            
            Assert.AreEqual(1, listEv.Count);

            var manualJson = listEv[0].ToJson();

            Assert.AreEqual(1, listJson.Count);

            var jsonExpected = JsonConvert.SerializeObject(listEv[0], Core.Configuration.JsonSettings);
            Assert.AreEqual(jsonExpected, listJson[0]);
            Assert.AreEqual(jsonExpected, manualJson);
            Assert.AreEqual(JsonAdapter.Serialize(listEv[0]), listEv[0].ToJson());
            Core.Configuration.JsonSettings.TypeNameHandling = TypeNameHandling.None;
        }
#else
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
            
            Assert.AreEqual(1, listEv.Count);

            var manualJson = listEv[0].ToJson();

            Assert.AreEqual(1, listJson.Count);

            var jsonExpected = JsonSerializer.Serialize(listEv[0], Core.Configuration.JsonSettings);
            Assert.IsTrue(jsonExpected.Count(c => c == '\n') > 5);
            Assert.AreEqual(jsonExpected, listJson[0]);
            Assert.AreEqual(jsonExpected, manualJson);
            Assert.AreEqual(JsonAdapter.Serialize(listEv[0]), listEv[0].ToJson());
            Core.Configuration.JsonSettings = prevSettings;
        }

#endif

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
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            using (var scope = new AuditScopeFactory().Create("", null, null, null, null))
            {
                scope.Save();
                scope.SaveAsync().Wait();
            }
            Audit.Core.Configuration.AuditDisabled = false;
            Assert.AreEqual(0, list.Count);
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
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
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
            Assert.AreEqual(0, list.Count);
        }

#if NETCOREAPP3_0_OR_GREATER || NET20_OR_GREATER
        [Test]
        public void Test_EF_MergeEntitySettings()
        {
            var now = DateTime.Now;
            var helper = new DbContextHelper();
            var attr = new Dictionary<Type, EfEntitySettings>();
            var local = new Dictionary<Type, EfEntitySettings>();
            var global = new Dictionary<Type, EfEntitySettings>();
            attr[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I1" }),
                OverrideProperties = new Dictionary<string, Func<DbEntityEntry, object>>() { { "C1", _ => 1 }, { "C2", _ => "ATTR" } }
            };
            local[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I1", "I2" }),
                OverrideProperties = new Dictionary<string, Func<DbEntityEntry, object>>() { { "C2", _ => "LOCAL" }, { "C3", _ => now } } 
            };
            global[typeof(string)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I3" }),
                OverrideProperties = new Dictionary<string, Func<DbEntityEntry, object>>() { { "C2", _ => "GLOBAL" }, { "C4", _ => null } }
            };

            attr[typeof(int)] = new EfEntitySettings()
            {
                IgnoredProperties = new HashSet<string>(new[] { "I3" }),
                OverrideProperties = new Dictionary<string, Func<DbEntityEntry, object>>() { { "C2", _ => "INT" }, { "C4", _ => null } }
            };

            var merged = helper.MergeEntitySettings(attr, local, global);
            var mustbenull1 = helper.MergeEntitySettings(null, null, null);
            var mustbenull2 = helper.MergeEntitySettings(null, new Dictionary<Type, EfEntitySettings>(), null);
            var mustbenull3 = helper.MergeEntitySettings(new Dictionary<Type, EfEntitySettings>(), new Dictionary<Type, EfEntitySettings>(), new Dictionary<Type, EfEntitySettings>());
            Assert.AreEqual(2, merged.Count);
            Assert.IsNull(mustbenull1);
            Assert.IsNull(mustbenull2);
            Assert.IsNull(mustbenull3);
            var merge = merged[typeof(string)];
            Assert.AreEqual(3, merge.IgnoredProperties.Count);
            Assert.IsTrue(merge.IgnoredProperties.Contains("I1"));
            Assert.IsTrue(merge.IgnoredProperties.Contains("I2"));
            Assert.IsTrue(merge.IgnoredProperties.Contains("I3"));
            Assert.AreEqual(4, merge.OverrideProperties.Count);
            Assert.AreEqual(1, merge.OverrideProperties["C1"].Invoke(null));
            Assert.AreEqual("ATTR", merge.OverrideProperties["C2"].Invoke(null));
            Assert.AreEqual(now, merge.OverrideProperties["C3"].Invoke(null));
            Assert.AreEqual(null, merge.OverrideProperties["C4"].Invoke(null));
            merge = merged[typeof(int)];
            Assert.AreEqual(1, merge.IgnoredProperties.Count);
            Assert.IsTrue(merge.IgnoredProperties.Contains("I3"));
            Assert.AreEqual("INT", merge.OverrideProperties["C2"].Invoke(null));
            Assert.AreEqual(null, merge.OverrideProperties["C4"].Invoke(null));
        }
#endif
        
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
            var fileFromProvider = (Audit.Core.Configuration.DataProvider as FileDataProvider).GetEvent($@"{dir}\evt-1.json");

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

            Assert.AreEqual(1, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
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

            Assert.AreEqual(3, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
            Assert.AreEqual(SaveMode.ReplaceOnEnd, modes[1]);
            Assert.AreEqual(SaveMode.ReplaceOnEnd, modes[2]);
        }

        [Test]
        public void Test_ScopeSaveMode_InsertOnStartInsertOnEnd()
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

            using (var scope = new AuditScopeFactory().Create(new AuditScopeOptions() { }))
            {
                scope.Save();
            }

            Assert.AreEqual(3, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnStart, modes[0]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[1]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[2]);
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

            Assert.AreEqual(2, modes.Count);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[0]);
            Assert.AreEqual(SaveMode.InsertOnEnd, modes[1]);
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

            Assert.AreEqual(1, modes.Count);
            Assert.AreEqual(SaveMode.Manual, modes[0]);
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
                    factory.Create(new AuditScopeOptions("LoginFailed", null, new { username = "adriano", id = i * -1 }, null, null, true));
                }));
            }
            Task.WaitAll(tasks.ToArray());
            Assert.AreEqual(MAX * 2, counter);
            Assert.AreEqual(MAX * 2, counter2);
            Assert.AreEqual(MAX * 2, counter3);
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
            Assert.AreEqual(1, onInsertCount);
            Assert.AreEqual(0, onReplaceCount);
            Assert.AreEqual(1, onInsertOrReplaceCount);
            scope.Save();
            Assert.AreEqual(1, onInsertCount);
            Assert.AreEqual(1, onReplaceCount);
            Assert.AreEqual(2, onInsertOrReplaceCount);
        }

        [Test]
        public void Test_TypeExtension()
        {
            Core.Configuration.IncludeTypeNamespaces = false;
            var s = new List<Dictionary<HashSet<string>, KeyValuePair<int, decimal>>>();
            var fullname = s.GetType().GetFullTypeName();
            Assert.AreEqual("List<Dictionary<HashSet<String>,KeyValuePair<Int32,Decimal>>>", fullname);
            Core.Configuration.IncludeTypeNamespaces = true;
            fullname = s.GetType().GetFullTypeName();
            Assert.AreEqual("System.Collections.Generic.List<System.Collections.Generic.Dictionary<System.Collections.Generic.HashSet<System.String>,System.Collections.Generic.KeyValuePair<System.Int32,System.Decimal>>>", fullname);
            Core.Configuration.IncludeTypeNamespaces = false;
            fullname = typeof(AuditEvent).GetFullTypeName();
            Assert.AreEqual("AuditEvent", fullname);
            Core.Configuration.IncludeTypeNamespaces = true;
            fullname = typeof(AuditEvent).GetFullTypeName();
            Assert.AreEqual("Audit.Core.AuditEvent", fullname);
            Core.Configuration.IncludeTypeNamespaces = false;
            var anon1 = (new { anon = true, str = "", inti = 1, otro = new { boo = true, str = "sdhdh" } }).GetType().GetFullTypeName();
            Core.Configuration.IncludeTypeNamespaces = true;
            var anon2 = (new { anon = true, str = "", inti = 1, otro = new { boo = true, str = "sdhdh" } }).GetType().GetFullTypeName();
            Assert.AreEqual("AnonymousType<Boolean,String,Int32,AnonymousType<Boolean,String>>", anon1);
            Assert.AreEqual("AnonymousType<System.Boolean,System.String,System.Int32,AnonymousType<System.Boolean,System.String>>", anon2);
        }

        [Test]
        public void Test_EntityFramework_Config_Precedence()
        {
            EntityFramework.Configuration.Setup()
                .ForContext<MyContext>(x => x.AuditEventType("ForContext"))
                .UseOptIn();
            EntityFramework.Configuration.Setup()
                .ForAnyContext(x => x.AuditEventType("ForAnyContext").IncludeEntityObjects(true).ExcludeValidationResults(true))
                .UseOptOut();

            var ctx = new MyContext();
            var ctx2 = new AnotherContext();

            Assert.AreEqual("FromAttr", ctx.AuditEventType);
            Assert.AreEqual(true, ctx.IncludeEntityObjects);
            Assert.AreEqual(true, ctx.ExcludeValidationResults);
            Assert.AreEqual(AuditOptionMode.OptIn, ctx.Mode);

            Assert.AreEqual("ForAnyContext", ctx2.AuditEventType);
            Assert.AreEqual(AuditOptionMode.OptOut, ctx2.Mode);
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
            Assert.AreEqual(typeof(FileDataProvider), Core.Configuration.DataProvider.GetType());
            Assert.AreEqual("prefix", (Core.Configuration.DataProvider as FileDataProvider).FilenamePrefix);
            Assert.AreEqual(@"C:\", (Core.Configuration.DataProvider as FileDataProvider).DirectoryPath);
            Assert.AreEqual(EventCreationPolicy.Manual, Core.Configuration.CreationPolicy);
            Assert.True(Core.Configuration.AuditScopeActions.ContainsKey(ActionType.OnScopeCreated));
            Assert.AreEqual(1, x);
        }
#if NET461 || NETCOREAPP2_0 || NET5_0_OR_GREATER
        [Test]
        public void Test_FluentConfig_EventLog()
        {
            Core.Configuration.Setup()
                .UseEventLogProvider(config => config.LogName("LogName").SourcePath("SourcePath").MachineName("MachineName"))
                .WithCreationPolicy(EventCreationPolicy.Manual);
            var scope = new AuditScopeFactory().Create("test", null);
            scope.Dispose();
            Assert.AreEqual(typeof(EventLogDataProvider), Core.Configuration.DataProvider.GetType());
            Assert.AreEqual("LogName", (Core.Configuration.DataProvider as EventLogDataProvider).LogName);
            Assert.AreEqual("SourcePath", (Core.Configuration.DataProvider as EventLogDataProvider).SourcePath);
            Assert.AreEqual("MachineName", (Core.Configuration.DataProvider as EventLogDataProvider).MachineName);
            Assert.AreEqual(EventCreationPolicy.Manual, Core.Configuration.CreationPolicy);
        }
#endif
        [Test]
        public void Test_StartAndSave()
        {
            var provider = new Mock<InMemoryDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();

            var eventType = "event type";

            new AuditScopeFactory().Create(new AuditScopeOptions(eventType, null, new { Extra1 = new { SubExtra1 = "test1" }, Extra2 = "test2" }, provider.Object, null, true));
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            provider.Verify(p => p.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Never);
        }

        [Test]
        public void Test_CustomAction_OnCreating()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
            
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

            Assert.AreEqual("eventType as id", id);
        }

        [Test]
        public void Test_CustomAction_OnSaving_Discard()
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
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
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
            Assert.AreEqual(eventType, ev.EventType);
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
            provider.Setup(p => p.Serialize(It.IsAny<string>())).CallBase();
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
            Assert.AreEqual(eventType, ev.EventType);
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
            var scope = new AuditScopeFactory().Create(new AuditScopeOptions("SomeEvent", null, new { @class = "class value", DATA = 123 }, null, EventCreationPolicy.Manual));
            scope.Comment("test");
            var ev = scope.Event;
            scope.Discard();
            Assert.AreEqual("123", ev.CustomFields["DATA"].ToString());
            Assert.AreEqual("class value", ev.CustomFields["class"].ToString());
        }

        [Test]
        public void Test_TwoScopes()
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns(() => Guid.NewGuid());
            Core.Configuration.DataProvider = provider.Object;
            var scope1 = new AuditScopeFactory().Create(new AuditScopeOptions("SomeEvent1", null, new { @class = "class value1", DATA = 111 }, null, EventCreationPolicy.Manual));
            scope1.Save();
            var scope2 = new AuditScopeFactory().Create(new AuditScopeOptions("SomeEvent2", null, new { @class = "class value2", DATA = 222 }, null, EventCreationPolicy.Manual));
            scope2.Save();
            Assert.NotNull(scope1.EventId);
            Assert.NotNull(scope2.EventId);
            Assert.AreNotEqual(scope1.EventId, scope2.EventId);
            provider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Exactly(2));
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
        public class SomeClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
