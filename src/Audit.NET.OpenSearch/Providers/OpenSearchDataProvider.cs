using System;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;

using OpenSearch.Client;

namespace Audit.OpenSearch.Providers
{
    /// <summary>
    /// OpenSearch data access
    /// </summary>
    /// <remarks>
    /// Settings:
    ///     ConnectionSettingsBuilder: A func that returns the connection settings for an AuditEvent. return an instance of AuditConnectionSettings in order to use the proper Audit Event serializer.
    ///     IdBuilder: A func that returns the ID to use for an AuditEvent.
    ///     IndexBuilder: A func that returns the Index Name to use for an AuditEvent.
    /// </remarks>
    public class OpenSearchDataProvider : AuditDataProvider
    {
        private Lazy<OpenSearchClient> _client;

        /// <summary>
        /// Gets or sets the settings to use when creating the OpenSearch client
        /// </summary>
        public IConnectionSettingsValues ClientSettings { get; set; } = new ConnectionSettings();

        /// <summary>
        /// Gets or sets the OpenSearch index to use when saving an audit event. Must be lowercase. NULL to use the default global index.
        /// </summary>
        public Setting<IndexName> Index { get; set; }

        /// <summary>
        /// Gets or sets the function that returns the document ID to use for an AuditEvent.
        /// </summary>
        public Func<AuditEvent, Id> IdBuilder { get; set; }

        /// <summary>
        /// Creates an instance of OpenSearchDataProvider
        /// </summary>
        public OpenSearchDataProvider()
        {
        }
        
        /// <summary>
        /// Creates an instance of OpenSearchDataProvider with the given OpenSearch client.
        /// </summary>
        /// <param name="client">The OpenSearch client to use</param>
        public OpenSearchDataProvider(OpenSearchClient client)
        {
            _client = new Lazy<OpenSearchClient>(() => client);
        }
        
        /// <summary>
        /// Creates an instance of OpenSearchDataProvider with the given configuration.
        /// </summary>
        public OpenSearchDataProvider(Action<Configuration.IOpenSearchProviderConfigurator> config)
        {
            var elConfig = new Configuration.OpenSearchProviderConfigurator();
            
            if (config != null)
            {
                config.Invoke(elConfig);
                if (elConfig._client != null)
                {
                    _client = new Lazy<OpenSearchClient>(() => elConfig._client);
                }
                ClientSettings = elConfig._clientSettings;
                IdBuilder = elConfig._idBuilder;
                Index = elConfig._index;
            }
        }

        public OpenSearchClient GetClient()
        {
            if (_client != null)
            {
                return _client.Value;
            }
            
            _client = new Lazy<OpenSearchClient>(() => new OpenSearchClient(ClientSettings));

            return _client.Value;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = GetClient().IndexAsync(createRequest).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response.IsValid && (response.Result == Result.Created || response.Result == Result.Updated))
            {
                return new OpenSearchAuditEventId() { Id = response.Id, Index = response.Index };
            }

            ThrowIfApiCallFailed(response);
            
            return "/";
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = await GetClient().IndexAsync(createRequest, cancellationToken);

            if (response.IsValid && response.Result is Result.Created or Result.Updated)
            {
                return new OpenSearchAuditEventId() { Id = response.Id, Index = response.Index };
            }

            ThrowIfApiCallFailed(response);

            return "/";
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var el = (OpenSearchAuditEventId)eventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = GetClient().IndexAsync(indexRequest).ConfigureAwait(false).GetAwaiter().GetResult();
            
            ThrowIfApiCallFailed(response);
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var el = (OpenSearchAuditEventId)eventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = await GetClient().IndexAsync(indexRequest, cancellationToken);
            
            ThrowIfApiCallFailed(response);
        }

        public override T GetEvent<T>(object eventId)
        {
            var el = eventId as OpenSearchAuditEventId ?? new OpenSearchAuditEventId() { Id = eventId.ToString() };
            
            return GetEvent<T>(el);
        }

        public T GetEvent<T>(OpenSearchAuditEventId eventId) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Id);
            var response = GetClient().GetAsync<T>(request).ConfigureAwait(false).GetAwaiter().GetResult();
            
            if (response.IsValid && response.Found)
            {
                return response.Source;
            }

            ThrowIfApiCallFailed(response);

            return default;
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var el = eventId as OpenSearchAuditEventId ?? new OpenSearchAuditEventId() { Id = eventId.ToString() };
            
            return await GetEventAsync<T>(el, cancellationToken);
        }

        public async Task<T> GetEventAsync<T>(OpenSearchAuditEventId eventId, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Id);
            var response = await GetClient().GetAsync<T>(request, cancellationToken);
            
            if (response.IsValid && response.Found)
            {
                return response.Source;
            }

            ThrowIfApiCallFailed(response);

            return default;
        }

        private static void ThrowIfApiCallFailed(ResponseBase response)
        {
            if (response.ApiCall?.OriginalException is not null)
            {
                throw response.ApiCall.OriginalException;
            }
        }
    }
}
