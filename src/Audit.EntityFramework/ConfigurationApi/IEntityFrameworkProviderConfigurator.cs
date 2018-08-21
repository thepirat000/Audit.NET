using System;
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461
using Microsoft.EntityFrameworkCore;
#elif NET45
using System.Data.Entity;
#endif

namespace Audit.EntityFramework.ConfigurationApi
{
    public interface IEntityFrameworkProviderConfigurator
    {
        /// <summary>
        /// Provides a custom Db Context to use **for storing the Audit Events**. By default it uses the same DbContext that is being audited.
        /// </summary>
        /// <param name="dbContextBuilder">A function that given an EF audit event, returns a custom DbContext to use **for storing the audit events**.</param>
        IEntityFrameworkProviderConfigurator UseDbContext(Func<AuditEventEntityFramework, DbContext> dbContextBuilder);
        /// <summary>
        /// Provides a custom Db Context to use **for storing the Audit Events**. By default it uses the same DbContext that is being audited.
        /// </summary>
        /// <param name="constructorArgs">The arguments to pass to the <typeparamref name="T"/> constructor, if any.</param>
        IEntityFrameworkProviderConfigurator UseDbContext<T>(params object[] constructorArgs) where T : DbContext;
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