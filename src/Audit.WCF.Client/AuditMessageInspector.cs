using Audit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Audit.Wcf.Client
{
    /// <summary>
    /// Message inpector to intercept and log requests and responses for WCF client calls
    /// </summary>
    public class AuditMessageInspector : IClientMessageInspector
    {
        private readonly string _eventType = "{action}";
        private readonly bool _includeRequestHeaders;
        private readonly bool _includeResponseHeaders;

        public AuditMessageInspector()
        {
        }

        public AuditMessageInspector(string eventType, bool includeRequestHeaders, bool includeResponseHeaders)
        {
            _eventType = eventType ?? "{action}";
            _includeRequestHeaders = includeRequestHeaders;
            _includeResponseHeaders = includeResponseHeaders;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // Create the Wcf client audit event
            var auditWcfEvent = CreateWcfClientAction(request);
            // Create the audit scope
            var eventType = _eventType.Replace("{action}", auditWcfEvent.Action);
            var auditEventWcf = new AuditEventWcfClient()
            {
                WcfClientEvent = auditWcfEvent
            };
            // Create the audit scope
            var auditScope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEventWcf,
                SkipExtraFrames = 8
            });
            return auditScope;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            var auditScope = correlationState as AuditScope;
            if (auditScope == null)
            {
                return;
            }

            // Update the WCF audit event with results
            var auditWcfEvent = auditScope.EventAs<AuditEventWcfClient>().WcfClientEvent;
            auditWcfEvent.IsFault = reply.IsFault;
            auditWcfEvent.ResponseAction = reply.Headers?.Action;
            auditWcfEvent.ResponseBody = reply.ToString();
            if (reply.Properties.TryGetValue("httpResponse", out var property))
            {
                var res = property as HttpResponseMessageProperty;
                auditWcfEvent.ResponseStatuscode = res?.StatusCode;
                auditWcfEvent.ResponseHeaders = _includeResponseHeaders ? GetHeaders(res?.Headers?.ToString()) : null;
            }
            auditScope.Dispose();
        }

        private Dictionary<string, string> GetHeaders(string headers)
        {
            if (headers == null)
            {
                return null;
            }
            return headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(h =>
                {
                    var kv = h.Split(':').ToArray();
                    return kv.Length > 1 ? new KeyValuePair<string, string>(kv[0], kv[1].TrimStart()) : new KeyValuePair<string, string>(kv[0], "");
                })
                .ToDictionary(k => k.Key, v => v.Value);
        }

        private WcfClientAction CreateWcfClientAction(Message request)
        {
            var action = new WcfClientAction()
            {
                Action = request.Headers?.Action,
                IsFault = request.IsFault,
                RequestBody = request.ToString(),
                MessageId = request.Headers?.MessageId?.ToString()
            };
            if (request.Properties.TryGetValue("httpRequest", out var property))
            {
                var req = property as HttpRequestMessageProperty;
                action.HttpMethod = req?.Method;
                action.RequestHeaders = _includeRequestHeaders ? GetHeaders(req?.Headers?.ToString()) : null;
            }
            return action;
        }
    }
}
