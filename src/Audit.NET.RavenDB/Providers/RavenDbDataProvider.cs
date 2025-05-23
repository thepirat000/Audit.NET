﻿using Audit.Core;
using Audit.RavenDB.ConfigurationApi;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using Audit.JsonNewtonsoftAdapter;

namespace Audit.RavenDB.Providers
{
    /// <summary>
    /// Data provider for persisting Audit Events as documents into a Raven DB 
    /// </summary>
    [CLSCompliant(false)]
    public class RavenDbDataProvider : AuditDataProvider
    {
        private readonly Setting<string> _databaseName;

        /// <summary>
        /// Json default settings
        /// </summary>
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// The Raven Document Store
        /// </summary>
        public IDocumentStore DocumentStore { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenDbDataProvider"/> class using a custom Document Store instance.
        /// </summary>
        /// <param name="documentStore">The document store.</param>
        /// <param name="databaseFunc">The function to obtain the database name from the audit event.</param>
        public RavenDbDataProvider(IDocumentStore documentStore, Func<AuditEvent, string> databaseFunc = null)
        {
            _databaseName = databaseFunc;
            DocumentStore = documentStore;
            DocumentStore.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenDbDataProvider"/> class using the given configuration.
        /// </summary>
        /// <param name="config">The RavenDB configuration fluent API.</param>
        public RavenDbDataProvider(Action<IRavenDbProviderConfigurator> config)
        {
            var ravenConfig = new RavenDbProviderConfigurator();
            config.Invoke(ravenConfig);

            _databaseName = ravenConfig._storeConfig._database;

            if (ravenConfig._documentStore == null)
            {
                DocumentStore = new DocumentStore()
                {
                    Certificate = ravenConfig._storeConfig._certificate, 
                    Urls = ravenConfig._storeConfig._urls, 
                    Database = ravenConfig._storeConfig._databaseDefault
                };
                ((NewtonsoftJsonSerializationConventions)DocumentStore.Conventions.Serialization)
                    .JsonContractResolver = new AuditContractResolver();
            }
            else
            {
                DocumentStore = ravenConfig._documentStore;
            }

            DocumentStore.Initialize();
        }

        public override object CloneValue<T>(T value, AuditEvent auditEvent)
        {
            if (value is null)
            {
                return null;
            }
            if (value is string)
            {
                return value;
            }
            if (Configuration.JsonAdapter is Audit.Core.JsonNewtonsoftAdapter adapter)
            {
                // The adapter is Newtonsoft, use the adapter
                return adapter.Deserialize(adapter.Serialize(value), value.GetType());
            }
            // Default to use Newtonsoft directly
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value, JsonSerializerSettings), value.GetType(), JsonSerializerSettings);
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            using (var session = DocumentStore.OpenSession(GetDatabaseName(auditEvent)))
            {
                session.Store(auditEvent);
                session.SaveChanges();

                return session.Advanced.GetDocumentId(auditEvent);
            }
        }

        /// <summary>
        /// Insert an event to the data source returning the event id generated
        /// </summary>
        /// <param name="auditEvent">The audit event being inserted.</param>
        /// <returns></returns>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            using (var session = DocumentStore.OpenAsyncSession(GetDatabaseName(auditEvent)))
            {
                await session.StoreAsync(auditEvent, cancellationToken);
                await session.SaveChangesAsync(cancellationToken);

                return session.Advanced.GetDocumentId(auditEvent);
            }
        }

        /// <summary>
        /// Retrieves a saved audit event from its id. Override this method to provide a way to access the audit events by id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventId">The event id being retrieved.</param>
        /// <returns></returns>
        public override T GetEvent<T>(object eventId)
        {
            using (var session = DocumentStore.OpenSession(GetDatabaseName()))
            {
                var auditEvent = session.Load<T>(eventId.ToString());
                return auditEvent;
            }
        }

        /// <summary>
        /// Asynchronously retrieves a saved audit event from its id. Override this method
        /// to provide a way to access the audit events by id.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventId">The event id being retrieved.</param>
        /// <returns></returns>
        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            using (var session = DocumentStore.OpenAsyncSession(GetDatabaseName()))
            {
                var auditEvent = await session.LoadAsync<T>(eventId.ToString(), cancellationToken);
                return auditEvent;
            }
        }

        /// <summary>
        /// Saves the specified audit event. Triggered when the scope is saved. Override
        /// this method to replace the specified audit event on the data source.
        /// </summary>
        /// <param name="eventId">The event id being replaced.</param>
        /// <param name="auditEvent">The audit event.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            using (var session = DocumentStore.OpenSession(GetDatabaseName(auditEvent)))
            {
                session.Store(auditEvent, eventId.ToString());
                session.SaveChanges();
            }
        }

        /// <summary>
        /// Saves the specified audit event. Triggered when the scope is saved. Override
        /// this method to replace the specified audit event on the data source.
        /// </summary>
        /// <param name="eventId">The event id being replaced.</param>
        /// <param name="auditEvent">The audit event.</param>
        /// <returns></returns>
        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            using (var session = DocumentStore.OpenAsyncSession(GetDatabaseName(auditEvent)))
            {
                await session.StoreAsync(auditEvent, eventId.ToString(), cancellationToken);
                await session.SaveChangesAsync(cancellationToken);
            }
        }

        internal string GetDatabaseName(AuditEvent auditEvent = null)
        {
            return _databaseName.GetValue(auditEvent);
        }
    }
}
