using Audit.EntityFramework.ConfigurationApi;
using System;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Attribute to define the Audit settings for the Db Context.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AuditDbContextAttribute : Attribute
    {
        internal EfSettings InternalConfig = new EfSettings();

        /// <summary>
        /// To indicate if the Transaction Id retrieval should be ignored. If set to <c>true</c> the Transations Id will not be included on the output.
        /// </summary>
        public bool ExcludeTransactionId
        {
            get
            {
                return InternalConfig.ExcludeTransactionId.HasValue && InternalConfig.ExcludeTransactionId.Value;
            }
            set
            {
                InternalConfig.ExcludeTransactionId = value;
            }
        }
#if NET45
        /// <summary>
        /// Value to indicate if the Independant Associations should be included. Independant associations are logged on EntityFrameworkEvent.Associations.
        /// </summary>
        public bool IncludeIndependantAssociations
        { 
            get
            {
                return InternalConfig.IncludeIndependantAssociations.HasValue && InternalConfig.IncludeIndependantAssociations.Value;
            }
            set
            {
                InternalConfig.IncludeIndependantAssociations = value;
            }
        }
#endif

        /// <summary>
        /// To indicate if the output should contain the modified entities objects. (Default is false)
        /// </summary>
        public bool IncludeEntityObjects
        {
            get
            {
                return InternalConfig.IncludeEntityObjects.HasValue && InternalConfig.IncludeEntityObjects.Value;
            }
            set
            {
                InternalConfig.IncludeEntityObjects = value;
            }
        }
        /// <summary>
        /// To indicate the audit operation mode. (Default if OptOut). 
        ///  - OptOut: All the entities are tracked by default, except those decorated with the AuditIgnore attribute. 
        ///  - OptIn: No entity is tracked by default, except those decorated with the AuditInclude attribute.
        /// </summary>
        public AuditOptionMode Mode
        {
            get
            {
                return InternalConfig.Mode.HasValue ? InternalConfig.Mode.Value : AuditOptionMode.OptOut;
            }
            set
            {
                InternalConfig.Mode = value;
            }
        }
        /// <summary>
        /// To indicate the event type to use on the audit event. (Default is the context name). 
        /// Can contain the following placeholders: 
        ///  - {context}: replaced with the Db Context type name.
        ///  - {database}: replaced with the database name.
        /// </summary>
        public string AuditEventType
        {
            get
            {
                return InternalConfig.AuditEventType;
            }
            set
            {
                InternalConfig.AuditEventType = value;
            }
        }
    }
}