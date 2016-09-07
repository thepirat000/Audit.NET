namespace Audit.EntityFramework
{
    /// <summary>
    /// The AuditDbContext operation mode
    /// </summary>
    public enum AuditOptionMode
    {
        /// <summary>
        /// All the entities are audited, except those explicitly ignored with AuditIgnoreAttribute. This is the default mode.
        /// </summary>
        OptOut = 0,
        /// <summary>
        /// No entity is audited except those explicitly included with AuditIncludeAttribute.
        /// </summary>        
        OptIn = 1
    }
}