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
        private FileSystemMonitorOptions _options;
        /// <summary>
        /// The FileSystemMonitor options.
        /// </summary>
        public FileSystemMonitorOptions Options { get => _options; set => _options = value; }
        private FileSystemWatcher _watcher;

        public FileSystemMonitor(string path)
        {
            _options = new FileSystemMonitorOptions(path);
        }
        public FileSystemMonitor(FileSystemMonitorOptions options)
        {
            _options = options;
        }
        public FileSystemMonitor()
        {
            _options = new FileSystemMonitorOptions(Directory.GetCurrentDirectory());
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
            _watcher.NotifyFilter = _options.NotifyFilters;
            _watcher.Filter = _options.Filter;
            _watcher.IncludeSubdirectories = _options.IncludeSubdirectories;
            _watcher.Path = _options.Path;
            _watcher.InternalBufferSize = _options.InternalBufferSize;
            if (_options.IncludedEventTypes == null || _options.IncludedEventTypes.Contains(FileSystemEventType.Change))
            {
                _watcher.Changed += _watcher_Changed;
            }
            if (_options.IncludedEventTypes == null || _options.IncludedEventTypes.Contains(FileSystemEventType.Rename))
            {
                _watcher.Renamed += _watcher_Renamed;
            }
            if (_options.IncludedEventTypes == null || _options.IncludedEventTypes.Contains(FileSystemEventType.Delete))
            {
                _watcher.Deleted += _watcher_Deleted;
            }
            if (_options.IncludedEventTypes == null || _options.IncludedEventTypes.Contains(FileSystemEventType.Create))
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
            return _options.CustomFilterPredicate == null || _options.CustomFilterPredicate.Invoke(e);
        }

        private void ProcessEvent(FileSystemEventArgs e, FileSystemEventType type)
        {
            var fsEvent = new FileSystemEvent()
            {
                Name = System.IO.Path.GetFileName(e.FullPath),
                Extension = System.IO.Path.GetExtension(e.FullPath),
                FullPath = e.FullPath,
                Event = type,
                OldName = (e is RenamedEventArgs) ? System.IO.Path.GetFileName((e as RenamedEventArgs).OldFullPath) : null
            };
            var fsAuditEvent = new AuditEventFileSystem()
            {
                FileSystemEvent = fsEvent
            };
            var eventType = (_options.EventTypeName ?? "[{type}] {name}").Replace("{name}", fsEvent.Name).Replace("{path}", fsEvent.FullPath).Replace("{type}", e.ChangeType.ToString());
            var factory = _options.AuditScopeFactory ?? Configuration.AuditScopeFactory;
            using (var auditScope = factory.Create(new AuditScopeOptions() { EventType = eventType, AuditEvent = fsAuditEvent, DataProvider = _options.AuditDataProvider, CreationPolicy = _options.CreationPolicy }))
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
                    (auditScope.Event as AuditEventFileSystem).FileSystemEvent = fsEvent;
                }
            }
        }

        private void FillEvent(FileSystemEvent fsEvent, FileSystemEventArgs e)
        {
            FileAttributes attr = FileAttributes.Archive;
            try
            {
                attr = File.GetAttributes(e.FullPath);
            }
            catch (IOException ex)
            {
                fsEvent.Errors.Add($"IOException when getting file attributes: {ex.Message}");
                return;
            }
            bool isDir = (attr & FileAttributes.Directory) == FileAttributes.Directory;
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
                    if (!_options.IgnoreMD5)
                    {
                        fsEvent.MD5 = ComputeMD5(e.FullPath);
                    }
                    if (_options.IncludeContentPredicate != null)
                    {
                        var contentType = _options.IncludeContentPredicate.Invoke(fi);
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

        private string ComputeMD5(string filePath)
        {
            byte[] hash;
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        hash = md5.ComputeHash(stream);
                    }
                }
                catch (IOException ex)
                {
                    return $"{{Error}} {ex.Message}";
                }
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(); 
            }
        }
    }
}
