using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public class ContextSettingsConfigurator<T> : IContextSettingsConfigurator<T>
    {
        public IContextSettingsConfigurator<T> AuditEventType(string eventType)
        {
            Configuration.SetAuditEventType<T>(eventType);
            return this;
        }

        public IContextSettingsConfigurator<T> ForEntity<TEntity>(Action<IContextEntitySetting<TEntity>> config)
        {
            Configuration.SetContextEntitySetting<T, TEntity>(config);
            return this;
        }

        public IContextSettingsConfigurator<T> IncludeEntityObjects(bool include = true)
        {
            Configuration.SetIncludeEntityObjects<T>(include);
            return this;
        }
        
        public IContextSettingsConfigurator<T> ExcludeValidationResults(bool exclude = true)
        {
            Configuration.SetExcludeValidationResults<T>(exclude);
            return this;
        }

        public IContextSettingsConfigurator<T> ExcludeTransactionId(bool exclude = true)
        {
            Configuration.SetExcludeTransactionId<T>(exclude);
            return this;
        }

#if EF_FULL
        public IContextSettingsConfigurator<T> IncludeIndependantAssociations(bool include = true)
        {
            Configuration.SetIncludeIndependantAssociations<T>(include);
            return this;
        }
#endif

        public IContextSettingsConfigurator<T> ReloadDatabaseValues(bool reloadDatabaseValues = true)
        {
            Configuration.SetReloadDatabaseValues<T>(reloadDatabaseValues);
            return this;
        }
    }
}