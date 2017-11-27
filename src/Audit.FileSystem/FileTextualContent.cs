using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.FileSystem
{
    public class FileTextualContent : FileContent
    {
        [JsonProperty(Order = 10)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Text;
        [JsonProperty(Order = 20)]
        public string Value { get; set; }
    }
}
