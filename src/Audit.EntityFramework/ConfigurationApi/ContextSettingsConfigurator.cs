namespace Audit.EntityFramework
{
    public class ContextSettingsConfigurator<T> : IContextSettingsConfigurator<T>
        where T : AuditDbContext
    {
        public IContextSettingsConfigurator<T> AuditEventType(string eventType)
        {
            Configuration.SetAuditEventType<T>(eventType);
            return this;
        }
        public IContextSettingsConfigurator<T> IncludeEntityObjects(bool include = true)
        {
            Configuration.SetIncludeEntityObjects<T>(include);
            return this;
        }
    }
}