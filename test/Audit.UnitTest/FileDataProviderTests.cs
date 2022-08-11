using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
#if NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

namespace Audit.UnitTest
{
    public class Loop
    {
        public int Id { get; set; }
        public Loop Inner { get; set; }
    }

    [TestFixture]
    public class FileDataProviderTests
    {
        private string _directory;

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.DataProvider = new FileDataProvider();
            Audit.Core.Configuration.AuditDisabled = false;
            Audit.Core.Configuration.ResetCustomActions();
            _directory = Path.Combine(Path.GetTempPath(), "events");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(_directory, true);
            }
            catch (Exception)
            {
            }
        }

#if NK_JSON
        [Test]
        public void Test_FileDataProvider_Loop()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;
            
            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            new AuditScopeFactory().Log(guid, loop);

            var ev = fdp.GetEvent(Path.Combine(_directory, guid));

            Assert.IsNotNull(ev);
            Assert.AreEqual(guid, ev.EventType);
            Assert.AreEqual(2, ev.CustomFields.Count);
            Assert.AreEqual(1, ev.CustomFields["Id"]);
            Assert.AreEqual("{\r\n  \"Id\": 1\r\n}", ev.CustomFields["Inner"].ToString());
        }
#else
        [Test]
        public void Test_FileDataProvider_Loop()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();

#if NET6_0_OR_GREATER                  
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };
#endif

            try
            {
                new AuditScopeFactory().Log(guid, loop);
                Assert.Fail("Should have thrown JsonException");
            }
            catch (JsonException ex)
            {
                if (!ex.Message.Contains("cycle"))
                {
                    Assert.Fail("Should have failed with JsonException object cycle");
                }
            }
#if NET6_0_OR_GREATER
            Configuration.JsonSettings = prevSettings;
#endif
            Assert.IsFalse(File.Exists(Path.Combine(_directory, guid)));
        }
#endif
#if NK_JSON
        [Test]
        public async Task Test_FileDataProvider_LoopAsync()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            await new AuditScopeFactory().CreateAsync(new AuditScopeOptions(guid, extraFields: loop, isCreateAndSave: true));

            var ev = await fdp.GetEventAsync(Path.Combine(_directory, guid));

            Assert.IsNotNull(ev);
            Assert.AreEqual(guid, ev.EventType);
            Assert.AreEqual(2, ev.CustomFields.Count);
            Assert.AreEqual(1, ev.CustomFields["Id"]);
            Assert.AreEqual("{\r\n  \"Id\": 1\r\n}", ev.CustomFields["Inner"].ToString());
        }
#else
        [Test]
        public async Task Test_FileDataProvider_LoopAsync()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();

#if NET6_0_OR_GREATER                  
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };
#endif

            try
            {
                await new AuditScopeFactory().LogAsync(guid, loop);
                Assert.Fail("Should have thrown JsonException");
            }
            catch (JsonException ex)
            {
                if (!ex.Message.Contains("cycle"))
                {
                    Assert.Fail("Should have failed with JsonException object cycle");
                }
            }
#if NET6_0_OR_GREATER
            Configuration.JsonSettings = prevSettings;
#endif
            Assert.IsTrue(File.Exists(Path.Combine(_directory, guid)));
            Assert.IsEmpty(File.ReadAllText(Path.Combine(_directory, guid)));
        }
#endif
            [Test]
        public void Test_FileDataProvider_Indent()
        {
            var loop = new Loop() { Id = 1 };

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

#if NK_JSON
            Configuration.JsonSettings.Formatting = Formatting.Indented;
#else
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { WriteIndented = true };
#endif
            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            new AuditScopeFactory().Log(guid, loop);

            var fileContents = File.ReadAllText(Path.Combine(_directory, guid));
#if NK_JSON
            Configuration.JsonSettings.Formatting = Formatting.None;
#else
            Configuration.JsonSettings = prevSettings;
#endif

            Assert.IsNotNull(fileContents);
            Assert.IsTrue(fileContents.StartsWith("{\r\n"));
        }

        [Test]
        public async Task Test_FileDataProvider_IndentAsync()
        {
            var loop = new Loop() { Id = 1 };

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

#if NK_JSON
            Configuration.JsonSettings.Formatting = Formatting.Indented;
#else
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { WriteIndented = true };
#endif

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            await new AuditScopeFactory().LogAsync(guid, loop);

            var fileContents = File.ReadAllText(Path.Combine(_directory, guid));
#if NK_JSON
            Configuration.JsonSettings.Formatting = Formatting.None;
#else
            Configuration.JsonSettings = prevSettings;
#endif

            Assert.IsNotNull(fileContents);
            Assert.IsTrue(fileContents.StartsWith("{\r\n"));
        }

        [Test]
        public void Test_FileDataProvider_Error()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };
#if NK_JSON
            Configuration.JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
#elif NET6_0_OR_GREATER
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };
#endif
            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                new AuditScopeFactory().Create(new AuditScopeOptions(guid, extraFields: loop, isCreateAndSave: true));
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("loop detected") || ex.Message.ToLower().Contains("cycle"));
            }
#if NK_JSON
            Configuration.JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
#elif NET6_0_OR_GREATER
            Configuration.JsonSettings = prevSettings;
#endif
        }

        [Test]
        public async Task Test_FileDataProvider_ErrorAsync()
        {
            var loop = new Loop() { Id = 1 };
            loop.Inner = loop;

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };
#if NK_JSON
            Configuration.JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Error;
#elif NET6_0_OR_GREATER
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };
#endif
            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                await new AuditScopeFactory().CreateAsync(new AuditScopeOptions(guid, extraFields: loop, isCreateAndSave: true));
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("loop detected") || ex.Message.ToLower().Contains("cycle"));
            }
#if NK_JSON
            Configuration.JsonSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
#elif NET6_0_OR_GREATER
            Configuration.JsonSettings = prevSettings;
#endif
        }
    }
}
