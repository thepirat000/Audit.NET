namespace Audit.Core.ConfigurationApi
{
    public class CreationPolicyConfigurator : ICreationPolicyConfigurator
    {
        public IActionConfigurator WithCreationPolicy(EventCreationPolicy policy)
        {
            Configuration.CreationPolicy = policy;
            return new ActionConfigurator();
        }

        public IActionConfigurator WithManualCreationPolicy()
        {
            Configuration.CreationPolicy = EventCreationPolicy.Manual;
            return new ActionConfigurator();
        }

        public IActionConfigurator WithInsertOnEndCreationPolicy()
        {
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnEnd;
            return new ActionConfigurator();
        }

        public IActionConfigurator WithInsertOnStartReplaceOnEndCreationPolicy()
        {
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd;
            return new ActionConfigurator();
        }

        public IActionConfigurator WithInsertOnStartInsertOnEndCreationPolicy()
        {
            Configuration.CreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd;
            return new ActionConfigurator();
        }
    }
}