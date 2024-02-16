using System;

namespace Audit.Core.ConfigurationApi
{
    public class FileLogProviderConfigurator : IFileLogProviderConfigurator
    {
        internal Setting<string> _directoryPath = "";
        internal Setting<string> _filenamePrefix = "";
        internal Func<AuditEvent, string> _filenameBuilder;

        public IFileLogProviderConfigurator Directory(string directoryPath)
        {
            _directoryPath = directoryPath;
            return this;
        }

        public IFileLogProviderConfigurator DirectoryBuilder(Func<AuditEvent, string> directoryPathBuilder)
        {
            _directoryPath = directoryPathBuilder;
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
