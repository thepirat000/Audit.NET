using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Audit.Core;
using Audit.EntityFramework.ConfigurationApi;
#if EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
#endif

namespace Audit.EntityFramework
{
    /// <summary>
    /// Base DbContext class for Audit. Inherit your DbContext from this class to enable audit.
    /// </summary>
    public abstract partial class AuditDbContext : DbContext, IAuditDbContext, IAuditBypass
    {
        private readonly DbContextHelper _helper = new DbContextHelper();

#if EF_CORE
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        protected AuditDbContext(DbContextOptions options) : base(options)
        {
            _helper.SetConfig(this);
        }
#else
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbCompiledModel model) : base(model)
        {
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(string nameOrConnectionString, DbCompiledModel model) : base(nameOrConnectionString, model)
        {
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection, contextOwnsConnection)
        {
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection) : base(existingConnection, model, contextOwnsConnection)
        {
            _helper.SetConfig(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext(ObjectContext objectContext, bool dbContextOwnsObjectContext) : base(objectContext, dbContextOwnsObjectContext)
        {
            _helper.SetConfig(this);
        }
#endif
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditDbContext" /> class.
        /// </summary>
        protected AuditDbContext() : base()
        {
            _helper.SetConfig(this);
        }

        #region Properties
        /// <inheritdoc/>
        public virtual string AuditEventType { get; set; }

        /// <inheritdoc/>
        public virtual bool AuditDisabled { get; set; }

        /// <inheritdoc/>
        public virtual bool IncludeEntityObjects { get; set; }

        /// <inheritdoc/>
        public virtual bool ExcludeValidationResults { get; set; }

        /// <inheritdoc/>
        public virtual AuditOptionMode Mode { get; set; }

        /// <inheritdoc/>
        public virtual AuditDataProvider AuditDataProvider { get; set; }

        /// <inheritdoc/>
        public virtual IAuditScopeFactory AuditScopeFactory { get; set; }

        /// <inheritdoc/>
        public Dictionary<string, object> ExtraFields { get; } = new Dictionary<string, object>();

        /// <inheritdoc/>
        public bool ExcludeTransactionId { get; set; }

        public DbContext DbContext { get { return this; } }
#if EF_FULL
        /// <inheritdoc/>
        public bool IncludeIndependantAssociations { get; set; }
#endif
        /// <inheritdoc/>
        public Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }

        /// <inheritdoc/>
        public bool ReloadDatabaseValues { get; set; }
        #endregion

        #region Public methods
        /// <inheritdoc/>
        public virtual void OnScopeCreated(IAuditScope auditScope)
        {
        }

        /// <inheritdoc/>
        public virtual void OnScopeSaving(IAuditScope auditScope)
        {
        }

        /// <inheritdoc/>
        public virtual void OnScopeSaved(IAuditScope auditScope)
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

#if EF_FULL
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int SaveChanges()
        {
            return _helper.SaveChanges(this, () => base.SaveChanges());
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return await _helper.SaveChangesAsync(this, () => base.SaveChangesAsync(cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Executes the SaveChanges operation in the DbContext and returns the EF audit event generated
        /// </summary>
        /// <returns>The generated EF audit event</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public EntityFrameworkEvent SaveChangesGetAudit()
        {
            return _helper.SaveChangesGetAudit(this, () => base.SaveChanges());
        }
        
        /// <summary>
        /// Executes the SaveChanges operation in the DbContext and returns the EF audit event generated
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The generated EF audit event</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<EntityFrameworkEvent> SaveChangesGetAuditAsync(CancellationToken cancellationToken = default)
        {
            return await _helper.SaveChangesGetAuditAsync(this, () => base.SaveChangesAsync(cancellationToken), cancellationToken);
        }
        
        int IAuditBypass.SaveChangesBypassAudit()
        {
            return base.SaveChanges();
        }
        
        Task<int> IAuditBypass.SaveChangesBypassAuditAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(cancellationToken);
        }
#else
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return _helper.SaveChanges(this, () => base.SaveChanges(acceptAllChangesOnSuccess));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return await _helper.SaveChangesAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Executes the SaveChanges operation in the DbContext and returns the EF audit event generated
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <returns>The generated EF audit event</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public EntityFrameworkEvent SaveChangesGetAudit(bool acceptAllChangesOnSuccess = true)
        {
            return _helper.SaveChangesGetAudit(this, () => base.SaveChanges(acceptAllChangesOnSuccess));
        }

        /// <summary>
        /// Executes the SaveChanges operation in the DbContext and returns the EF audit event generated
        /// </summary>
        /// <param name="acceptAllChangesOnSuccess">Indicates whether ChangeTracker.AcceptAllChanges is called after the changes have been sent successfully to the database.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The generated EF audit event</returns>

        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<EntityFrameworkEvent> SaveChangesGetAuditAsync(bool acceptAllChangesOnSuccess = true, CancellationToken cancellationToken = default)
        {
            return await _helper.SaveChangesGetAuditAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken), cancellationToken);
        }
       
        int IAuditBypass.SaveChangesBypassAudit()
        {
            return base.SaveChanges(true);
        }

        Task<int> IAuditBypass.SaveChangesBypassAuditAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(true, cancellationToken);
        }
#endif
        #endregion
    }
}
