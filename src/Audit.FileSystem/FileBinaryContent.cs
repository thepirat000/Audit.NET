using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.FileSystem
{
    public class FileBinaryContent : FileContent
    {
        [JsonProperty(Order = 10)]
        [JsonConverter(typeof(StringEnumConverter))]
        public override ContentType Type { get; set; } = ContentType.Binary;
        [JsonProperty(Order = 20)]
        public byte[] Value { get; set; }
    }
}
