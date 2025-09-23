using Audit.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Audit.FileSystem
{
    /// <summary>
    /// Monitor a folder in the file system generating en audit event for each change
    /// </summary>
    public class FileSystemMonitor
    {
        /// <summary>
        /// The FileSystemMonitor options.
        /// </summary>
        public FileSystemMonitorOptions Options { get; set; }

        private FileSystemWatcher _watcher;

        internal FileSystemWatcher GetWatcher()
        {
            return _watcher; 
        }

        public FileSystemMonitor(string path)
        {
            Options = new FileSystemMonitorOptions(path);
        }

        public FileSystemMonitor(FileSystemMonitorOptions options)
        {
            Options = options;
        }

        public FileSystemMonitor()
        {
            Options = new FileSystemMonitorOptions(Directory.GetCurrentDirectory());
        }

        /// <summary>
        /// Start monitoring the file system
        /// </summary>
        public void Start()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
            _watcher = new FileSystemWatcher();
            _watcher.NotifyFilter = Options.NotifyFilters;
            _watcher.Filter = Options.Filter;
            _watcher.IncludeSubdirectories = Options.IncludeSubdirectories;
            _watcher.Path = Options.Path;
            _watcher.InternalBufferSize = Options.InternalBufferSize;
            if (Options.IncludedEventTypes == null || Options.IncludedEventTypes.Contains(FileSystemEventType.Change))
            {
                _watcher.Changed += _watcher_Changed;
            }
            if (Options.IncludedEventTypes == null || Options.IncludedEventTypes.Contains(FileSystemEventType.Rename))
            {
                _watcher.Renamed += _watcher_Renamed;
            }
            if (Options.IncludedEventTypes == null || Options.IncludedEventTypes.Contains(FileSystemEventType.Delete))
            {
                _watcher.Deleted += _watcher_Deleted;
            }
            if (Options.IncludedEventTypes == null || Options.IncludedEventTypes.Contains(FileSystemEventType.Create))
            {
                _watcher.Created += _watcher_Created;
            }
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring the file system
        /// </summary>
        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (IncludeObject(e))
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessEvent(e, FileSystemEventType.Create);
                });
            }
        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            if (IncludeObject(e))
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessEvent(e, FileSystemEventType.Delete);
                });
            }
        }

        private void _watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (IncludeObject(e))
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessEvent(e, FileSystemEventType.Rename);
                });
            }
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (IncludeObject(e))
            {
                Task.Factory.StartNew(() =>
                {
                    ProcessEvent(e, FileSystemEventType.Change);
                });
            }
        }

        private bool IncludeObject(FileSystemEventArgs e)
        {
            return Options.CustomFilterPredicate == null || Options.CustomFilterPredicate.Invoke(e);
        }

        private void ProcessEvent(FileSystemEventArgs e, FileSystemEventType type)
        {
            var fsEvent = new FileSystemEvent()
            {
                Name = Path.GetFileName(e.FullPath),
                Extension = Path.GetExtension(e.FullPath),
                FullPath = e.FullPath,
                Event = type,
                OldName = (e is RenamedEventArgs args) ? Path.GetFileName(args.OldFullPath) : null
            };
            var fsAuditEvent = new AuditEventFileSystem()
            {
                FileSystemEvent = fsEvent
            };
            var eventType = (Options.EventTypeName ?? "[{type}] {name}").Replace("{name}", fsEvent.Name).Replace("{path}", fsEvent.FullPath).Replace("{type}", e.ChangeType.ToString());
            var factory = Options.AuditScopeFactory ?? Configuration.AuditScopeFactory;
            using (var auditScope = factory.Create(new AuditScopeOptions() { EventType = eventType, AuditEvent = fsAuditEvent, DataProvider = Options.AuditDataProvider, CreationPolicy = Options.CreationPolicy }))
            {
                if (type != FileSystemEventType.Delete)
                {
                    fsEvent.Errors = new List<string>();
                    try
                    {
                        FillEvent(fsEvent, e);
                    }
                    catch (Exception ex)
                    {
                        fsEvent.Errors.Add($"{ex.GetType().Name}: {ex.Message})");
                    }
                    if (fsEvent.Errors.Count == 0)
                    {
                        fsEvent.Errors = null;
                    }
                    auditScope.EventAs<AuditEventFileSystem>().FileSystemEvent = fsEvent;
                }
            }
        }

        private void FillEvent(FileSystemEvent fsEvent, FileSystemEventArgs e)
        {
            FileAttributes attr;
            try
            {
                attr = File.GetAttributes(e.FullPath);
            }
            catch (IOException ex)
            {
                fsEvent.Errors.Add($"IOException when getting file attributes: {ex.Message}");
                return;
            }
            var isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;
            FileSystemInfo fsInfo;
            if (isDir)
            {
                var di = new DirectoryInfo(e.FullPath);
                fsInfo = di;
                fsEvent.Object = FileSystemObjectType.Directory;
            }
            else
            {
                var fi = new FileInfo(e.FullPath);
                fsInfo = fi;
                fsEvent.Length = fi.Length;
                fsEvent.ReadOnly = fi.IsReadOnly;
                fsEvent.Object = FileSystemObjectType.File;
                if (fi.Exists)
                {
                    if (!Options.IgnoreMD5)
                    {
                        fsEvent.MD5 = ComputeMd5(e.FullPath);
                    }
                    if (Options.IncludeContentPredicate != null)
                    {
                        var contentType = Options.IncludeContentPredicate.Invoke(fi);
                        if (contentType != ContentType.None)
                        {
                            try
                            {
                                if (contentType == ContentType.Binary)
                                {
                                    fsEvent.FileContent = new FileBinaryContent() { Value = File.ReadAllBytes(e.FullPath) };
                                }
                                else if (contentType == ContentType.Text)
                                {
                                    fsEvent.FileContent = new FileTextualContent() { Value = File.ReadAllText(e.FullPath) };
                                }
                            }
                            catch (IOException ex)
                            {
                                fsEvent.Errors.Add($"IOException when getting file content: {ex.Message}");
                            }
                        }
                    }
                }
            }
            fsEvent.Attributes = attr.ToString();
            fsEvent.CreationTime = fsInfo.CreationTime;
            fsEvent.LastAccessTime = fsInfo.LastAccessTime;
            fsEvent.LastWriteTime = fsInfo.LastWriteTime;
        }

        private static string ComputeMd5(string filePath)
        {
            byte[] hash;
            using var md5 = MD5.Create();
            try
            {
                using var stream = File.OpenRead(filePath);

                hash = md5.ComputeHash(stream);
            }
            catch (IOException ex)
            {
                return $"{{Error}} {ex.Message}";
            }
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
