#if IS_NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json.Serialization;
using StringEnumConverter = System.Text.Json.Serialization.JsonStringEnumConverter;
#endif
using System;
using System.Collections.Generic;

namespace Audit.FileSystem
{
    public class FileSystemEvent
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystemObjectType Object { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystemEventType Event { get; set; }
        public List<string> Errors { get; set; }
        public string Attributes { get; set; }
        public string OldName { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public string FullPath { get; set; }
        public long? Length { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public bool ReadOnly { get; set; }
        public string MD5 { get; set; }
        public FileContent FileContent { get; set; }
    }
}
