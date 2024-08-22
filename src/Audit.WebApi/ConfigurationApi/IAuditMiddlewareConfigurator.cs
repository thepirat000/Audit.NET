#if ASP_CORE
using Microsoft.AspNetCore.Http;
using System;

namespace Audit.WebApi.ConfigurationApi
{
    public interface IAuditMiddlewareConfigurator
    {
        /// <summary>
        /// Specifies a filter function to determine if a request should be logged.
        /// </summary>
        /// <param name="requestPredicate">Return true to include the request on the audit output, or false otherwise.</param>
        IAuditMiddlewareConfigurator FilterByRequest(Func<HttpRequest, bool> requestPredicate);
        /// <summary>
        /// Specifies whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Response headers, false otherwise</param>
        IAuditMiddlewareConfigurator IncludeResponseHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Response headers should be included on the audit output</param>
        IAuditMiddlewareConfigurator IncludeResponseHeaders(Func<HttpContext, bool> includePredicate);
        /// <summary>
        /// Specifies whether the HTTP Request headers should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Request headers, false otherwise</param>
        IAuditMiddlewareConfigurator IncludeHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Request headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Request headers should be included on the audit output</param>
        IAuditMiddlewareConfigurator IncludeHeaders(Func<HttpContext, bool> includePredicate);
        /// <summary>
        /// Specifies whether the request body should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the request body, false otherwise</param>
        IAuditMiddlewareConfigurator IncludeRequestBody(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the request body should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the request body should be included on the audit output</param>
        IAuditMiddlewareConfigurator IncludeRequestBody(Func<HttpContext, bool> includePredicate);
        /// <summary>
        /// Specifies the event type name to use.
        /// </summary>
        /// <param name="eventTypeName">The event type name to use. The following placeholders can be used as part of the string: 
        /// - {url}: replaced with the requst URL.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditMiddlewareConfigurator WithEventType(string eventTypeName);
        /// <summary>
        /// Specifies a predicate to determine the event type name on the audit output.
        /// </summary>
        /// <param name="eventTypeNameBuilder">A function of the executing context to determine the event type name. The following placeholders can be used as part of the string: 
        /// - {url}: replaced with the requst URL.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditMiddlewareConfigurator WithEventType(Func<HttpContext, string> eventTypeNameBuilder);
        /// <summary>
        /// Specifies whether the response body should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the response body, false otherwise</param>
        IAuditMiddlewareConfigurator IncludeResponseBody(bool include = true);

        /// <summary>
        /// Specifies a predicate to determine whether the response body should be included on the audit output.
        /// The predicate is evaluated before request execution.
        /// </summary>
        /// <param name="includePredicate">A function of the executed context to determine whether the response body should be included on the audit output.
        /// This predicate is evaluated before request execution.</param>
        IAuditMiddlewareConfigurator IncludeResponseBody(Func<HttpContext, bool> includePredicate);

        /// <summary>
        /// Specifies a predicate to determine whether the response body content should be skipped or included in the audit output.
        /// The predicate is evaluated after request execution.
        /// </summary>
        /// <param name="skipPredicate">A function of the executed context to determine whether the response body content should be skipped or included in the audit output.
        /// This predicate is evaluated after request execution.</param>
        IAuditMiddlewareConfigurator SkipResponseBodyContent(Func<HttpContext, bool> skipPredicate);

        /// <summary>
        /// Specifies whether the response body content should be skipped or included in the audit output.
        /// </summary>
        /// <param name="skip">A boolean to determine whether the response body should be skipped or included in the audit output.</param>
        IAuditMiddlewareConfigurator SkipResponseBodyContent(bool skip);
    }
}
#endif