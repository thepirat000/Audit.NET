using Audit.FileSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Audit.UnitTest
{
    public class FileSystemTests
    {
        private const string folder = @"D:\temp";

        [SetUp]
        public void Setup()
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        [Test]
        public void Test_FileSystem_1()
        {
            var t1path = Path.Combine(folder, "test.txt");
            var t2path = Path.Combine(folder, "test2.txt");
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
            Thread.Sleep(500);

            var create = evs.Single(x => x.Event == FileSystemEventType.Create);
            Assert.IsTrue(evs.Count >= 3);
            Assert.AreEqual(FileSystemEventType.Create, create.Event);
            Assert.AreEqual("test.txt", create.Name);
            Assert.AreEqual(14, create.Length);
            Assert.AreEqual(ContentType.Text, create.FileContent.Type);
            Assert.AreEqual("this is a test", (create.FileContent as FileTextualContent).Value);
            Assert.IsNotNull(create.MD5);

            var rename = evs.Single(x => x.Event == FileSystemEventType.Rename);
            Assert.AreEqual("test.txt", rename.OldName);
            Assert.AreEqual("test2.txt", rename.Name);
            Assert.IsNotNull(rename.MD5);

            var delete = evs.Single(x => x.Event == FileSystemEventType.Delete);
            Assert.AreEqual("test2.txt", delete.Name);
        }

    }
}
