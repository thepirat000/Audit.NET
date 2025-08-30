using System.Text.Json.Serialization;

namespace Audit.FileSystem
{
    public class FileTextualContent : IFileContent
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContentType Type { get; set; } = ContentType.Text;
        public string Value { get; set; }
    }
}
