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
    [Timeout(60000)]
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
                .UseDynamicProvider(d => d.OnInsert(ev =>
                {
                    lock (locker)
                    {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Options.IncludeContentPredicate = _ => ContentType.Text;
            fsMon.Start();

            Thread.Sleep(500);

            File.WriteAllText(t1Path, "this is a test");

            WaitForChange(fsMon);

            File.Delete(t1Path);

            WaitForChange(fsMon);

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

        [Test]
        public void FileSystemMonitor_OnPredicateException_ReturnsError()
        {
            var folder = Path.Combine(Path.GetTempPath(), Random.Next(10000, 99999).ToString());
            Directory.CreateDirectory(folder);

            var filename1 = $"test_{Random.Next(1000, 9999)}.txt";
            var t1Path = Path.Combine(folder, filename1);
            File.Delete(t1Path);

            Audit.Core.Configuration.Setup()
                .UseInMemoryProvider(out var dataProvider);

            var fsMon = new FileSystemMonitor()
            {
                Options = new FileSystemMonitorOptions()
                {
                    Path = folder,
                    CustomFilterPredicate = _ => true,
                    IncludeContentPredicate = _ => throw new Exception("test"),
                    IncludedEventTypes = [FileSystemEventType.Create, FileSystemEventType.Change, FileSystemEventType.Rename, FileSystemEventType.Delete]
                }
            };
            fsMon.Start();

            File.WriteAllBytes(t1Path, "MZ1234"u8.ToArray());

            WaitForChange(fsMon);

            var events = dataProvider.GetAllEventsOfType<AuditEventFileSystem>();

            Assert.That(events, Has.Count.GreaterThanOrEqualTo(0));
            Assert.That(events[0].FileSystemEvent.Errors, Has.Count.EqualTo(1));
            Assert.That(events[0].FileSystemEvent.Errors[0], Does.Contain("test"));

            Directory.Delete(folder, true);
        }

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
                .UseDynamicProvider(d => d.OnInsert(ev =>
                {
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

            WaitForChange(fsMon);

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

        [Test]
        public void Test_FileSystem_Rename()
        {
            var folder = Path.Combine(Path.GetTempPath(), Random.Next(1000, 9999).ToString());
            Directory.CreateDirectory(folder);

            var filename1 = $"test_{Random.Next(1000, 9999)}.txt";
            var t1Path = Path.Combine(folder, filename1);
            
            File.Delete(t1Path);
            File.WriteAllBytes(t1Path, "MZ1234"u8.ToArray());

            var locker = new object();

            var evs = new List<FileSystemEvent>();

            Core.Configuration.Setup()
                .UseDynamicProvider(d => d.OnInsert(ev =>
                {
                    lock (locker)
                    {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Start();

            Thread.Sleep(500);
            
            File.Move(t1Path, Path.Combine(folder, "renamed.txt"));

            WaitForChange(fsMon);

            Assert.That(evs.Count >= 1, Is.True, $"Events: {evs.Count}");
            var rename = evs.LastOrDefault(x => x.Event == FileSystemEventType.Rename);
            Assert.That(rename, Is.Not.Null);

            Directory.Delete(folder, true);
        }

        [Test]
        public void Test_FileSystem_Directory()
        {
            var folder = Path.Combine(Path.GetTempPath(), Random.Next(1000, 9999).ToString());
            Directory.CreateDirectory(folder);

            var newDirectory = $"test_{Random.Next(1000, 9999)}";
            var t1Path = Path.Combine(folder, newDirectory);

            var locker = new object();

            var evs = new List<FileSystemEvent>();

            Core.Configuration.Setup()
                .UseDynamicProvider(d => d.OnInsert(ev =>
                {
                    lock (locker)
                    {
                        evs.Add(((AuditEventFileSystem)ev).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Start();

            Thread.Sleep(500);

            Directory.CreateDirectory(t1Path);

            WaitForChange(fsMon);

            Assert.That(evs.Count >= 1, Is.True, $"Events: {evs.Count}");
            var create = evs.LastOrDefault(x => x.Event == FileSystemEventType.Create);
            Assert.That(create, Is.Not.Null);
            Assert.That(create.Object, Is.EqualTo(FileSystemObjectType.Directory));

            Directory.Delete(folder, true);
        }

        [Test]
        public void FileSystemMonitor_Constructor_Overloads()
        {
            var fsMon = new FileSystemMonitor();
            Assert.That(fsMon.Options.Path, Is.EqualTo(Directory.GetCurrentDirectory()));

            fsMon = new FileSystemMonitor("c:\\test");
            Assert.That(fsMon.Options.Path, Is.EqualTo("c:\\test"));
            
            var opts = new FileSystemMonitorOptions("d:\\test2");
            fsMon = new FileSystemMonitor(opts);
            Assert.That(fsMon.Options.Path, Is.EqualTo("d:\\test2"));
        }

        [Test]
        public void FileSystemMonitor_Stop_DisableEvents()
        {
            var fsMon = new FileSystemMonitor()
            {
                Options = new FileSystemMonitorOptions()
                {
                    Path = Directory.GetCurrentDirectory(),
                    CustomFilterPredicate = _ => false
                }
            };
            fsMon.Start();
            var w = fsMon.GetWatcher();
            Assert.That(w.EnableRaisingEvents, Is.True);
            fsMon.Stop();
            Assert.That(w.EnableRaisingEvents, Is.False);
        }

        [Test]
        public void FileSystemMonitor_DoubleStart_Disposes()
        {
            var fsMon = new FileSystemMonitor()
            {
                Options = new FileSystemMonitorOptions()
                {
                    Path = Directory.GetCurrentDirectory(),
                    CustomFilterPredicate = _ => false,
                    IncludedEventTypes = [FileSystemEventType.Create, FileSystemEventType.Change, FileSystemEventType.Rename, FileSystemEventType.Delete]
                }
            };
            fsMon.Start();
            var w1 = fsMon.GetWatcher();
            fsMon.Start();
            var w2 = fsMon.GetWatcher();
            Assert.That(w1, Is.Not.EqualTo(w2));
        }
        
        private static void WaitForChange(FileSystemMonitor fsMon, int milliseconds = 5000)
        {
            while (true)
            {
                var res = fsMon.GetWatcher().WaitForChanged(WatcherChangeTypes.All, milliseconds);
                if (res.TimedOut)
                {
                    break;
                }
            }
        }
    }
}
