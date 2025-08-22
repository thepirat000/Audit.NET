using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Audit.Core.Providers;

namespace Audit.FileSystem.UnitTest
{
    [TestFixture]
    public class FileSystemTests
    {
        private static Random random = new Random();

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_FileDataProvider_FluentApi()
        {
            var x = new FileDataProvider(_ => _
                .Directory(@"c:\t")
                .FilenameBuilder(ev => "fn")
                .FilenamePrefix("px"));

            Assert.That(x.DirectoryPath.GetDefault(), Is.EqualTo(@"c:\t"));
            Assert.That(x.FilenameBuilder.Invoke(null), Is.EqualTo("fn"));
            Assert.That(x.FilenamePrefix.GetDefault(), Is.EqualTo("px"));
        }

        [Test]
        public void Test_FileSystem_1()
        {
            var folder = Path.Combine(Path.GetTempPath(), random.Next(1000, 9999).ToString());
            System.IO.Directory.CreateDirectory(folder);
            
            var filename1 = $"test_{random.Next(1000, 9999)}.txt";
            var t1path = Path.Combine(folder, filename1);
            File.Delete(t1path);
            var locker = new object();

            var evs = new List<FileSystemEvent>();

            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev => {
                    lock (locker) {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Options.IncludeContentPredicate = _ => ContentType.Text;
            fsMon.Start();

            Thread.Sleep(500);
            
            File.WriteAllText(t1path, "this is a test");

            Thread.Sleep(1500);
            
            File.Delete(t1path);

            Thread.Sleep(1500);

            Assert.That(evs.Count >= 2, Is.True, $"Events: {evs.Count}");
            var create = evs.LastOrDefault(x => x.Event == FileSystemEventType.Create);
            Assert.That(create, Is.Not.Null);
            Assert.That(create.Event, Is.EqualTo(FileSystemEventType.Create));
            Assert.That(create.Name, Is.EqualTo(filename1));
            Assert.That(create.Length, Is.EqualTo(14));
            Assert.That(create.FileContent.Type, Is.EqualTo(ContentType.Text));
            Assert.That((create.FileContent as FileTextualContent)?.Value, Is.EqualTo("this is a test"));
            Assert.That(create.MD5, Is.Not.Null);
            
            var delete = evs.LastOrDefault(x => x.Event == FileSystemEventType.Delete);
            Assert.That(delete, Is.Not.Null);
            Assert.That(delete.Name, Is.EqualTo(filename1));

            Directory.Delete(folder, true);
        }

    }
}
