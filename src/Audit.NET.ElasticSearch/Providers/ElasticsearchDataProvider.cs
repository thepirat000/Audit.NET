using System;
using System.Threading;
using Audit.Core;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;

namespace Audit.Elasticsearch.Providers
{
    /// <summary>
    /// Elasticsearch data access
    /// </summary>
    /// <remarks>
    /// Settings:
    ///     ConnectionSettingsBuilder: A func that returns the connection settings for an AuditEvent. return an instance of AuditConnectionSettings in order to use the proper Audit Event serializer.
    ///     IdBuilder: A func that returns the ID to use for an AuditEvent.
    ///     IndexBuilder: A func that returns the Index Name to use for an AuditEvent.
    /// </remarks>
    public class ElasticsearchDataProvider : AuditDataProvider
    {
        private Lazy<ElasticsearchClient> _client;

        /// <summary>
        /// Gets or sets the settings to use when creating the Elasticsearch client
        /// </summary>
        public IElasticsearchClientSettings Settings { get; set; } = new ElasticsearchClientSettings();

        /// <summary>
        /// Gets or sets the Elasticsearch index to use when saving an audit event. Must be lowercase. NULL to use the default global index.
        /// </summary>
        public Setting<IndexName> Index { get; set; }

        /// <summary>
        /// Gets or sets the function that returns the document ID to use for an AuditEvent.
        /// </summary>
        public Func<AuditEvent, Id> IdBuilder { get; set; }

        /// <summary>
        /// Creates an instance of ElasticsearchDataProvider
        /// </summary>
        public ElasticsearchDataProvider()
        {
        }
        
        /// <summary>
        /// Creates an instance of ElasticsearchDataProvider with the given Elasticsearch client.
        /// </summary>
        /// <param name="client">The Elasticsearch client to use</param>
        public ElasticsearchDataProvider(ElasticsearchClient client)
        {
            _client = new Lazy<ElasticsearchClient>(() => client);
        }
        
        /// <summary>
        /// Creates an instance of ElasticsearchDataProvider with the given configuration.
        /// </summary>
        public ElasticsearchDataProvider(Action<Configuration.IElasticsearchProviderConfigurator> config)
        {
            var elConfig = new Configuration.ElasticsearchProviderConfigurator();
            
            if (config != null)
            {
                config.Invoke(elConfig);
                if (elConfig._client != null)
                {
                    _client = new Lazy<ElasticsearchClient>(() => elConfig._client);
                }
                Settings = elConfig._clientSettings;
                IdBuilder = elConfig._idBuilder;
                Index = elConfig._index;
            }
        }

        public ElasticsearchClient GetClient()
        {
            if (_client != null)
            {
                return _client.Value;
            }
            
            _client = new Lazy<ElasticsearchClient>(() => new ElasticsearchClient(Settings));

            return _client.Value;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = GetClient().IndexAsync(createRequest).ConfigureAwait(false).GetAwaiter().GetResult();
            
            if (response.IsValidResponse && (response.Result == Result.Created || response.Result == Result.Updated))
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index };
            }
            
            if (response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
            
            return "/";
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = await GetClient().IndexAsync(createRequest, cancellationToken);
            
            if (response.IsValidResponse && (response.Result == Result.Created || response.Result == Result.Updated))
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index };
            }
            
            if (response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
            
            return "/";
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var el = eventId as ElasticsearchAuditEventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = GetClient().IndexAsync(indexRequest).ConfigureAwait(false).GetAwaiter().GetResult();
            
            if (!response.IsValidResponse && response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var el = eventId as ElasticsearchAuditEventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = await GetClient().IndexAsync(indexRequest, cancellationToken);
            
            if (!response.IsValidResponse && response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
        }

        public override T GetEvent<T>(object eventId)
        {
            var el = eventId as ElasticsearchAuditEventId ?? new ElasticsearchAuditEventId() { Id = eventId.ToString() };
            
            return GetEvent<T>(el);
        }

        public T GetEvent<T>(ElasticsearchAuditEventId eventId) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Id);
            var response = GetClient().GetAsync<T>(request).ConfigureAwait(false).GetAwaiter().GetResult();
            
            if (response.IsValidResponse && response.Found)
            {
                return response.Source;
            }
            
            if (response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
            
            return default;
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var el = eventId as ElasticsearchAuditEventId ?? new ElasticsearchAuditEventId() { Id = eventId.ToString() };
            
            return await GetEventAsync<T>(el, cancellationToken);
        }

        public async Task<T> GetEventAsync<T>(ElasticsearchAuditEventId eventId, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Id);
            var response = await GetClient().GetAsync<T>(request, cancellationToken);
            
            if (response.IsValidResponse && response.Found)
            {
                return response.Source;
            }
            
            if (response.TryGetOriginalException(out var exception))
            {
                throw exception;
            }
            
            return default;
        }
    }
}
