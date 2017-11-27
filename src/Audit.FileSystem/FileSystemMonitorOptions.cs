using Audit.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Audit.FileSystem
{
    /// <summary>
    /// Options for FileSystemMonitor creation
    /// </summary>
    public class FileSystemMonitorOptions
    {
        /// <summary>
        /// Creates an instance of FileSystemMonitorOptions
        /// </summary>
        public FileSystemMonitorOptions()
        {
        }
        /// <summary>
        /// Creates an instance of FileSystemMonitorOptions
        /// </summary>
        /// <param name="path">The path of the directory to monitor.</param>
        public FileSystemMonitorOptions(string path)
        {
            Path = path;
        }
        /// <summary>
        /// To indicate the event types included on the audit. NULL means all the event types will be logged.
        /// </summary>
        public FileSystemEventType[] IncludedEventTypes { get; set; }
        /// <summary>
        /// Gets or sets a value indicating the event type name.
        /// Can contain the following placeholders:
        /// - {name}: replaced with the file/directory name.
        /// - {path}: replaced with the full file/directory path.
        /// - {type}: replaced with the event type.
        /// </summary>
        public string EventTypeName { get; set; }
        /// <summary>
        /// The path of the directory to monitor.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// The filter string used to determine what files are monitored
        /// </summary>
        public string Filter { get; set; }
        /// <summary>
        /// A function that given a file event, returns true if the entry should be logged and false otherwise. Default includes all the files.
        /// </summary>
        public Func<FileSystemEventArgs, bool> CustomFilterPredicate { get; set; }
        /// <summary>
        /// A function that given a file info, returns the content type to determine whether the file content should be included and if it's textual or binary data. Default is not including the content.
        /// </summary>
        public Func<FileInfo, ContentType> IncludeContentPredicate { get; set; }
        /// <summary>
        /// The notify filters. Default is DirectoryName | FileName | LastAccess | LastWrite.
        /// </summary>
        public NotifyFilters NotifyFilters { get; set; } = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite;
        /// <summary>
        /// To indicate if the subdirectories should be monitored
        /// </summary>
        public bool IncludeSubdirectories { get; set; }
        /// <summary>
        /// To indicate if the MD5 computation should be ignored
        /// </summary>
        public bool IgnoreMD5 { get; set; }
        /// <summary>
        /// Gets or sets the size (in bytes) of the FileSystemWatcher internal buffer.
        /// </summary>
        public int InternalBufferSize { get; set; } = 8192;
        /// <summary>
        /// To indicate the Audit Data Provider to use. (Default is NULL to use the configured default data provider). 
        /// </summary>
        public AuditDataProvider AuditDataProvider { get; set; }
        /// <summary>
        /// Gets or sets the event creation policy to use. By default it will use the global Configuration.CreationPolicy.
        /// </summary>
        public EventCreationPolicy? CreationPolicy { get; set; }
    }
}
