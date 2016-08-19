namespace Audit.Core
{
    /// <summary>
    /// Data Access Interface
    /// </summary>
    public interface IAuditDataProvider
    {
        /// <summary>
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="settings">The settings.</param>
        void WriteEvent(AuditEvent auditEvent);
        /// <summary>
        /// Override this method to provide a different serialization method for the values that need to be serialized before saving 
        /// (old target value & custom fields)
        /// </summary>
        object Serialize<T>(T value);
        /// <summary>
        /// Tests the connection to the database.
        /// </summary>
        /// <returns><c>true</c> if connection is sucessfull, <c>false</c> otherwise.</returns>
        bool TestConnection();
        /// <summary>
        /// Initialization method, called when the scope is created
        /// </summary>
        /// <param name="auditEvent">The audit event being created.</param>
        void Initialize(AuditEvent auditEvent);
    }
}
