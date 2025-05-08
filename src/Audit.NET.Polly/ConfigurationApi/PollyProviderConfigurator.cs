using Audit.Core;

namespace Audit.Polly.Configuration
{
    public class PollyProviderConfigurator : IPollyProviderConfigurator
    {
        internal PollyResilienceConfigurator? _resilienceConfigurator;

        public IPollyResilienceConfigurator DataProvider(IAuditDataProvider primaryDataProvider)
        {
            _resilienceConfigurator = new PollyResilienceConfigurator()
            {
                _innerDataProvider = primaryDataProvider
            };
            
            return _resilienceConfigurator;
        }
    }
}