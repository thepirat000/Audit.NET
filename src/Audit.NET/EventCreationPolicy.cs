namespace Audit.Core
{
    /// <summary>
    /// Event Creation behaviors
    /// </summary>
    public enum EventCreationPolicy
    {
        /// <summary> Insert/Replace the event manually by calling .Save() method on AuditScope.</summary>
        Manual = -1,
        /// <summary> Insert the event when the scope is disposed. (Default)</summary>
        InsertOnEnd = 0,
        /// <summary> Insert the event when the scope starts, replace the event when the scope ends.</summary>
        InsertOnStartReplaceOnEnd = 1,
        /// <summary> Insert the event when the scope starts, and insert another when the scope ends.</summary>
        InsertOnStartInsertOnEnd = 2
    }
}
