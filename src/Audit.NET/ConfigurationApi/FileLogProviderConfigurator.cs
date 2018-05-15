using Newtonsoft.Json;
using System;

namespace Audit.Core.ConfigurationApi
{
    public class FileLogProviderConfigurator : IFileLogProviderConfigurator
    {
        internal JsonSerializerSettings _jsonSettings = null;
        internal string _directoryPath = "";
        internal string _filenamePrefix = "";
        internal Func<AuditEvent, string> _filenameBuilder;
        internal Func<AuditEvent, string> _directoryPathBuilder;

        public IFileLogProviderConfigurator JsonSettings(JsonSerializerSettings jsonSettings)
        {
            _jsonSettings = jsonSettings;
            return this;
        }

        public IFileLogProviderConfigurator Directory(string directoryPath)
        {
            _directoryPath = directoryPath;
            return this;
        }

        public IFileLogProviderConfigurator DirectoryBuilder(Func<AuditEvent, string> directoryPathBuilder)
        {
            _directoryPathBuilder = directoryPathBuilder;
            return this;
        }

        public IFileLogProviderConfigurator FilenamePrefix(string filenamePrefix)
        {
            _filenamePrefix = filenamePrefix;
            return this;
        }

        public IFileLogProviderConfigurator FilenameBuilder(Func<AuditEvent, string> filenameBuilder)
        {
            _filenameBuilder = filenameBuilder;
            return this;
        }
    }
}
