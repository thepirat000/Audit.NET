using System;

namespace Audit.Core.ConfigurationApi
{
    public interface IConditionalDataProviderConfigurator
    {
        /// <summary>
        /// Attaches a conditional data provider using a deferred factory method that will be invoked for each Audit Event that meets the guard condition
        /// </summary>
        /// <param name="guardCondition">The guard condition that should be met in order to use the given data provider factory</param>
        /// <param name="dataProviderFactory">The data provider factory to use when the guard condition is satisfied. This factory method will be invoked for each Audit Event that meets the guard condition</param>
        IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, Func<AuditEvent, AuditDataProvider> dataProviderFactory);

        /// <summary>
        /// Attaches a conditional data provider using a lazy initializer that will be invoked at most once; when the first Audit Event meets the guard condition.
        /// Note that the AuditEvent provided to the GuardCondition will be null when resolving the data provider for retrieving events.
        /// </summary>
        /// <param name="guardCondition">The guard condition that should be met in order to use the given data provider factory</param>
        /// <param name="dataProviderInitializer">The data provider lazy initializer to use when the guard condition is satisfied. It's invoked for the initial Audit Event meeting the guard condition; subsequent events fulfilling the guard condition will reuse the data provider instance</param>
        IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, Func<AuditDataProvider> dataProviderInitializer);

        /// <summary>
        /// Attaches a conditional data provider depending on the AuditEvent.
        /// Note that the AuditEvent provided to the GuardCondition will be null when resolving the data provider for retrieving events.
        /// </summary>
        /// <param name="guardCondition">The guard condition that should be met in order to use the given data provider</param>
        /// <param name="dataProvider">The data provider to use when the guard condition is met</param>
        IConditionalDataProviderConfigurator When(Func<AuditEvent, bool> guardCondition, AuditDataProvider dataProvider);

        /// <summary>
        /// Attaches a default data provider using a deferred factory method, invoked for each Audit Event that meets no other guard condition
        /// </summary>
        /// <param name="dataProviderFactory">The data provider factory to use when no other guard condition is satisfied</param>
        void Otherwise(Func<AuditEvent, AuditDataProvider> dataProviderFactory);

        /// <summary>
        /// Attaches a default data provider using a lazy initializer, invoked at most once when the first Audit Event meets no other guard condition.
        /// </summary>
        /// <param name="dataProviderInitializer">The data provider lazy initializer to use when no other guard condition is satisfied. It's invoked for the initial Audit Event meeting no other guard condition; subsequent events will reuse the data provider instance</param>
        void Otherwise(Func<AuditDataProvider> dataProviderInitializer);

        /// <summary>
        /// Attaches a default data provider to use when no other condition is satisfied.
        /// </summary>
        /// <param name="dataProvider">The data provider to use when no other guard condition is satisfied</param>
        void Otherwise(AuditDataProvider dataProvider);
    }
}