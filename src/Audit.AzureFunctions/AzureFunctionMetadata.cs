namespace Audit.AzureFunctions;

/// <summary>
/// Contains metadata about an Azure Functions binding.
/// </summary>
public class AzureFunctionMetadata
{
    /// <summary>
    /// Gets the type of the binding.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets the name of the binding metadata entry.
    /// </summary>
    public string Name { get; set; }
}