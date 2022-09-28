namespace Audit.NET.Serilog
{
    public enum LogLevel
    {
        /// <summary>
        ///     Debug level.
        /// </summary>
        Debug = 0,

        /// <summary>
        ///     Information.
        /// </summary>
        Info = 1,

        /// <summary>
        ///     Warning.
        /// </summary>
        Warn = 2,

        /// <summary>
        ///     Error.
        /// </summary>
        Error = 3,

        /// <summary>
        ///     Fatal.
        /// </summary>
        Fatal = 4,
    }
}