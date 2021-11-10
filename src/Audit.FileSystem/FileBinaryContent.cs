#if IS_NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json.Serialization;
using StringEnumConverter = System.Text.Json.Serialization.JsonStringEnumConverter;
#endif

namespace Audit.FileSystem
{
    public class FileBinaryContent : FileContent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Binary;
        public byte[] Value { get; set; }
    }
}
