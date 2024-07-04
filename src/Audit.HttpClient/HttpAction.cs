using System;
using System.Net.Http;

namespace Audit.Http
{
    public class HttpAction
    {
        public string Method { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public Request Request { get; set; }
        public Response Response { get; set; }
        public string Exception { get; set; }

        /// <summary>
        /// A weak reference to the HttpRequestMessage that originated this event
        /// </summary>
        private readonly WeakReference<HttpRequestMessage> _requestMessage = new WeakReference<HttpRequestMessage>(null);

        internal void SetRequestMessage(HttpRequestMessage requestMessage)
        {
            _requestMessage.SetTarget(requestMessage);
        }

        /// <summary>
        /// Returns the HttpRequestMessage associated to this Http Action.
        /// Returns NULL if the action is not associated to an HttpRequestMessage, or after it has been disposed of and garbage collected.
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage GetRequestMessage()
        {
            return _requestMessage.TryGetTarget(out var request) ? request : null;
        }

        /// <summary>
        /// A weak reference to the HttpResponseMessage associated to this event
        /// </summary>
        private readonly WeakReference<HttpResponseMessage> _responseMessage = new WeakReference<HttpResponseMessage>(null);

        internal void SetResponseMessage(HttpResponseMessage responseMessage)
        {
            _responseMessage.SetTarget(responseMessage);
        }

        /// <summary>
        /// Returns the HttpResponseMessage associated to this Http Action.
        /// Returns NULL if the action failed to execute, or after the HttpResponseMessage has been disposed of and garbage collected.
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage GetResponseMessage()
        {
            return _responseMessage.TryGetTarget(out var response) ? response : null;
        }
    }
}
