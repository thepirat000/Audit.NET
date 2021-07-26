#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NET5_0
using Microsoft.AspNetCore.Mvc.Filters;
#endif
using System.Collections.Generic;
using Audit.Core;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif


namespace Audit.Mvc
{
    public class AuditAction : IAuditOutput
    {
        public string TraceId { get; set; }
        public string HttpMethod { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string ViewName { get; set; }
        public string ViewPath { get; set; }
        public IDictionary<string, string> FormVariables { get; set; }
        public IDictionary<string, object> ActionParameters { get; set; }
        public BodyContent RequestBody { get; set; }
        public BodyContent ResponseBody { get; set; }
        public string UserName { get; set; }
        public string RequestUrl { get; set; }
        public string IpAddress { get; set; }
        public string ResponseStatus { get; set; }
        public int ResponseStatusCode { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public object Model { get; set; }
        public bool? ModelStateValid { get; set; }
        public IDictionary<string, string> ModelStateErrors { get; set; }
        public string RedirectLocation { get; set; }
        public string Exception { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NET5_0
        [JsonIgnore]
        internal PageHandlerExecutingContext PageHandlerExecutingContext { get; set; }
        /// <summary>
        /// Gets the ActionExecutingContext related to this event
        /// </summary>
        public PageHandlerExecutingContext GetPageHandlerExecutingContext()
        {
            return PageHandlerExecutingContext;
        }
#endif

        /// <summary>
        /// Serializes this Audit Action as a JSON string
        /// </summary>
        public string ToJson()
        {
            return Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Audit Action from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Audit Action representation.</param>
        public static AuditAction FromJson(string json)
        {
            return Configuration.JsonAdapter.Deserialize<AuditAction>(json);
        }
    }
}