using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Audit.FileSystem
{
    public class FileSystemEvent
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FileSystemObjectType Object { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
