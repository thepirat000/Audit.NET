using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace Audit.Core.Providers
{
    /// <summary>
    /// Write the event outputs as files.
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - DirectoryPathBuilder: A function that returns the file system path to store the output for an event. If this setting is provided, DirectoryPath setting will be ignored.
    /// - DirectoryPath: Directory path to store the output files (default is current directory). This setting is ignored when using DirectoryPathBuilder.
    /// - FilenameBuilder: A function that returns the file name to store the output for an event.
    /// - FilenamePrefix: Filename prefix that will be appended with a timestamp.
    /// </remarks>
    public class FileDataProvider : AuditDataProvider
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        private string _filenamePrefix = string.Empty;
        private string _directoryPath = string.Empty;
        private Func<AuditEvent, string> _filenameBuilder;
        private Func<AuditEvent, string> _directoryPathBuilder;

        /// <summary>
        /// Gets or sets the filename builder.
        /// A function that returns the file system path to store the output for an event.
        /// </summary>
        public Func<AuditEvent, string> FilenameBuilder
        {
            get { return _filenameBuilder; }
            set { _filenameBuilder = value; }
        }

        /// <summary>
        /// Gets or sets the Filename prefix that will be used.
        /// </summary>
        public string FilenamePrefix
        {
            get { return _filenamePrefix; }
            set { _filenamePrefix = value; }
        }

        /// <summary>
        /// Gets or sets the directory path builder.
        /// A function that returns the file system path to store the output for an event. If this setting is provided, DirectoryPath setting will be ignored.
        /// </summary>
        public Func<AuditEvent, string> DirectoryPathBuilder
        {
            get { return _directoryPathBuilder; }
            set { _directoryPathBuilder = value; }
        }

        /// <summary>
        /// Gets or sets the Directory path to store the output files (default is current directory). This setting is ignored when using DirectoryPathBuilder.
        /// </summary>
        public string DirectoryPath
        {
            get { return _directoryPath; }
            set { _directoryPath = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var fullPath = GetFilePath(auditEvent);
            var json = JsonConvert.SerializeObject(auditEvent, JsonSettings);
            File.WriteAllText(fullPath, json);
            return fullPath;
        }

        public override void ReplaceEvent(object path, AuditEvent auditEvent)
        {
            var fullPath = path.ToString();
            var json = JsonConvert.SerializeObject(auditEvent, JsonSettings);
            File.WriteAllText(fullPath, json);
        }

        private string GetFilePath(AuditEvent auditEvent)
        {
            string fileName = _filenamePrefix;
            if (_filenameBuilder != null)
            {
                fileName += _filenameBuilder.Invoke(auditEvent);
            }
            else
            {
                fileName += $"{DateTime.Now:yyyyMMddHHmmss}_{HiResDateTime.UtcNowTicks}.json";
            }
            string directory;
            if (_directoryPathBuilder != null)
            {
                directory = _directoryPathBuilder.Invoke(auditEvent);
            }
            else
            {
                directory = _directoryPath ?? string.Empty;
            }
            if (directory.Length > 0)
            {
                Directory.CreateDirectory(directory);
            }
            return Path.Combine(directory, fileName);
        }

        // Original from: http://stackoverflow.com/a/14369695/122195
        public class HiResDateTime
        {
            private static long lastTimeStamp = DateTime.UtcNow.Ticks;
            public static long UtcNowTicks
            {
                get
                {
                    long original, newValue;
                    do
                    {
                        original = lastTimeStamp;
                        long now = DateTime.UtcNow.Ticks;
                        newValue = Math.Max(now, original + 1);
                    } while (Interlocked.CompareExchange
                                 (ref lastTimeStamp, newValue, original) != original);

                    return newValue;
                }
            }
        }
    }
}
