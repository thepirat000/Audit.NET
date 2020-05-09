using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings
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

        public FileDataProvider()
        {
        }

        public FileDataProvider(Action<ConfigurationApi.IFileLogProviderConfigurator> config)
        {
            var fileConfig = new ConfigurationApi.FileLogProviderConfigurator();
            if (config != null)
            {
                config.Invoke(fileConfig);
                _directoryPath = fileConfig._directoryPath;
                _directoryPathBuilder = fileConfig._directoryPathBuilder;
                _filenameBuilder = fileConfig._filenameBuilder;
                _filenamePrefix = fileConfig._filenamePrefix;
                JsonSettings = fileConfig._jsonSettings;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var fullPath = GetFilePath(auditEvent);
            var json = JsonConvert.SerializeObject(auditEvent, JsonSettings);
            File.WriteAllText(fullPath, json);
            return fullPath;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var fullPath = GetFilePath(auditEvent);
            await SaveFileAsync(fullPath, auditEvent);
            return fullPath;
        }

        public override void ReplaceEvent(object path, AuditEvent auditEvent)
        {
            var fullPath = path.ToString();
            var json = JsonConvert.SerializeObject(auditEvent, JsonSettings);
            File.WriteAllText(fullPath, json);
        }

        public override T GetEvent<T>(object path)
        {
            var fullPath = path.ToString();
            var json = File.ReadAllText(fullPath);
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }

        public override async Task ReplaceEventAsync(object path, AuditEvent auditEvent)
        {
            var fullPath = path.ToString();
            await SaveFileAsync(fullPath, auditEvent);
        }

        public override async Task<T> GetEventAsync<T>(object path) 
        {
            var fullPath = path.ToString();
            return await GetFromFileAsync<T>(fullPath);
        }

        private async Task SaveFileAsync(string fullPath, AuditEvent auditEvent)
        {
            using (FileStream file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true))
            {
                var jObject = JObject.FromObject(auditEvent, JsonSerializer.Create(JsonSettings));
                using (var sw = new StreamWriter(file))
                {
                    using (var jw = new JsonTextWriter(sw))
                    {
                        await jObject.WriteToAsync(jw);
                    }
                }
            }
        }

        private async Task<T> GetFromFileAsync<T>(string fullPath) where T : AuditEvent
        {
            using (FileStream file = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                using (var sr = new StreamReader(file))
                {
                    using (var jr = new JsonTextReader(sr))
                    {
                        var jObject = await JObject.LoadAsync(jr);
                        return jObject.ToObject<T>(JsonSerializer.Create(JsonSettings));
                    }
                }
            }
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
                fileName += $"{Configuration.SystemClock.UtcNow:yyyyMMddHHmmss}_{HiResDateTime.UtcNowTicks}.json";
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
            private static long lastTimeStamp = Configuration.SystemClock.UtcNow.Ticks;
            public static long UtcNowTicks
            {
                get
                {
                    long original, newValue;
                    do
                    {
                        original = lastTimeStamp;
                        long now = Configuration.SystemClock.UtcNow.Ticks;
                        newValue = Math.Max(now, original + 1);
                    } while (Interlocked.CompareExchange
                                 (ref lastTimeStamp, newValue, original) != original);

                    return newValue;
                }
            }
        }
    }
}
