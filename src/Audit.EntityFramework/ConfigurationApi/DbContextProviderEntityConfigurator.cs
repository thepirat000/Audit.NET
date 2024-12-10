#if NET7_0_OR_GREATER
using System;
using Audit.Core;

namespace Audit.EntityFramework.ConfigurationApi;

public class DbContextProviderEntityConfigurator<TEntity> : IDbContextProviderEntityConfigurator<TEntity>
    where TEntity : class, new()
{
    internal Action<AuditEvent, TEntity> _mapper;
    internal bool _disposeDbContext;

    public IDbContextProviderEntityConfigurator<TEntity> Mapper(Action<AuditEvent, TEntity> mapper)
    {
        _mapper = mapper;
        return this;
    }
    
    public IDbContextProviderEntityConfigurator<TEntity> DisposeDbContext(bool dispose = true)
    {
        _disposeDbContext = dispose;
        return this;
    }
}
#endif