using System;

namespace Audit.EntityFramework.ConfigurationApi
{
    public interface IEntityFrameworkProviderConfigurator
    {
        /// <summary>
        /// Specifies a function that maps an entity type to its audited type. If the function returns null for a given type, the audit event will not be saved.
        /// </summary>
        /// <param name="mapper">Function that maps an entity type to its audited type.</param>
        IEntityFrameworkProviderConfiguratorAction AuditTypeMapper(Func<Type, Type> mapper);
        /// <summary>
        /// Specifies a function that maps an entity type name to its audited type name. Both entities should be on the same assembly and namespace. If the function returns null for a given type name, the audit event will not be saved.
        /// </summary>
        /// <param name="mapper">Function that maps an entity type to its audited type.</param>
        IEntityFrameworkProviderConfiguratorAction AuditTypeNameMapper(Func<string, string> mapper);
        /// <summary>
        /// Specifies a mapping type to type.
        /// </summary>
        /// <param name="config">Mapping explicit configuration.</param>
        IEntityFrameworkProviderConfiguratorExtra AuditTypeExplicitMapper(Action<IAuditEntityMapping> config);
    }
}