namespace Audit.EntityFramework
{
    public class ModeConfigurator<T> : IModeConfigurator<T> where T : AuditDbContext
    {
        public IIncludeConfigurator<T> UseOptIn()
        {
            Configuration.SetMode<T>(AuditOptionMode.OptIn);
            return new IncludeConfigurator<T>();
        }

        public IExcludeConfigurator<T> UseOptOut()
        {
            Configuration.SetMode<T>(AuditOptionMode.OptOut);
            return new ExcludeConfigurator<T>();
        }
    }
}