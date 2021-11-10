
namespace Audit.Core
{
    /// <summary>
    /// Indicates the current save mode for the scope
    /// </summary>
    public enum SaveMode
    {
        /// <summary>
        /// Manual save mode
        /// </summary>
        Manual = 0,
        /// <summary>
        /// Insert on Start mode for creation policies InsertOnStartReplaceOnEnd and InsertOnStartInsertOnEnd
        /// </summary>
        InsertOnStart = 1,
        /// <summary>
        /// Insert on End mode for creation policies InsertOnStartInsertOnEnd and InsertOnEnd
        /// </summary>
        InsertOnEnd = 2,
        /// <summary>
        /// Replace on End mode for creation policy InsertOnStartReplaceOnEnd
        /// </summary>
        ReplaceOnEnd = 3
    }
}
