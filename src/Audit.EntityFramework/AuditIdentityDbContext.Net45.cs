#if NET45

using System.Data.Entity.Infrastructure;
using System.Data.Common;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext : AuditIdentityDbContext<IdentityUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public AuditIdentityDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(DbCompiledModel model) : base(model)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        /// <param name="model">The model that will back this context.</param>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        { }
    }

    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
    public abstract class AuditIdentityDbContext<TUser> : AuditIdentityDbContext<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
        where TUser : IdentityUser
    {
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public AuditIdentityDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(DbCompiledModel model) : base(model)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        { }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        /// <param name="model">The model that will back this context.</param>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        { }
    }
}

#endif