using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Audit.FileSystem.UnitTest
{
    public class FileSystemTests
    {
        private static Random random = new Random();

        [Test]
        public void Test_FileSystem_1()
        {
            var folder = Path.Combine(Path.GetTempPath(), random.Next(1000, 9999).ToString());
            System.IO.Directory.CreateDirectory(folder);
            
            var filename1 = $"test_{random.Next(1000, 9999)}.txt";
            var filename2 = $"test_{random.Next(1000, 9999)}.txt";
            var t1path = Path.Combine(folder, filename1);
            var t2path = Path.Combine(folder, filename2);
            File.Delete(t1path);
            File.Delete(t2path);
            var locker = new object();
            var evs = new List<FileSystemEvent>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _.OnInsert(ev => {
                    lock (locker) {
                        evs.Add((ev as FileSystem.AuditEventFileSystem).FileSystemEvent);
                    }
                }));

            var fsMon = new FileSystemMonitor(folder);
            fsMon.Options.IncludeSubdirectories = true;
            fsMon.Options.IncludeContentPredicate = _ => ContentType.Text;
            fsMon.Start();
            Thread.Sleep(500);

            File.WriteAllText(t1path, "this is a test");
            Thread.Sleep(500);
            File.Move(t1path, t2path);
            Thread.Sleep(500);

            File.Delete(t2path);
            Thread.Sleep(1500);

            Assert.IsTrue(evs.Count >= 3, "Events: {0}", evs.Count);
            var create = evs.Single(x => x.Event == FileSystemEventType.Create);
            Assert.AreEqual(FileSystemEventType.Create, create.Event);
            Assert.AreEqual(filename1, create.Name);
            Assert.AreEqual(14, create.Length);
            Assert.AreEqual(ContentType.Text, create.FileContent.Type);
            Assert.AreEqual("this is a test", (create.FileContent as FileTextualContent).Value);
            Assert.IsNotNull(create.MD5);

            var rename = evs.Single(x => x.Event == FileSystemEventType.Rename);
            Assert.AreEqual(filename1, rename.OldName);
            Assert.AreEqual(filename2, rename.Name);
            Assert.IsNotNull(rename.MD5);

            var delete = evs.Single(x => x.Event == FileSystemEventType.Delete);
            Assert.AreEqual(filename2, delete.Name);

            System.IO.Directory.Delete(folder, true);
        }

    }
}
