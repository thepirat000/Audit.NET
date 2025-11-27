namespace Audit.Grpc.Server;

/// <summary>
/// Metadata entry for gRPC calls.
/// </summary>
public class GrpcMetadata
{
    /// <summary>
    /// The metadata key.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The metadata value as a string.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// The metadata value as a byte array.
    /// </summary>
    public byte[] ValueBytes { get; set; }

    /// <summary>
    /// Indicates whether the metadata value is binary.
    /// </summary>
    public bool IsBinary { get; set; }
}