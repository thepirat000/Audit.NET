namespace Audit.Core.ConfigurationApi
{
    public class CreationPolicyConfigurator : ICreationPolicyConfigurator
    {
        public IActionConfigurator WithCreationPolicy(EventCreationPolicy creationPolicy)
        {
            Configuration.CreationPolicy = creationPolicy;
            return new ActionConfigurator();
        }
    }
}