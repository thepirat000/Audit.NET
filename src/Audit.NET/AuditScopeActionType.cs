namespace Audit.Core
{
    /// <summary>
    /// To indicate when the action on the Audit Scope should be performed.
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// When the Audit Scope is being created, before any saving.
        /// </summary>
        OnScopeCreated = 0,
        /// <summary>
        /// When the Audit Event on the Scope is about to be saved.
        /// </summary>
        OnEventSaving = 1
    }
}
