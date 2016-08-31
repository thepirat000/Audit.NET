using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace Audit.Core.Providers
{
    /// <summary>
    /// Write the events as files.
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - DirectoryPath: Directory path to store the output files (default is current directory)
    /// - FilenamePrefix: Filename prefix (default: "")
    /// </remarks>
    public class FileDataProvider : AuditDataProvider
    {
        private string _filenamePrefix = string.Empty;
        private string _directoryPath = string.Empty;

        public string FilenamePrefix
        {
            get { return _filenamePrefix; }
            set { _filenamePrefix = value; }
        }

        public string DirectoryPath
        {
            get { return _directoryPath; }
            set { _directoryPath = value; }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var fileName = _filenamePrefix + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + HiResDateTime.UtcNowTicks + ".json";
            if (_directoryPath.Length > 0)
            {
                Directory.CreateDirectory(_directoryPath);
            }
            var fullPath = Path.Combine(_directoryPath, fileName);
            var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            
            File.WriteAllText(fullPath, json);
            return fullPath;
        }

        public override void ReplaceEvent(object path, AuditEvent auditEvent)
        {
            var fullPath = path.ToString();
            var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            File.WriteAllText(fullPath, json);
        }

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
