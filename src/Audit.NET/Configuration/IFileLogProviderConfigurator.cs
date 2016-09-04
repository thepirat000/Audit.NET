namespace Audit.Core.Configuration
{
    /// <summary>
    /// Provides a configuration for the FileLogDataProvider
    /// </summary>
    public interface IFileLogProviderConfigurator
    {
        /// <summary>
        /// Specifies the directory where to store the audit log files.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        IFileLogProviderConfigurator Directory(string directoryPath);
        /// <summary>
        /// Specifies the filename prefix to use in the audit log files.
        /// </summary>
        /// <param name="filenamePrefix">The filename prefix.</param>
        IFileLogProviderConfigurator FilenamePrefix(string filenamePrefix);
    }
}