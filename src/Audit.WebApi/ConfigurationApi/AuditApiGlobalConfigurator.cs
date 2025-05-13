#if ASP_CORE
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
#else
using ActionExecutingContext = System.Web.Http.Controllers.HttpActionContext;
using ActionExecutedContext = System.Web.Http.Filters.HttpActionExecutedContext;
using System.Net.Http;
#endif
using System;
using System.Collections.Generic;
using System.Text;


namespace Audit.WebApi.ConfigurationApi
{

    public class AuditApiGlobalConfigurator : IAuditApiGlobalConfigurator
    {
        internal Func<ActionExecutingContext, bool> _logDisabledBuilder;
        internal Func<ActionExecutingContext, bool> _includeRequestHeadersBuilder;
        internal Func<ActionExecutedContext, bool> _includeResponseHeadersBuilder;
        internal Func<ActionExecutedContext, bool> _includeModelStateBuilder;
        internal Func<ActionExecutingContext, bool> _includeRequestBodyBuilder;
        internal Func<ActionExecutedContext, bool> _includeResponseBodyBuilder;
        internal Func<ActionExecutingContext, string> _eventTypeNameBuilder;
#if ASP_NET
        internal Func<HttpRequestMessage, IContextWrapper> _contextWrapperBuilder;
#endif
        internal bool _serializeActionParameters;

        public IAuditApiGlobalConfigurator WithEventType(string eventTypeName)
        {
            _eventTypeNameBuilder = _ => eventTypeName;
            return this;
        }

        public IAuditApiGlobalConfigurator WithEventType(Func<ActionExecutingContext, string> eventTypeNameBuilder)
        {
            _eventTypeNameBuilder = eventTypeNameBuilder;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeHeaders(bool include = true)
        {
            _includeRequestHeadersBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeHeaders(Func<ActionExecutingContext, bool> includePredicate)
        {
            _includeRequestHeadersBuilder = includePredicate;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseHeaders(bool include = true)
        {
            _includeResponseHeadersBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseHeaders(Func<ActionExecutedContext, bool> includePredicate)
        {
            _includeResponseHeadersBuilder = includePredicate;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeModelState(bool include = true)
        {
            _includeModelStateBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeModelState(Func<ActionExecutedContext, bool> includePredicate)
        {
            _includeModelStateBuilder = includePredicate;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeRequestBody(bool include = true)
        {
            _includeRequestBodyBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeRequestBody(Func<ActionExecutingContext, bool> includePredicate)
        {
            _includeRequestBodyBuilder = includePredicate;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseBody(bool include = true)
        {
            _includeResponseBodyBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseBody(Func<ActionExecutedContext, bool> includePredicate)
        {
            _includeResponseBodyBuilder = includePredicate;
            return this;
        }

        public IAuditApiGlobalConfigurator SerializeActionParameters(bool serialize = true)
        {
            _serializeActionParameters = serialize;
            return this;
        }
    }
}
