using Audit.Core;
using Audit.Core.Providers;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            Audit.Core.Configuration.Reset();
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

            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };

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
            Configuration.JsonSettings = prevSettings;
            Assert.IsFalse(File.Exists(Path.Combine(_directory, guid)));
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

            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };

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
            Configuration.JsonSettings = prevSettings;
            Assert.That(File.Exists(Path.Combine(_directory, guid)), Is.True);
            Assert.IsEmpty(File.ReadAllText(Path.Combine(_directory, guid)));
        }

        [Test]
        public void Test_FileDataProvider_Indent()
        {
            var loop = new Loop() { Id = 1 };

            var fdp = new FileDataProvider()
            {
                DirectoryPath = _directory,
                FilenameBuilder = x => x.EventType
            };

            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { WriteIndented = true };
            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            new AuditScopeFactory().Log(guid, loop);

            var fileContents = File.ReadAllText(Path.Combine(_directory, guid));
            Configuration.JsonSettings = prevSettings;

            Assert.That(fileContents, Is.Not.Null);
            Assert.That(fileContents.StartsWith("{" + Environment.NewLine), Is.True);
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

            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { WriteIndented = true };

            Configuration.DataProvider = fdp;
            var guid = "x" + Guid.NewGuid().ToString();
            await new AuditScopeFactory().LogAsync(guid, loop);

            var fileContents = File.ReadAllText(Path.Combine(_directory, guid));
            Configuration.JsonSettings = prevSettings;

            Assert.That(fileContents, Is.Not.Null);
            Assert.That(fileContents.StartsWith("{" + Environment.NewLine), Is.True);
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

            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };

            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                new AuditScopeFactory().Create(new AuditScopeOptions() { EventType = guid, ExtraFields = loop, IsCreateAndSave = true });
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message.ToLower().Contains("loop detected") || ex.Message.ToLower().Contains("cycle"), Is.True);
            }

            Configuration.JsonSettings = prevSettings;
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
            var prevSettings = Configuration.JsonSettings;
            Configuration.JsonSettings = new JsonSerializerOptions() { MaxDepth = 1, ReferenceHandler = ReferenceHandler.Preserve };

            Configuration.Setup().UseFileLogProvider(_ => _
                .Directory(_directory)
                .FilenameBuilder(x => x.EventType));

            var guid = "x" + Guid.NewGuid().ToString();
            try
            {
                await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = guid, ExtraFields = loop, IsCreateAndSave = true });
                Assert.Fail("Should not get here. JsonSettings not respected?");
            }
            catch (Exception ex)
            {
                Assert.That(ex.Message.ToLower().Contains("loop detected") || ex.Message.ToLower().Contains("cycle"), Is.True);
            }

            Configuration.JsonSettings = prevSettings;
        }

        [Test]
        public void Test_FileDataProvider_FluentApi()
        {
            var x = new FileDataProvider(_ => _
                .Directory(@"Directory")
                .FilenamePrefix("FilenamePrefix")
            );
            Assert.That(x.DirectoryPath.GetDefault(), Is.EqualTo("Directory"));
            Assert.That(x.FilenamePrefix.GetDefault(), Is.EqualTo("FilenamePrefix"));
        }

        [Test]
        public void Test_FileDataProvider_FluentApi_Func()
        {
            var x = new FileDataProvider(_ => _
                .DirectoryBuilder(ev => @"Directory")
                .FilenameBuilder(ev => "Filename")
            );
            Assert.That(x.DirectoryPath.GetValue(new AuditEvent()), Is.EqualTo("Directory"));
            Assert.That(x.FilenameBuilder.Invoke(new AuditEvent()), Is.EqualTo("Filename"));
        }

    }
}
