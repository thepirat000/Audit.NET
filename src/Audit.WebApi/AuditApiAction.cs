using System.Collections.Generic;
using Audit.Core;
#if ASP_CORE
using Microsoft.AspNetCore.Mvc.Filters;
#else
using System.Web.Http.Controllers;
#endif
using System.Text.Json.Serialization;

namespace Audit.WebApi
{
    public class AuditApiAction : IAuditOutput
    {
        public string TraceId { get; set; }
        public string HttpMethod { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public IDictionary<string, object> ActionParameters { get; set; }
        public IDictionary<string, string> FormVariables { get; set; }
        public string UserName { get; set; }
        public string RequestUrl { get; set; }
        public string IpAddress { get; set; }
        public string ResponseStatus { get; set; }
        public int ResponseStatusCode { get; set; }
        public BodyContent RequestBody { get; set; }
        public BodyContent ResponseBody { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, string> ResponseHeaders { get; set; }
        public bool? ModelStateValid { get; set; }
        public IDictionary<string, string> ModelStateErrors { get; set; }
        public string Exception { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
#if ASP_CORE
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
            return Core.Configuration.JsonAdapter.Serialize(this);
        }
        /// <summary>
        /// Parses an Audit Action from its JSON string representation.
        /// </summary>
        /// <param name="json">JSON string with the Entity Audit Action representation.</param>
        public static AuditApiAction FromJson(string json)
        {
            return Core.Configuration.JsonAdapter.Deserialize<AuditApiAction>(json);
        }
    }
}
