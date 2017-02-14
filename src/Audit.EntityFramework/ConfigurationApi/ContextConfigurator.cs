using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ContextConfigurator : IContextConfigurator
    {
        public IModeConfigurator<AuditDbContext> ForAnyContext(Action<IContextSettingsConfigurator<AuditDbContext>> config = null)
        {
            if (config != null)
            {
                var contextConfig = new ContextSettingsConfigurator<AuditDbContext>();
                config.Invoke(contextConfig);
            }
            return new ModeConfigurator<AuditDbContext>();
        }
        public IModeConfigurator<T> ForContext<T>(Action<IContextSettingsConfigurator<T>> config = null) where T : IAuditDbContext
        {
            if (config != null)
            {
                var contextConfig = new ContextSettingsConfigurator<T>();
                config.Invoke(contextConfig);
            }
            return new ModeConfigurator<T>();
        }
    }
}


