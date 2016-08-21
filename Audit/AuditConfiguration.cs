using Audit.Core.Providers;

namespace Audit.Core
{
    /// <summary>
    /// Global configuration for Audit.NET 
    /// </summary>
    public static class AuditConfiguration
    {
        /// <summary>
        /// Gets the current data provider.
        /// </summary>
        public static AuditDataProvider DataProvider { get; private set; }
        static AuditConfiguration()
        {
            DataProvider = new FileDataProvider();
        }
        /// <summary>
        /// Sets the data provider to use.
        /// </summary>
        /// <param name="dataProvider">The data provider.</param>
        public static void SetDataProvider(AuditDataProvider dataProvider)
        {
            DataProvider = dataProvider;
        }
    }
}
