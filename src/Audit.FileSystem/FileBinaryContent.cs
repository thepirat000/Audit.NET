using System.Text.Json.Serialization;
using StringEnumConverter = System.Text.Json.Serialization.JsonStringEnumConverter;

namespace Audit.FileSystem
{
    public class FileBinaryContent : FileContent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Binary;
        public byte[] Value { get; set; }
    }
}
