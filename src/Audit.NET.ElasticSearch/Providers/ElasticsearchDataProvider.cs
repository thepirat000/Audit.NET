using System;
using System.Threading;
using Audit.Core;
using System.Threading.Tasks;
using Nest;

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
        private readonly Lazy<IElasticClient> _client;

        /// <summary>
        /// The Elasticsearch NEST client
        /// </summary>
        public IElasticClient Client => _client.Value;

        /// <summary>
        /// The Elasticsearch connection settings to use. 
        /// </summary>
        public IConnectionSettingsValues ConnectionSettings { get; set; }

        /// <summary>
        /// The Elasticsearch index to use when saving an audit event. Must be lowercase. NULL to use the default global index.
        /// </summary>
        public Setting<IndexName> Index { get; set; }

        /// <summary>
        /// The Elasticsearch document id to use when saving an audit event
        /// </summary>
        public Func<AuditEvent, Id> IdBuilder { get; set; }

        public ElasticsearchDataProvider(IElasticClient client)
        {
            _client = new Lazy<IElasticClient>(() => client);
        }

        /// <summary>
        /// Creates an instance of ElasticsearchDataProvider with the given audit connection settings.
        /// </summary>
        public ElasticsearchDataProvider()
        {
            _client = new Lazy<IElasticClient>(() => new ElasticClient(ConnectionSettings));
        }

        /// <summary>
        /// Creates an instance of ElasticsearchDataProvider with the given configuration.
        /// </summary>
        public ElasticsearchDataProvider(Action<Configuration.IElasticsearchProviderConfigurator> config)
        {
            _client = new Lazy<IElasticClient>(() => new ElasticClient(ConnectionSettings));
            var elConfig = new Configuration.ElasticsearchProviderConfigurator();
            if (config != null)
            {
                config.Invoke(elConfig);
                ConnectionSettings = elConfig._connectionSettings;
                IdBuilder = elConfig._idBuilder;
                Index = elConfig._index;
            }
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = Client.Index(createRequest);
            if (response.IsValid && response.Result != Result.Error)
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index };
            }
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            return "/";
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new IndexRequest<object>(auditEvent, Index.GetValue(auditEvent), id);
            var response = await Client.IndexAsync(createRequest, cancellationToken);
            if (response.IsValid && response.Result != Result.Error)
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index };
            }
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            return "/";
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var el = eventId as ElasticsearchAuditEventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = Client.Index(indexRequest);
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var el = eventId as ElasticsearchAuditEventId;
            var indexRequest = new IndexRequest<object>(auditEvent, el.Index, el.Id);
            var response = await Client.IndexAsync(indexRequest, cancellationToken);
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
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
            var response = Client.Get(new DocumentPath<T>(eventId.Id), x => x.Index(eventId.Index));
            if (response.IsValid && response.Found)
            {
                return response.Source;
            }
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            return default(T);
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var el = eventId as ElasticsearchAuditEventId ?? new ElasticsearchAuditEventId() { Id = eventId.ToString() };
            return await GetEventAsync<T>(el, cancellationToken);
        }

        public async Task<T> GetEventAsync<T>(ElasticsearchAuditEventId eventId, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Id);
            var response = await Client.GetAsync(new DocumentPath<T>(eventId.Id), x => x.Index(eventId.Index), cancellationToken);
            if (response.IsValid && response.Found)
            {
                return response.Source;
            }
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            return default(T);
        }
    }
}
