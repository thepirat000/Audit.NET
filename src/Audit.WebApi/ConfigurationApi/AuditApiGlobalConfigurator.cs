#if NETCOREAPP3_0 || NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
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
#if NET45
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

        public IAuditApiGlobalConfigurator IncludeHeaders(Func<ActionExecutingContext, bool> includeBuilder)
        {
            _includeRequestHeadersBuilder = includeBuilder;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseHeaders(bool include = true)
        {
            _includeResponseHeadersBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseHeaders(Func<ActionExecutedContext, bool> includeBuilder)
        {
            _includeResponseHeadersBuilder = includeBuilder;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeModelState(bool include = true)
        {
            _includeModelStateBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeModelState(Func<ActionExecutedContext, bool> includeBuilder)
        {
            _includeModelStateBuilder = includeBuilder;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeRequestBody(bool include = true)
        {
            _includeRequestBodyBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeRequestBody(Func<ActionExecutingContext, bool> includeBuilder)
        {
            _includeRequestBodyBuilder = includeBuilder;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseBody(bool include = true)
        {
            _includeResponseBodyBuilder = _ => include;
            return this;
        }

        public IAuditApiGlobalConfigurator IncludeResponseBody(Func<ActionExecutedContext, bool> includeBuilder)
        {
            _includeResponseBodyBuilder = includeBuilder;
            return this;
        }
    }

}
