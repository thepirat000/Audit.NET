using System.Collections.Generic;

namespace Audit.AzureFunctions;

/// <summary>
/// Represents the definition of an Azure Function, including its identity, entry point, assembly location, and binding metadata.
/// </summary>
public class AzureFunctionDefinition
{
    /// <summary>
    /// Gets the unique function id, assigned by the Functions host.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets the unique function name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the method entry point to the function.
    /// </summary>
    public string EntryPoint { get; set; }

    /// <summary>
    /// Gets the path to the assembly that contains the function.
    /// </summary>
    public string Assembly { get; set; }

    /// <summary>
    /// Gets the parameters for the function.
    /// </summary>
    public List<AzureFunctionMetadata> Parameters { get; set; }
    
    /// <summary>
    /// Gets the input binding metadata.
    /// </summary>
    public List<AzureFunctionMetadata> InputBindings { get; set; }

    /// <summary>
    /// Gets the output binding metadata.
    /// </summary>
    public List<AzureFunctionMetadata> OutputBindings { get; set; }
}