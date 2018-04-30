using System;
using Audit.Core;
using System.Threading.Tasks;
using Nest;
using Newtonsoft.Json;

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
    ///     TypeNameBuilder: A func that returns the type name to use for an AuditEvent.
    /// </remarks>
    public class ElasticsearchDataProvider : AuditDataProvider
    {
        private readonly Lazy<IElasticClient> _client;

        /// <summary>
        /// The Elasticsearch connection settings to use. 
        /// The recommendation is to return an instance of AuditConnectionSettings in order to use the proper Audit Event serializer.
        /// </summary>
        public IConnectionSettingsValues ConnectionSettings { get; set; }

        /// <summary>
        /// The Elasticsearch index to use when saving an audit event. Must be lowercase. NULL (or Func that returns NULL) to use the default global index.
        /// </summary>
        public Func<AuditEvent, IndexName> IndexBuilder { get; set; }

        /// <summary>
        /// The Elasticsearch type name to use when saving an audit event. NULL (or Func that returns NULL) to use the index name.
        /// </summary>
        /// <remarks>
        /// https://www.elastic.co/guide/en/elasticsearch/reference/master/removal-of-types.html
        /// </remarks>
        [Obsolete("Mapping types will be completely removed in Elasticsearch 7.0.0.")]
        public Func<AuditEvent, TypeName> TypeNameBuilder { get; set; }

        /// <summary>
        /// The Elasticsearch document id to use when savint an audit event
        /// </summary>
        public Func<AuditEvent, Id> IdBuilder { get; set; }

        internal ElasticsearchDataProvider(IElasticClient client)
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

        public override object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            return JsonConvert.SerializeObject(value, Core.Configuration.JsonSettings);
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new CreateRequest<AuditEvent>(auditEvent, IndexBuilder?.Invoke(auditEvent), TypeNameBuilder?.Invoke(auditEvent), id);
            var response = _client.Value.Create(createRequest);
            if (response.IsValid && response.Result != Result.Error)
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index, Type = response.Type };
            }
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
            return "/";
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var id = IdBuilder?.Invoke(auditEvent);
            var createRequest = new CreateRequest<AuditEvent>(auditEvent, IndexBuilder?.Invoke(auditEvent), TypeNameBuilder?.Invoke(auditEvent), id);
            var response = await _client.Value.CreateAsync(createRequest);
            if (response.IsValid && response.Result != Result.Error)
            {
                return new ElasticsearchAuditEventId() { Id = response.Id, Index = response.Index, Type = response.Type };
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
            var indexRequest = new IndexRequest<AuditEvent>(auditEvent, el.Index, el.Type, el.Id);
            var response = _client.Value.Index(indexRequest);
            if (response.OriginalException != null)
            {
                throw response.OriginalException;
            }
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            var el = eventId as ElasticsearchAuditEventId;
            var indexRequest = new IndexRequest<AuditEvent>(auditEvent, el.Index, el.Type, el.Id);
            var response = await _client.Value.IndexAsync(indexRequest);
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
            var request = new GetRequest(eventId.Index, eventId.Type, eventId.Id);
            var response = _client.Value.Get(new DocumentPath<T>(eventId.Id), x => x.Index(eventId.Index).Type(eventId.Type));
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

        public override async Task<T> GetEventAsync<T>(object eventId)
        {
            var el = eventId as ElasticsearchAuditEventId ?? new ElasticsearchAuditEventId() { Id = eventId.ToString() };
            return await GetEventAsync<T>(el);
        }

        public async Task<T> GetEventAsync<T>(ElasticsearchAuditEventId eventId) where T : AuditEvent
        {
            var request = new GetRequest(eventId.Index, eventId.Type, eventId.Id);
            var response = await _client.Value.GetAsync(new DocumentPath<T>(eventId.Id), x => x.Index(eventId.Index).Type(eventId.Type));
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
