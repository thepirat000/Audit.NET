using Audit.Core;

namespace Audit.AzureFunctions;

/// <summary>
/// Represents an audit event that captures information about an Azure Function invocation.
/// </summary>
/// <remarks>This class is used to record details of Azure Function executions for auditing purposes. It extends the base AuditEvent type to include Azure Function-specific call information.</remarks>
public class AuditEventAzureFunction : AuditEvent
{
    /// <summary>
    /// Azure Function call details associated with this audit event.
    /// </summary>
    public AzureFunctionCall Call { get; set; }
}