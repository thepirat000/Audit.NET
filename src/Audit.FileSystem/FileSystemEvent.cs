using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.FileSystem
{
    public class FileSystemEvent
    {
        [JsonProperty(Order = 5)]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystemObjectType Object { get; set; }
        [JsonProperty(Order = 10)]
        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystemEventType Event { get; set; }
        [JsonProperty(Order = 20, NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Errors { get; set; }
        [JsonProperty(Order = 30, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Attributes { get; set; }
        [JsonProperty(Order = 40, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string OldName { get; set; }
        [JsonProperty(Order = 50, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty(Order = 70)]
        public string Extension { get; set; }
        [JsonProperty(Order = 80)]
        public string FullPath { get; set; }
        [JsonProperty(Order = 90, NullValueHandling = NullValueHandling.Ignore)]
        public long? Length { get; set; }
        [JsonProperty(Order = 100, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime CreationTime { get; set; }
        [JsonProperty(Order = 110, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime LastAccessTime { get; set; }
        [JsonProperty(Order = 120, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime LastWriteTime { get; set; }
        [JsonProperty(Order = 130, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool ReadOnly { get; set; }
        [JsonProperty(Order = 140, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string MD5 { get; set; }
        [JsonProperty(Order = 150, DefaultValueHandling = DefaultValueHandling.Ignore)]
        public FileContent FileContent { get; set; }
    }
}
