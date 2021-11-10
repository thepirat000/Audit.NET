using System;
#if EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

namespace Audit.EntityFramework.ConfigurationApi
{
    /// <summary>
    /// Provides a global configuration for Audit.EntityFramework
    /// </summary>
    public interface IContextConfigurator
    {
        /// <summary>
        /// Provides a configuration for a specific AuditDbContext (has precedence over ForAnyContext)
        /// </summary>
        /// <param name="config">The context configuration</param>
        IModeConfigurator<T> ForContext<T>(Action<IContextSettingsConfigurator<T>> config = null) where T : DbContext;
        /// <summary>
        /// Provides a configuration for all the AuditDbContext
        /// </summary>
        /// <param name="config">The context configuration</param>
        IModeConfigurator<AuditDbContext> ForAnyContext(Action<IContextSettingsConfigurator<AuditDbContext>> config = null);
    }
}