using Audit.Core.Providers;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Audit.FileSystem.UnitTest
{
    [TestFixture]
    public class FileSystemTests
    {
        private static readonly Random Random = new Random();

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

#if !NETCOREAPP3_1
        [Test]
        public void Test_FileSystem_Text()
        {
            var folder = Path.Combine(Path.GetTempPath(), Random.Next(1000, 9999).ToString());
            Directory.CreateDirectory(folder);
            
            var filename1 = $"test_{Random.Next(1000, 9999)}.txt";
            var t1Path = Path.Combine(folder, filename1);
            File.Delete(t1Path);
            var locker = new object();

            var evs = new List<FileSystemEvent>();

            Core.Configuration.Setup()
                .UseDynamicProvider(d => d.OnInsert(ev => {
                    lock (locker) {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Options.IncludeContentPredicate = _ => ContentType.Text;
            fsMon.Start();

            Thread.Sleep(500);
            
            File.WriteAllText(t1Path, "this is a test");

            Thread.Sleep(1500);
            
            File.Delete(t1Path);

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
#endif

        [Test]
        public void Test_FileSystem_Binary()
        {
            var folder = Path.Combine(Path.GetTempPath(), Random.Next(1000, 9999).ToString());
            Directory.CreateDirectory(folder);

            var filename1 = $"test_{Random.Next(1000, 9999)}.txt";
            var t1Path = Path.Combine(folder, filename1);
            File.Delete(t1Path);
            var locker = new object();

            var evs = new List<FileSystemEvent>();

            Core.Configuration.Setup()
                .UseDynamicProvider(d => d.OnInsert(ev => {
                    lock (locker)
                    {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Options.IncludeContentPredicate = _ => ContentType.Binary;
            fsMon.Start();

            Thread.Sleep(500);

            File.WriteAllBytes(t1Path, "MZ123"u8.ToArray());

            Thread.Sleep(1500);

            Assert.That(evs.Count >= 1, Is.True, $"Events: {evs.Count}");
            var create = evs.LastOrDefault(x => x.Event == FileSystemEventType.Create);
            Assert.That(create, Is.Not.Null);
            Assert.That(create.Event, Is.EqualTo(FileSystemEventType.Create));
            Assert.That(create.Name, Is.EqualTo(filename1));
            Assert.That(create.Length, Is.EqualTo(5));
            Assert.That(create.FileContent.Type, Is.EqualTo(ContentType.Binary));
            Assert.That((create.FileContent as FileBinaryContent)?.Value, Is.EqualTo("MZ123"u8.ToArray()));
            Assert.That(create.MD5, Is.Not.Null);

            Directory.Delete(folder, true);
        }

    }
}
