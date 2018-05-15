using Newtonsoft.Json;
using System;

namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the FileLogDataProvider
    /// </summary>
    public interface IFileLogProviderConfigurator
    {
        /// <summary>
        /// Specifies the JSON settings to use to serialize the audit events.
        /// </summary>
        /// <param name="jsonSettings">JSON settings to use.</param>
        IFileLogProviderConfigurator JsonSettings(JsonSerializerSettings jsonSettings);

        /// <summary>
        /// Specifies the directory where to store the audit log files. This setting is ignored when using DirectoryBuilder.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        IFileLogProviderConfigurator Directory(string directoryPath);

        /// <summary>
        /// Specifies the directory builder to get the path where to store the audit log files. If this setting is provided, Directory setting will be ignored.
        /// </summary>
        /// <param name="directoryPathBuilder">The directory path builder. A function that returns the file system path to store the output for an event.</param>
        /// <returns>IFileLogProviderConfigurator.</returns>
        IFileLogProviderConfigurator DirectoryBuilder(Func<AuditEvent, string> directoryPathBuilder);

        /// <summary>
        /// Specifies the filename prefix to use in the audit log files.
        /// </summary>
        /// <param name="filenamePrefix">The filename prefix.</param>
        IFileLogProviderConfigurator FilenamePrefix(string filenamePrefix);

        /// <summary>
        /// Specifies the filename builder to get the filename to store the audit log for an event.
        /// </summary>
        /// <param name="filenameBuilder">The filename builder. A function that returns the file name to store the output for an event.</param>
        /// <returns>IFileLogProviderConfigurator.</returns>
        IFileLogProviderConfigurator FilenameBuilder(Func<AuditEvent, string> filenameBuilder);
    }
}