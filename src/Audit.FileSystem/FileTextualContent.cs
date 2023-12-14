using System.Text.Json.Serialization;

namespace Audit.FileSystem
{
    public class FileTextualContent : FileContent
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Text;
        public string Value { get; set; }
    }
}
