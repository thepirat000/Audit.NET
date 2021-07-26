#if IS_NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json.Serialization;
using StringEnumConverter = System.Text.Json.Serialization.JsonStringEnumConverter;
#endif

namespace Audit.FileSystem
{
    public class FileTextualContent : FileContent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Text;
        public string Value { get; set; }
    }
}
