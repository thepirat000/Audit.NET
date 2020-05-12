#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Mvc.Filters;
#else
using ActionExecutingContext = System.Web.Http.Controllers.HttpActionContext;
using ActionExecutedContext = System.Web.Http.Filters.HttpActionExecutedContext;
#endif
using System;

namespace Audit.WebApi.ConfigurationApi
{
    public interface IAuditApiGlobalConfigurator
    {
        /// <summary>
        /// Specifies whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Response headers, false otherwise</param>
        IAuditApiGlobalConfigurator IncludeResponseHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Response headers should be included on the audit output</param>
        IAuditApiGlobalConfigurator IncludeResponseHeaders(Func<ActionExecutedContext, bool> includePredicate);
        /// <summary>
        /// Specifies whether the HTTP Request headers should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Request headers, false otherwise</param>
        IAuditApiGlobalConfigurator IncludeHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Request headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Request headers should be included on the audit output</param>
        IAuditApiGlobalConfigurator IncludeHeaders(Func<ActionExecutingContext, bool> includePredicate);
        /// <summary>
        /// Specifies whether the request body should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the request body, false otherwise</param>
        IAuditApiGlobalConfigurator IncludeRequestBody(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the request body should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the request body should be included on the audit output</param>
        IAuditApiGlobalConfigurator IncludeRequestBody(Func<ActionExecutingContext, bool> includePredicate);
        /// <summary>
        /// Specifies the event type name to use.
        /// </summary>
        /// <param name="eventTypeName">The event type name to use. The following placeholders can be used as part of the string: 
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditApiGlobalConfigurator WithEventType(string eventTypeName);
        /// <summary>
        /// Specifies a predicate to determine the event type name on the audit output.
        /// </summary>
        /// <param name="eventTypeNamePredicate">A function of the executing context to determine the event type name. The following placeholders can be used as part of the string: 
        /// - {controller}: replaced with the controller name.
        /// - {action}: replaced with the action name.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditApiGlobalConfigurator WithEventType(Func<ActionExecutingContext, string> eventTypeNamePredicate);
        /// <summary>
        /// Specifies whether the model state should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the model state, false otherwise</param>
        IAuditApiGlobalConfigurator IncludeModelState(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the model state should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executed context to determine whether the model state should be included on the audit output</param>
        IAuditApiGlobalConfigurator IncludeModelState(Func<ActionExecutedContext, bool> includePredicate);
        /// <summary>
        /// Specifies whether the response body should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the response body, false otherwise</param>
        IAuditApiGlobalConfigurator IncludeResponseBody(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the response body should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executed context to determine whether the response body should be included on the audit output</param>
        IAuditApiGlobalConfigurator IncludeResponseBody(Func<ActionExecutedContext, bool> includePredicate);
    }

}
