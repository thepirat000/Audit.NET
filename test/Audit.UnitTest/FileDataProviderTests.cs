using Newtonsoft.Json;
using Audit.Core;
using Audit.Core.Providers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
            AuditScope.CreateAndSave(guid, loop);

            var ev = fdp.GetEvent(Path.Combine(_directory, guid));

            Assert.IsNotNull(ev);
            Assert.AreEqual(guid, ev.EventType);
            Assert.AreEqual(2, ev.CustomFields.Count);
            Assert.AreEqual(1, ev.CustomFields["Id"]);
            Assert.AreEqual(JObject.Parse("{\"Id\": 1}").ToString(), ev.CustomFields["Inner"].ToString());
        }

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
            await AuditScope.CreateAndSaveAsync(guid, loop);

            var ev = await fdp.GetEventAsync(Path.Combine(_directory, guid));

            Assert.IsNotNull(ev);
            Assert.AreEqual(guid, ev.EventType);
            Assert.AreEqual(2, ev.CustomFields.Count);
            Assert.AreEqual(1, ev.CustomFields["Id"]);
            Assert.AreEqual(JObject.Parse("{\"Id\": 1}").ToString(), ev.CustomFields["Inner"].ToString());
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

            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType)
                .JsonSettings(new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Error }));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                AuditScope.CreateAndSave(guid, loop);
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (JsonSerializationException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("loop detected"));
            }
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

            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType)
                .JsonSettings(new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Error }));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                await AuditScope.CreateAndSaveAsync(guid, loop);
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (JsonSerializationException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("loop detected"));
            }
        }

    }
}
