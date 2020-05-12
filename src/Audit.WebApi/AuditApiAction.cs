using System.Collections.Generic;
using Audit.Core;
using Newtonsoft.Json;
#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Web.Http.Controllers;
#endif

namespace Audit.WebApi
{
    public class AuditApiAction : IAuditOutput
    {
        [JsonProperty(Order = -1, NullValueHandling = NullValueHandling.Ignore)]
        public string TraceId { get; set; }
        [JsonProperty(Order = 0)]
        public string HttpMethod { get; set; }
        [JsonProperty(Order = 5, NullValueHandling = NullValueHandling.Ignore)]
        public string ControllerName { get; set; }
        [JsonProperty(Order = 10, NullValueHandling = NullValueHandling.Ignore)]
        public string ActionName { get; set; }
        [JsonProperty(Order = 15, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> ActionParameters { get; set; }
        [JsonProperty(Order = 17)]
        public IDictionary<string, string> FormVariables { get; set; }
        [JsonProperty(Order = 20)]
        public string UserName { get; set; }
        [JsonProperty(Order = 25)]
        public string RequestUrl { get; set; }
        [JsonProperty(Order = 30)]
        public string IpAddress { get; set; }
        [JsonProperty(Order = 35)]
        public string ResponseStatus { get; set; }
        [JsonProperty(Order = 40)]
        public int ResponseStatusCode { get; set; }
        [JsonProperty(Order = 42, NullValueHandling = NullValueHandling.Ignore)]
        public BodyContent RequestBody { get; set; }
        [JsonProperty(Order = 43, NullValueHandling = NullValueHandling.Ignore)]
        public BodyContent ResponseBody { get; set; }
        [JsonProperty(Order = 45, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Headers { get; set; }
        [JsonProperty(Order = 47, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> ResponseHeaders { get; set; }
        [JsonProperty(Order = 50, NullValueHandling = NullValueHandling.Ignore)]
        public bool? ModelStateValid { get; set; }
        [JsonProperty(Order = 55, NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> ModelStateErrors { get; set; }
        [JsonProperty(Order = 999, NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        [JsonIgnore]
        internal bool IsMiddleware { get; set; }

        [JsonIgnore]
        internal ActionExecutingContext ActionExecutingContext { get; set; }
        /// <summary>
        /// Gets the ActionExecutingContext related to this event
        /// </summary>
        public ActionExecutingContext GetActionExecutingContext()
        {
            return ActionExecutingContext;
        }
#else
        [JsonIgnore]
        internal HttpActionContext HttpActionContext { get; set; }
        /// <summary>
        /// Gets the HttpActionContext related to this event
        /// </summary>
        public HttpActionContext GetHttpActionContext()
        {
            return HttpActionContext;
        }
#endif
        /// <summary>
        /// Serializes this Audit Action as a JSON string
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Core.Configuration.JsonSettings);
        }
        /// <summary>
        /// Parses an Audit Action from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Audit Action representation.</param>
        public static AuditApiAction FromJson(string json)
        {
            return JsonConvert.DeserializeObject<AuditApiAction>(json, Core.Configuration.JsonSettings);
        }
    }
}
