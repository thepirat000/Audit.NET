#if NETSTANDARD1_5 || NETSTANDARD2_0 || NET461

using System;
using Microsoft.EntityFrameworkCore;
#if NETSTANDARD1_5
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#else
using Microsoft.AspNetCore.Identity;
#endif
namespace Audit.EntityFramework
{
    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext : AuditIdentityDbContext<IdentityUser, IdentityRole, string>, IAuditBypass
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
    public abstract class AuditIdentityDbContext<TUser> : AuditIdentityDbContext<TUser, IdentityRole, string>, IAuditBypass
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
#if NETSTANDARD1_5
    public abstract class AuditIdentityDbContext<TUser, TRole, TKey> : AuditIdentityDbContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>, IAuditBypass
        where TUser : IdentityUser<TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>>
        where TRole : IdentityRole<TKey, IdentityUserRole<TKey>, IdentityRoleClaim<TKey>>
#else
    public abstract class AuditIdentityDbContext<TUser, TRole, TKey> : AuditIdentityDbContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>, IAuditBypass
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
#endif
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