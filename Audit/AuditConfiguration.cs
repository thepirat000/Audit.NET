using Audit.Core.Providers;

namespace Audit.Core
{
    /// <summary>
    /// Global configuration for Audit.NET 
    /// </summary>
    public static class AuditConfiguration
    {
        /// <summary>
        /// Gets or Sets the Default creation policy.
        /// </summary>
        public static EventCreationPolicy CreationPolicy { get; private set; } 
        /// <summary>
        /// Gets the Default data provider.
        /// </summary>
        public static AuditDataProvider DataProvider { get; private set; }
        static AuditConfiguration()
        {
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
        }
        /// <summary>
        /// Sets the default data provider to use.
        /// </summary>
        /// <param name="dataProvider">The data provider.</param>
        public static void SetDataProvider(AuditDataProvider dataProvider)
        {
            DataProvider = dataProvider;
        }
        /// <summary>
        /// Sets the default creation policy.
        /// </summary>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        public static void SetCreationPolicy(EventCreationPolicy creationPolicy)
        {
            CreationPolicy = creationPolicy;
        }
    }
}
