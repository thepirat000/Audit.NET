#if NET7_0_OR_GREATER
using System;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework;

public static class DbContextExtensions
{
    /// <summary>
    /// Gets a DbSet for a given entity type.
    /// </summary>
    /// <param name="dbContext">The DbContext instance</param>
    /// <param name="entityType">The entity type</param>
    /// <returns>The DbSet instance for the given entity type. </returns>
    public static object Set(this DbContext dbContext, Type entityType)
    {
        var methodSet = dbContext.GetType().GetMethod("Set", new Type[0])!.MakeGenericMethod(entityType);

        var dbSet = methodSet.Invoke(dbContext, new object[0]);

        return dbSet;
    }
}
#endif