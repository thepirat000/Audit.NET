﻿using Audit.Core;
using Audit.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Http
{
    /// <summary>
    /// Handler to intercept calls to an HttpClient and generate audit logs
    /// </summary>
    public class AuditHttpClientHandler : DelegatingHandler
    {
        private ConfigurationApi.AuditClientHandlerConfigurator _config = new ConfigurationApi.AuditClientHandlerConfigurator();
        /// <summary>
        /// Sets a filter function to determine the events to log depending on the request. By default all events are logged.
        /// </summary>
        public Func<HttpRequestMessage, bool> RequestFilter { set => _config._requestFilter = value; }
        /// <summary>
        /// Sets a filter function to determine the events to log depending on the response. By default all events are logged.
        /// </summary>
        public Func<HttpResponseMessage, bool> ResponseFilter { set => _config._responseFilter = value; }
        /// <summary>
        /// Specifies whether the HTTP Content headers should be included on the audit output. Default is false.
        /// </summary>
        public bool IncludeContentHeaders { set => _config._includeContentHeaders = _ => value; }
        /// <summary>
        /// Specifies whether the HTTP Request headers should be included on the audit output. Default is false.
        /// </summary>
        public bool IncludeRequestHeaders { set => _config._includeRequestHeaders = _ => value; }
        /// <summary>
        /// Specifies whether the HTTP Response headers should be included on the audit output. Default is false.
        /// </summary>
        public bool IncludeResponseHeaders { set => _config._includeResponseHeaders = _ => value; }
        /// <summary>
        /// Specifies whether the request body should be included on the audit output. Default is false. 
        /// </summary>
        public bool IncludeRequestBody { set => _config._includeRequestBody = _ => value; }
        /// <summary>
        /// Specifies whether the response body should be included on the audit output. Default is false. 
        /// </summary>
        public bool IncludeResponseBody { set => _config._includeResponseBody = _ => value; }
        /// <summary>
        /// Specifies whether the request options should be included on the audit output based on a predicate. Default is false.
        /// </summary>
        public Func<string, bool> IncludeOptions { set => _config._includeOptions = value; }
        /// <summary>
        /// Specifies the event type name to use.
        /// The following placeholders can be used as part of the string: 
        /// - {url}: replaced with the requst URL.
        /// - {verb}: replaced with the HTTP verb used (GET, POST, etc).
        /// </summary>
        public string EventTypeName { set => _config._eventTypeName = _ => value; }
        /// <summary>
        /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
        /// </summary>
        public EventCreationPolicy? CreationPolicy { set => _config._eventCreationPolicy = value; }
        /// <summary>
        /// Specifies the audit data provider to use. Default is NULL to use the globally configured data provider.
        /// </summary>
        public IAuditDataProvider AuditDataProvider { set => _config._auditDataProvider = value; }
        /// <summary>
        /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
        /// </summary>
        public IAuditScopeFactory AuditScopeFactory { set => _config._auditScopeFactory = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditHttpClientHandler"/> class with a default HttpClientHandler as the Inner Handler.
        /// </summary>
        public AuditHttpClientHandler()
            : base(new HttpClientHandler())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditHttpClientHandler"/> class with a default HttpClientHandler as the Inner Handler.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public AuditHttpClientHandler(Action<ConfigurationApi.IAuditClientHandlerConfigurator> config)
            : base(new HttpClientHandler())
        {
            if (config != null)
            {
                config.Invoke(_config);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditHttpClientHandler"/> class with the given Inner Handler.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="innerHandler">The Inner Handler.</param>
        public AuditHttpClientHandler(Action<ConfigurationApi.IAuditClientHandlerConfigurator> config, HttpMessageHandler innerHandler)
        {
            if (innerHandler != null)
            {
                InnerHandler = innerHandler;
            }
            if (config != null)
            {
                config.Invoke(_config);
            }
        }
        
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Configuration.AuditDisabled
                || (_config._requestFilter != null && !_config._requestFilter.Invoke(request)))
            {
                // audit disabled or filtered out by request
                return await base.SendAsync(request, cancellationToken);
            }
            var eventType = (_config._eventTypeName?.Invoke(request) ?? "{verb} {url}")
                .Replace("{verb}", request.Method.Method)
                .Replace("{url}", request.RequestUri.ToString());

            var action = new HttpAction()
            {
                Method = request.Method.Method,
                Url = request.RequestUri.ToString(),
                Version = request.Version?.ToString(),
                Request = await GetRequestAudit(request, cancellationToken)
            };

            action.SetRequestMessage(request);
            
            var auditEvent = new AuditEventHttpClient() { Action = action };
            var options = new AuditScopeOptions()
            {
                EventType = eventType,
                AuditEvent = auditEvent,
                CreationPolicy = _config._eventCreationPolicy,
                DataProvider = _config._auditDataProvider
            };
            HttpResponseMessage response;
            var auditScopeFactory = _config._auditScopeFactory ?? Configuration.AuditScopeFactory;
            var scope = await auditScopeFactory.CreateAsync(options, cancellationToken);
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                action.Exception = ex.GetExceptionInfo();
                await SaveDispose(scope, cancellationToken);
                throw;
            }
            if (_config._responseFilter != null && !_config._responseFilter.Invoke(response))
            {
                // response filtered out, discard the event
                scope.Discard();
            }
            else
            {
                // Update the response and save
                action.Response = await GetResponseAudit(response, cancellationToken);
                action.SetResponseMessage(response);
                scope.EventAs<AuditEventHttpClient>().Action = action;
                await SaveDispose(scope, cancellationToken);
            }
            return response;
        }

        private async Task SaveDispose(IAuditScope scope, CancellationToken cancellationToken)
        {
            if (scope.EventCreationPolicy == Core.EventCreationPolicy.Manual)
            {
                await scope.SaveAsync(cancellationToken);
            }
            await scope.DisposeAsync();
        }

        private async Task<Request> GetRequestAudit(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestInfo = new Request()
            {
                Scheme = request.RequestUri.Scheme,
                Path = request.RequestUri.AbsolutePath,
                QueryString = request.RequestUri.Query,
                Headers = _config._includeRequestHeaders != null && _config._includeRequestHeaders.Invoke(request)
                            ? GetHeaders(request.Headers)
                            : null,
                Content = await GetRequestContent(request, cancellationToken),
                Options = GetOptions(request)
            };
            return requestInfo;
        }

        private Dictionary<string, object> GetOptions(HttpRequestMessage request)
        {
            if (_config._includeOptions == null)
            {
                return null;
            }
            var result = new Dictionary<string, object>();
#if NET6_0_OR_GREATER
            foreach (var option in request.Options)
            {
                if (_config._includeOptions.Invoke(option.Key))
                {
                    result[option.Key] = option.Value;
                }
            }
#else
            foreach (var prop in request.Properties)
            {
                if (_config._includeOptions.Invoke(prop.Key))
                {
                    result[prop.Key] = prop.Value;
                }
            }
#endif
            return result.Count == 0 ? null : result;
        }

        private async Task<Response> GetResponseAudit(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var responseInfo = new Response()
            {
                Status = response.StatusCode.ToString(),
                StatusCode = (int)response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                Reason = response.ReasonPhrase,
                Headers = _config._includeResponseHeaders != null && _config._includeResponseHeaders.Invoke(response)
                    ? GetHeaders(response.Headers)
                    : null,
                Content = await GetResponseContent(response, cancellationToken)
            };
            return responseInfo;
        }

        private async Task<Content> GetRequestContent(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = new Content()
            {
                Body = _config._includeRequestBody != null && _config._includeRequestBody.Invoke(request)
                            ? await GetContentBody(request.Content, cancellationToken)
                            : null,
                Headers = _config._includeContentHeaders != null && _config._includeContentHeaders.Invoke(request)
                    ? GetHeaders(request.Content?.Headers)
                    : null
            };
            return content.Body == null && content.Headers == null ? null : content;
        }

        private async Task<Content> GetResponseContent(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var content = new Content()
            {
                Body = _config._includeResponseBody != null && _config._includeResponseBody.Invoke(response)
                    ? await GetContentBody(response.Content, cancellationToken)
                    : null,
                Headers = _config._includeContentHeaders != null && _config._includeContentHeaders.Invoke(response.RequestMessage)
                    ? GetHeaders(response.Content?.Headers)
                    : null
            };
            return content.Body == null && content.Headers == null ? null : content;
        }

        private async Task<object> GetContentBody(System.Net.Http.HttpContent content, CancellationToken cancellationToken)
        {
            if (content == null)
            {
                return null;
            }
#if NET6_0_OR_GREATER
            return await content.ReadAsStringAsync(cancellationToken);
#else
            return await content.ReadAsStringAsync();
#endif
        }

        private Dictionary<string, string> GetHeaders(HttpHeaders headers)
        {
            return headers?.ToDictionary(k => k.Key, v => string.Join(", ", v.Value));
        }
    }
}
