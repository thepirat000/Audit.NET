using Newtonsoft.Json;

namespace Audit.WebApi
{
    public class BodyContent
    {
        public string Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public long? Length { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Value { get; set; }
    }
}
