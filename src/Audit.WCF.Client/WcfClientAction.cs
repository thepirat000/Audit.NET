using System.Collections.Generic;
using System.Net;

namespace Audit.Wcf.Client
{
    public class WcfClientAction
    {
        /// <summary>
        /// Request action URL
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// Request body XML
        /// </summary>
        public string RequestBody { get; set; }
        /// <summary>
        /// Request headers
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; }
        /// <summary>
        /// HTTP method
        /// </summary>
        public string HttpMethod { get; set; }
        /// <summary>
        /// Response action
        /// </summary>
        public string ResponseAction { get; set; }
        /// <summary>
        /// Message ID
        /// </summary>
        public string MessageId { get; set; }
        /// <summary>
        /// Response HTTP status code
        /// </summary>
        public HttpStatusCode? ResponseStatuscode { get; set; }
        /// <summary>
        /// Response body XML
        /// </summary>
        public string ResponseBody { get; set; }
        /// <summary>
        /// Response headers
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; }
        /// <summary>
        /// Value that indicates whether this message generates any SOAP faults.
        /// </summary>
        public bool IsFault { get; set; }

    }
}
