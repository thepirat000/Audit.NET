using Audit.Core;
using System;
using System.Net.Http;

namespace Audit.Http.ConfigurationApi
{
    public interface IAuditClientHandlerConfigurator
    {
        /// <summary>
        /// Specifies a filter function to determine the events to log depending on the request. By default all events are logged.
        /// </summary>
        /// <param name="requestPredicate">Return true to include the request on the audit output, or false otherwise.</param>
        IAuditClientHandlerConfigurator FilterByRequest(Func<HttpRequestMessage, bool> requestPredicate);
        /// <summary>
        /// Specifies a filter function to determine the events to log depending on the response. By default all events are logged.
        /// </summary>
        /// <param name="responsePredicate">Return true to include the response on the audit output, or false otherwise.</param>
        IAuditClientHandlerConfigurator FilterByResponse(Func<HttpResponseMessage, bool> responsePredicate);
        /// <summary>
        /// Specifies whether the HTTP Request headers should be included on the audit output. 
        /// </summary>
        /// <param name="include">True to include the HTTP Request headers, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeRequestHeaders(bool include = true);
        /// <summary>
        /// Specifies whether the request body should be included on the audit output. 
        /// </summary>
        /// <param name="include">True to include the request body, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeRequestBody(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the request body should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the request body should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeRequestBody(Func<HttpRequestMessage, bool> includePredicate);
        /// <summary>
        /// Specifies whether the HTTP Content headers should be included on the audit output. 
        /// </summary>
        /// <param name="include">True to include the HTTP Content headers, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeContentHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Content headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Content headers should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeContentHeaders(Func<HttpRequestMessage, bool> includePredicate);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Request headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Request headers should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeRequestHeaders(Func<HttpRequestMessage, bool> includePredicate);
        /// <summary>
        /// Specifies whether the response body should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the response body, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeResponseBody(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the response body should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the response body should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeResponseBody(Func<HttpResponseMessage, bool> includePredicate);
        /// <summary>
        /// Specifies whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Response headers, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeResponseHeaders(bool include = true);
        /// <summary>
        /// Specifies a predicate to determine whether the HTTP Response headers should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the executing context to determine whether the HTTP Response headers should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeResponseHeaders(Func<HttpResponseMessage, bool> includePredicate);

        /// <summary>
        /// Specifies which HTTP Request Options should be included on the audit output.
        /// </summary>
        /// <param name="includePredicate">A function of the option key to determine which HTTP Request Options should be included on the audit output</param>
        IAuditClientHandlerConfigurator IncludeOptions(Func<string, bool> includePredicate);

        /// <summary>
        /// Specifies whether the HTTP Request Options should be included on the audit output.
        /// </summary>
        /// <param name="include">True to include the HTTP Request Options, false otherwise</param>
        IAuditClientHandlerConfigurator IncludeOptions(bool include = true);
        
        /// <summary>
        /// Specifies a predicate to determine the event type name on the audit output.
        /// </summary>
        /// <param name="eventTypeNamePredicate">A function of the executing context to determine the event type name. The following placeholders can be used as part of the string: 
        /// - {url}: replaced with the requst URL.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditClientHandlerConfigurator EventType(Func<HttpRequestMessage, string> eventTypeNamePredicate);
        /// <summary>
        /// Specifies the event type name to use.
        /// </summary>
        /// <param name="eventTypeName">The event type name to use. The following placeholders can be used as part of the string: 
        /// - {url}: replaced with the requst URL.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </param>
        IAuditClientHandlerConfigurator EventType(string eventTypeName);
        /// <summary>
        /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
        /// </summary>
        /// <param name="eventCreationPolicy">The creation policy to use</param>
        IAuditClientHandlerConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy);
        /// <summary>
        /// Specifies the audit data provider to use. Default is NULL to use the globally configured data provider.
        /// </summary>
        IAuditClientHandlerConfigurator AuditDataProvider(AuditDataProvider auditDataProvider);
        /// <summary>
        /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
        /// </summary>
        IAuditClientHandlerConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);
    }
}
