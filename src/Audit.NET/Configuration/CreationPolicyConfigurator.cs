namespace Audit.Core.Configuration
{
    public class CreationPolicyConfigurator : ICreationPolicyConfigurator
    {
        public IActionConfigurator WithCreationPolicy(EventCreationPolicy creationPolicy)
        {
            AuditConfiguration.SetCreationPolicy(creationPolicy);
            return new ActionConfigurator();
        }
    }
}