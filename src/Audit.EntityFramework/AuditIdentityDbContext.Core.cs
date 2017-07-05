#if NETSTANDARD1_5

using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext : AuditIdentityDbContext<IdentityUser, IdentityRole, string>
    {
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="options">The options to be used by a Microsoft.EntityFrameworkCore.DbContext</param>
        public AuditIdentityDbContext(DbContextOptions options) : base(options)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        { }
    }

    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext<TUser> : AuditIdentityDbContext<TUser, IdentityRole, string>
        where TUser : IdentityUser
    {
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="options">The options to be used by a Microsoft.EntityFrameworkCore.DbContext</param>
        public AuditIdentityDbContext(DbContextOptions options) : base(options)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        { }
    }

    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext<TUser, TRole, TKey> : AuditIdentityDbContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="options">The options to be used by a Microsoft.EntityFrameworkCore.DbContext</param>
        public AuditIdentityDbContext(DbContextOptions options) : base(options)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        { }
    }
}

#endif