using Audit.Core;

namespace Audit.Polly.Configuration
{
    /// <summary>
    /// Provides a fluent API to configure the Polly data provider.
    /// </summary>
    public interface IPollyProviderConfigurator
    {
        /// <summary>
        /// Sets the primary data provider to use
        /// </summary>
        /// <param name="primaryDataProvider">The primary data provider</param>
        IPollyResilienceConfigurator DataProvider(AuditDataProvider primaryDataProvider);
    }
}