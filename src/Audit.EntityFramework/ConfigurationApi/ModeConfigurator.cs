namespace Audit.EntityFramework.ConfigurationApi
{
    public class ModeConfigurator<T> : IModeConfigurator<T>
    {
        public IIncludeEntityConfigurator<T> UseOptIn()
        {
            Configuration.SetMode<T>(AuditOptionMode.OptIn);
            return new IncludeEntityConfigurator<T>();
        }
        public IExcludeConfigurator<T> UseOptOut()
        {
            Configuration.SetMode<T>(AuditOptionMode.OptOut);
            return new ExcludeConfigurator<T>();
        }
        public IModeConfigurator<T> Reset()
        {
            Configuration.Reset<T>();
            return this;
        }
    }
}