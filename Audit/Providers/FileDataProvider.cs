using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

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

        public override void WriteEvent(AuditEvent auditEvent)
        {
            var fileName = _filenamePrefix + DateTime.Now.ToString("yyyyMMddmmssfff") + ".json";
            if (_directoryPath.Length > 0)
            {
                Directory.CreateDirectory(_directoryPath);
            }
            var fullPath = Path.Combine(_directoryPath, fileName);
            var json = JsonConvert.SerializeObject(auditEvent, new JsonSerializerSettings() { Formatting = Formatting.Indented });
            File.WriteAllText(fullPath, json);
        }

    }
}
