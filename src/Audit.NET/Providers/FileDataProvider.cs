using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        /// <summary>
        /// Gets or sets the filename builder.
        /// A function that returns the file system path to store the output for an event.
        /// </summary>
        public Func<AuditEvent, string> FilenameBuilder { get; set; }

        /// <summary>
        /// Gets or sets the Filename prefix that will be used.
        /// </summary>
        public Setting<string> FilenamePrefix { get; set; }

        /// <summary>
        /// Gets or sets the Directory path to store the output files (default is current directory).
        /// </summary>
        public Setting<string> DirectoryPath { get; set; }

        public FileDataProvider()
        {
        }

        public FileDataProvider(Action<ConfigurationApi.IFileLogProviderConfigurator> config)
        {
            var fileConfig = new ConfigurationApi.FileLogProviderConfigurator();
            if (config != null)
            {
                config.Invoke(fileConfig);
                DirectoryPath = fileConfig._directoryPath;
                FilenameBuilder = fileConfig._filenameBuilder;
                FilenamePrefix = fileConfig._filenamePrefix;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var fullPath = GetFilePath(auditEvent);
            var json = Configuration.JsonAdapter.Serialize(auditEvent);
            File.WriteAllText(fullPath, json);
            return fullPath;
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var fullPath = GetFilePath(auditEvent);
            await SaveFileAsync(fullPath, auditEvent, cancellationToken);
            return fullPath;
        }

        public override void ReplaceEvent(object path, AuditEvent auditEvent)
        {
            var fullPath = path.ToString();
            var json = Configuration.JsonAdapter.Serialize(auditEvent);
            File.WriteAllText(fullPath, json);
        }

        public override T GetEvent<T>(object path)
        {
            var fullPath = path.ToString();
            var json = File.ReadAllText(fullPath);
            return Configuration.JsonAdapter.Deserialize<T>(json);
        }

        public override async Task ReplaceEventAsync(object path, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var fullPath = path.ToString();
            await SaveFileAsync(fullPath, auditEvent, cancellationToken);
        }

        public override async Task<T> GetEventAsync<T>(object path, CancellationToken cancellationToken = default) 
        {
            var fullPath = path.ToString();
            return await GetFromFileAsync<T>(fullPath, cancellationToken);
        }

        private async Task SaveFileAsync(string fullPath, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await Configuration.JsonAdapter.SerializeAsync(stream, auditEvent, cancellationToken);
            }
        }

        private async Task<T> GetFromFileAsync<T>(string fullPath, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            using (var stream = File.OpenRead(fullPath))
            {
                return await Configuration.JsonAdapter.DeserializeAsync<T>(stream, cancellationToken);
            }
        }

        private string GetFilePath(AuditEvent auditEvent)
        {
            var fileName = FilenamePrefix.GetValue(auditEvent) ?? "";
            if (FilenameBuilder != null)
            {
                fileName += FilenameBuilder.Invoke(auditEvent);
            }
            else
            {
                fileName += $"{Configuration.SystemClock.GetCurrentDateTime():yyyyMMddHHmmss}_{HiResDateTime.UtcNowTicks}.json";
            }
            
            var directory = DirectoryPath.GetValue(auditEvent) ?? "";
            
            if (directory.Length > 0)
            {
                Directory.CreateDirectory(directory);
            }
            
            return Path.Combine(directory, fileName);
        }

        // Original from: http://stackoverflow.com/a/14369695/122195
        public class HiResDateTime
        {
            private static long lastTimeStamp = Configuration.SystemClock.GetCurrentDateTime().Ticks;
            public static long UtcNowTicks
            {
                get
                {
                    long original, newValue;
                    do
                    {
                        original = lastTimeStamp;
                        long now = Configuration.SystemClock.GetCurrentDateTime().Ticks;
                        newValue = Math.Max(now, original + 1);
                    } while (Interlocked.CompareExchange
                                 (ref lastTimeStamp, newValue, original) != original);

                    return newValue;
                }
            }
        }
    }
}
