using System;
using System.Collections.Generic;
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
        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the context name). 
        /// Can contain the following placeholders: 
        ///  - {context}: replaced with the Db Context type name.
        ///  - {database}: replaced with the database name.
        /// </summary>
        public virtual string AuditEventType { get; set; }

        /// <summary>
        /// Indicates if the Audit is disabled.
        /// Default is false.
        /// </summary>
        public virtual bool AuditDisabled { get; set; }

        /// <summary>
        /// To indicate if the output should contain the modified entities objects. (Default is false)
        /// </summary>
        public virtual bool IncludeEntityObjects { get; set; }

        /// <summary>
        /// To indicate if the entity validations should be avoided and excluded from the audit output. (Default is false)
        /// </summary>
        public virtual bool ExcludeValidationResults { get; set; }

        /// <summary>
        /// To indicate the audit operation mode. (Default is OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        public virtual AuditOptionMode Mode { get; set; }

        /// <summary>
        /// To indicate the Audit Data Provider to use. (Default is NULL to use the configured default data provider). 
        /// </summary>
        public virtual AuditDataProvider AuditDataProvider { get; set; }

        /// <summary>
        /// To indicate a custom audit scope factory. (Default is NULL to use the Audit.Core.Configuration.DefaultAuditScopeFactory). 
        /// </summary>
        public virtual IAuditScopeFactory AuditScopeFactory { get; set; }

        /// <summary>
        /// Optional custom fields added to the audit event
        /// </summary>
        public Dictionary<string, object> ExtraFields { get; } = new Dictionary<string, object>();

        /// <summary>
        /// To indicate if the Transaction Id retrieval should be ignored. If set to <c>true</c> the Transations Id will not be included on the output.
        /// </summary>
        public bool ExcludeTransactionId { get; set; }

        /// <summary>
        /// To indicate if the audit event should be saved before the entity saving operation takes place. 
        /// Default is false to save the audit event after the entity saving operation completes or fails.
        /// </summary>
        public bool EarlySavingAudit { get; set; }

        public DbContext DbContext { get { return this; } }
#if EF_FULL
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        public bool IncludeIndependantAssociations { get; set; }
#endif
        /// <summary>
        /// A collection of settings per entity type.
        /// </summary>
        public Dictionary<Type, EfEntitySettings> EntitySettings { get; set; }
        #endregion

        #region Public methods
        /// <summary>
        /// Called after the audit scope is created.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public virtual void OnScopeCreated(IAuditScope auditScope)
        {
        }
        /// <summary>
        /// Called after the EF operation execution and before the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
        public virtual void OnScopeSaving(IAuditScope auditScope)
        {
        }
        /// <summary>
        /// Called after the AuditScope saving.
        /// Override to specify custom logic.
        /// </summary>
        /// <param name="auditScope">The audit scope.</param>
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
        public override int SaveChanges()
        {
            return _helper.SaveChanges(this, () => base.SaveChanges());
        }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return _helper.SaveChangesAsync(this, () => base.SaveChangesAsync(cancellationToken));
        }
        int IAuditBypass.SaveChangesBypassAudit()
        {
            return base.SaveChanges();
        }
        Task<int> IAuditBypass.SaveChangesBypassAuditAsync()
        {
            return base.SaveChangesAsync(CancellationToken.None);
        }
#else
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return _helper.SaveChanges(this, () => base.SaveChanges(acceptAllChangesOnSuccess));
        }
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return _helper.SaveChangesAsync(this, () => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));
        }
        int IAuditBypass.SaveChangesBypassAudit()
        {
            return base.SaveChanges(true);
        }
        Task<int> IAuditBypass.SaveChangesBypassAuditAsync()
        {
            return base.SaveChangesAsync(true, default);
        }
#endif
        #endregion
    }
}
