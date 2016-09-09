namespace Audit.Core.ConfigurationApi
{
    public class FileLogProviderConfigurator : IFileLogProviderConfigurator
    {
        internal string _directoryPath = "";
        internal string _filenamePrefix = "";
        public IFileLogProviderConfigurator Directory(string directoryPath)
        {
            _directoryPath = directoryPath;
            return this;
        }

        public IFileLogProviderConfigurator FilenamePrefix(string filenamePrefix)
        {
            _filenamePrefix = filenamePrefix;
            return this;
        }
    }
}
