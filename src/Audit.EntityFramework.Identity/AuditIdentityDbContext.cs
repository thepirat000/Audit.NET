using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audit.Core;
using System.Threading;
using Audit.EntityFramework.ConfigurationApi;
#if EF_CORE && NETSTANDARD1_5
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#elif EF_CORE
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
#elif EF_FULL
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#endif

namespace Audit.EntityFramework
{
    /// <summary>
    /// Base IdentityDbContext class for Audit. Inherit your IdentityDbContext from this class to enable audit.
    /// </summary>
#if EF_CORE
    public abstract partial class AuditIdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        : IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>, IAuditDbContext, IAuditBypass
#if NETSTANDARD1_5
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
#else
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
#endif
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
#else
    public abstract partial class AuditIdentityDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>
        : IdentityDbContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim>, IAuditDbContext, IAuditBypass
        where TUser : IdentityUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : IdentityRole<TKey, TUserRole>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
#endif
    {
        private DbContextHelper _helper = new DbContextHelper();

        /// <summary>
        /// Initializes a new instance of the AuditIdentityDbContext
        /// </summary>
        public AuditIdentityDbContext() : base()
        {
            _helper.SetConfig(this);
        }
#if EF_CORE
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="options">The options to be used by a Microsoft.EntityFrameworkCore.DbContext</param>
        public AuditIdentityDbContext(DbContextOptions options) : base(options)
        {
            _helper.SetConfig(this);
        }
#else
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        public AuditIdentityDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        { 
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(DbCompiledModel model) : base(model)
        { 
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        { 
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="nameOrConnectionString">Either the database name or a connection string.</param>
        /// <param name="model">The model that will back this context.</param>
        public AuditIdentityDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        { 
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of AuditIdentityDbContext
        /// </summary>
        /// <param name="existingConnection">An existing connection to use for the new context.</param>
        /// <param name="model">The model that will back this context.</param>
        /// <param name="contextOwnsConnection">If set to true the connection is disposed when the context is disposed, otherwise the caller must dispose the connection.</param>
        public AuditIdentityDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base (existingConnection, model, contextOwnsConnection)
        { 
            _helper.SetConfig(this);
        }
#endif
        /// <summary>
        /// To indicate the Audit Data Provider to use. (Default is NULL to use the configured default data provider). 
        /// </summary>
        public AuditDataProvider AuditDataProvider { get; set; }

        /// <summary>
        /// Indicates if the Audit is disabled.
        /// Default is false.
        /// </summary>
        public bool AuditDisabled { get; set; }

        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the context name). 
        /// Can contain the following placeholders: 
        ///  - {context}: replaced with the Db Context type name.
        ///  - {database}: replaced with the database name.
        /// </summary>
        public string AuditEventType { get; set; }

        /// <summary>
        /// The Db Context for this instance
        /// </summary>
        public DbContext DbContext { get { return this; } }

        /// <summary>
        /// Optional custom fields added to the audit event
        /// </summary>
        public Dictionary<string, object> ExtraFields { get; } = new Dictionary<string, object>();

        /// <summary>
        /// To indicate if the output should contain the modified entities objects. (Default is false)
        /// </summary>
        public bool IncludeEntityObjects { get; set; }

        /// <summary>
        /// To indicate if the entity validations should be avoided and excluded from the audit output. (Default is false)
        /// </summary>
        public bool ExcludeValidationResults { get; set; }

        /// <summary>
        /// To indicate the audit operation mode. (Default is OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        public AuditOptionMode Mode { get; set; }

        /// <summary>
        /// To indicate if the Transaction Id retrieval should be ignored. If set to <c>true</c> the Transations Id will not be included on the output.
        /// </summary>
        public bool ExcludeTransactionId { get; set; }

#if EF_FULL
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        public bool IncludeIndependantAssociations { get; set; }
#endif
        /// <summary>
        /// A collection of settings per entity type.
        /// </summary>
        public Dictionary<Type, EfEntitySettings> EntitySettings { get; set;  }

        /// <summary>
        /// Called after the audit scope is created.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public virtual void OnScopeCreated(AuditScope auditScope)
        {
        }

        /// <summary>
        /// Called after the EF operation execution and before the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public virtual void OnScopeSaving(AuditScope auditScope)
        {
        }

        /// <summary>
        /// Adds a custom field to the audit scope.
        /// The value will be serialized when SaveChanges takes place.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        public void AddAuditCustomField(string fieldName, object value)
        {
            ExtraFields[fieldName] = value;
        }

        /// <summary>
        /// Saves the changes synchronously.
        /// </summary>
        public override int SaveChanges()
        {
            return _helper.SaveChanges(this, () => base.SaveChanges());
        }

        /// <summary>
        /// Saves the changes asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await _helper.SaveChangesAsync(this, () => base.SaveChangesAsync(cancellationToken));
        }

        int IAuditBypass.SaveChangesBypassAudit()
        {
            return base.SaveChanges();
        }
        async Task<int> IAuditBypass.SaveChangesBypassAuditAsync()
        {
            return await base.SaveChangesAsync();
        }
    }
}