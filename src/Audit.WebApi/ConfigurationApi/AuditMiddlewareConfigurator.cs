#if NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Http;
using System;

namespace Audit.WebApi.ConfigurationApi
{
    public class AuditMiddlewareConfigurator : IAuditMiddlewareConfigurator
    {
        internal Func<HttpRequest, bool> _requestFilter;
        internal Func<HttpContext, bool> _includeRequestHeadersBuilder;
        internal Func<HttpContext, bool> _includeResponseHeadersBuilder;
        internal Func<HttpContext, bool> _includeRequestBodyBuilder;
        internal Func<HttpContext, bool> _includeResponseBodyBuilder;
        internal Func<HttpContext, string> _eventTypeNameBuilder;

        public IAuditMiddlewareConfigurator IncludeHeaders(bool include = true)
        {
            _includeRequestHeadersBuilder = _ => include;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeHeaders(Func<HttpContext, bool> includePredicate)
        {
            _includeRequestHeadersBuilder = includePredicate;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeResponseHeaders(bool include = true)
        {
            _includeResponseHeadersBuilder = _ => include;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeResponseHeaders(Func<HttpContext, bool> includePredicate)
        {
            _includeResponseHeadersBuilder = includePredicate;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeRequestBody(bool include = true)
        {
            _includeRequestBodyBuilder = _ => include;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeRequestBody(Func<HttpContext, bool> includePredicate)
        {
            _includeRequestBodyBuilder = includePredicate;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeResponseBody(bool include = true)
        {
            _includeResponseBodyBuilder = _ => include;
            return this;
        }

        public IAuditMiddlewareConfigurator IncludeResponseBody(Func<HttpContext, bool> includePredicate)
        {
            _includeResponseBodyBuilder = includePredicate;
            return this;
        }

        public IAuditMiddlewareConfigurator FilterByRequest(Func<HttpRequest, bool> requestPredicate)
        {
            _requestFilter = requestPredicate;
            return this;
        }

        public IAuditMiddlewareConfigurator WithEventType(string eventTypeName)
        {
            _eventTypeNameBuilder = _ => eventTypeName;
            return this;
        }

        public IAuditMiddlewareConfigurator WithEventType(Func<HttpContext, string> eventTypeNamePredicate)
        {
            _eventTypeNameBuilder = eventTypeNamePredicate;
            return this;
        }
    }
}
#endif