using Audit.Core;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Firestore.Providers
{
    /// <summary>
    /// Google Cloud Firestore data provider
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - ProjectId: The Google Cloud project ID
    /// - Database: The Firestore database name (default: "(default)")
    /// - Collection: The Firestore collection name
    /// - CredentialsFilePath: Path to the credentials JSON file (optional)
    /// - CredentialsJson: JSON string with credentials (optional)
    /// - FirestoreDbFactory: Custom FirestoreDb factory (optional)
    /// - IdBuilder: Function to generate document IDs (optional)
    /// - SanitizeFieldNames: Whether to fix field names with dots (default: false)
    /// </remarks>
    public class FirestoreDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Gets or sets the Google Cloud project ID.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the Firestore database name. Default is "(default)".
        /// </summary>
        public string Database { get; set; } = "(default)";

        /// <summary>
        /// Gets or sets the Firestore collection name.
        /// </summary>
        public Setting<string> Collection { get; set; } = "AuditEvents";

        /// <summary>
        /// Gets or sets the path to the credentials JSON file.
        /// </summary>
        public string CredentialsFilePath { get; set; }

        /// <summary>
        /// Gets or sets the credentials JSON string.
        /// </summary>
        public string CredentialsJson { get; set; }

        /// <summary>
        /// Gets or sets a custom FirestoreDb factory to use.
        /// </summary>
        public Func<FirestoreDb> FirestoreDbFactory { get; set; }

        /// <summary>
        /// Gets or sets a function that returns the document ID to use for a given audit event.
        /// By default, it will generate a new document ID automatically.
        /// </summary>
        public Func<AuditEvent, string> IdBuilder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to sanitize field names by replacing dots with underscores.
        /// Default is false.
        /// </summary>
        public bool SanitizeFieldNames { get; set; }

        private Lazy<FirestoreDb> _firestoreDb;

        public FirestoreDataProvider()
        {
            InitializeFirestoreDb();
        }

        public FirestoreDataProvider(FirestoreDb firestoreDb)
        {
            FirestoreDbFactory = () => firestoreDb;

            InitializeFirestoreDb();
        }

        public FirestoreDataProvider(Action<ConfigurationApi.IFirestoreProviderConfigurator> config)
        {
            if (config != null)
            {
                var firestoreConfig = new ConfigurationApi.FirestoreProviderConfigurator();
                config.Invoke(firestoreConfig);
                ProjectId = firestoreConfig._projectId;
                Database = firestoreConfig._database ?? "(default)";
                Collection = firestoreConfig._collection;
                CredentialsFilePath = firestoreConfig._credentialsFilePath;
                CredentialsJson = firestoreConfig._credentialsJson;
                FirestoreDbFactory = firestoreConfig._firestoreDbFactory;
                IdBuilder = firestoreConfig._idBuilder;
                SanitizeFieldNames = firestoreConfig._sanitizeFieldNames;
            }

            InitializeFirestoreDb();
        }

        private void InitializeFirestoreDb()
        {
            _firestoreDb = new Lazy<FirestoreDb>(CreateFirestoreDb, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private FirestoreDb CreateFirestoreDb()
        {
            if (FirestoreDbFactory != null)
            {
                return FirestoreDbFactory.Invoke();
            }

            if (string.IsNullOrEmpty(ProjectId))
            {
                throw new InvalidOperationException("Project ID is required for Firestore connection");
            }

            FirestoreDbBuilder builder;
            var database = Database ?? "(default)";

            if (!string.IsNullOrEmpty(CredentialsFilePath))
            {
                builder = new FirestoreDbBuilder
                {
                    ProjectId = ProjectId,
                    DatabaseId = database,
                    CredentialsPath = CredentialsFilePath
                };
            }
            else if (!string.IsNullOrEmpty(CredentialsJson))
            {
                builder = new FirestoreDbBuilder
                {
                    ProjectId = ProjectId,
                    DatabaseId = database,
                    JsonCredentials = CredentialsJson
                };
            }
            else
            {
                builder = new FirestoreDbBuilder
                {
                    ProjectId = ProjectId,
                    DatabaseId = database
                };
            }

            return builder.Build();
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);
            var id = GetDocumentId(auditEvent);

            if (string.IsNullOrEmpty(id))
            {
                // Let Firestore generate the ID
                var docRef = collection.AddAsync(documentData).GetAwaiter().GetResult();
                return docRef.Id;
            }
            else
            {
                // Use the specified ID
                var docRef = collection.Document(id);
                docRef.SetAsync(documentData).GetAwaiter().GetResult();
                return id;
            }
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);
            var id = GetDocumentId(auditEvent);

            if (string.IsNullOrEmpty(id))
            {
                // Let Firestore generate the ID
                var docRef = await collection.AddAsync(documentData, cancellationToken);
                return docRef.Id;
            }
            else
            {
                // Use the specified ID
                var docRef = collection.Document(id);
                await docRef.SetAsync(documentData, null, cancellationToken);
                return id;
            }
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var id = eventId.ToString();

            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);

            var docRef = collection.Document(id);
            docRef.SetAsync(documentData, SetOptions.MergeAll).GetAwaiter().GetResult();
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var id = eventId.ToString();

            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);

            var docRef = collection.Document(id);
            await docRef.SetAsync(documentData, SetOptions.MergeAll, cancellationToken);
        }

        public override T GetEvent<T>(object eventId)
        {
            var id = eventId.ToString();

            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);

            var docRef = collection.Document(id);
            var snapshot = docRef.GetSnapshotAsync().GetAwaiter().GetResult();

            if (!snapshot.Exists)
            {
                return null;
            }

            var json = Configuration.JsonAdapter.Serialize(snapshot.ToDictionary());
            return Configuration.JsonAdapter.Deserialize<T>(json);
        }

        public override async Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default)
        {
            var id = eventId.ToString();

            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);

            var docRef = collection.Document(id);
            var snapshot = await docRef.GetSnapshotAsync(cancellationToken);

            if (!snapshot.Exists)
            {
                return null;
            }

            var json = Configuration.JsonAdapter.Serialize(snapshot.ToDictionary());
            return Configuration.JsonAdapter.Deserialize<T>(json);
        }
        
        /// <summary>
        /// Queries events with specific filters applied to Firestore query.
        /// </summary>
        /// <param name="queryBuilder">The query builder to apply. Example: q => q.WhereEqualTo("EventType", "Login")</param>
        public IAsyncEnumerable<AuditEvent> QueryEventsAsync(Func<Query, Query> queryBuilder = null)
        {
            return QueryEventsAsync<AuditEvent>(queryBuilder);
        }

        /// <summary>
        /// Queries events with specific filters applied to Firestore query.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="queryBuilder">The where clause to apply. Example: q => q.WhereEqualTo("EventType", "Login")</param>
        public async IAsyncEnumerable<T> QueryEventsAsync<T>(Func<Query, Query> queryBuilder = null) where T : AuditEvent
        {
            var db = GetFirestoreDb();
            Query query = GetCollection(db, null);

            if (queryBuilder != null)
            {
                query = queryBuilder.Invoke(query);
            }

            var snapshots = await query.GetSnapshotAsync();

            foreach (var doc in snapshots.Documents)
            {
                var json = Configuration.JsonAdapter.Serialize(doc.ToDictionary());
                var auditEvent = Configuration.JsonAdapter.Deserialize<T>(json);

                yield return auditEvent;
            }
        }

        /// <summary>
        /// Gets the native Firestore collection reference.
        /// </summary>
        public CollectionReference GetFirestoreCollection()
        {
            var db = GetFirestoreDb();
            return GetCollection(db, null);
        }

        /// <summary>
        /// Gets the FirestoreDb instance used by this provider.
        /// </summary>
        public FirestoreDb GetFirestoreDb()
        {
            return _firestoreDb?.Value;
        }

        private CollectionReference GetCollection(FirestoreDb db, AuditEvent auditEvent)
        {
            var collectionName = Collection.GetValue(auditEvent) ?? "AuditEvents";

            return db.Collection(collectionName);
        }

        internal string GetDocumentId(AuditEvent auditEvent)
        {
            return IdBuilder?.Invoke(auditEvent);
        }

        internal Dictionary<string, object> ConvertToFirestoreData(AuditEvent auditEvent)
        {
            // Serialize to JSON first, then deserialize to Dictionary to ensure proper conversion
            var json = Configuration.JsonAdapter.Serialize(auditEvent);
            var data = Configuration.JsonAdapter.Deserialize<Dictionary<string, object>>(json);
            
            data = SanitizeDictionary(data);

            if (SanitizeFieldNames)
            {
                data = FixFieldNames(data);
            }

            // Add server timestamp
            data["_timestamp"] = FieldValue.ServerTimestamp;

            return data;
        }

        internal Dictionary<string, object> SanitizeDictionary(Dictionary<string, object> data)
        {
            if (data == null)
            {
                return null;
            }

            var sanitized = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                sanitized[kvp.Key] = SanitizeValue(kvp.Value);
            }

            return sanitized;
        }

        private object SanitizeValue(object value)
        {
            return value switch
            {
                JsonElement jElement => SanitizeValue(jElement),
                Dictionary<string, object> dict => SanitizeDictionary(dict),
                List<object> list => list.Select(SanitizeValue).ToList(),
                object[] array => array.Select(SanitizeValue).ToList(),
                _ => value
            };
        }

        internal object SanitizeValue(JsonElement jElement)
        {
            switch (jElement.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in jElement.EnumerateObject())
                    {
                        dict[prop.Name] = SanitizeValue(prop.Value);
                    }
                    return dict;
                case JsonValueKind.Array:
                    return jElement.EnumerateArray().Select(SanitizeValue).ToList();
                case JsonValueKind.String:
                    return jElement.GetString();
                case JsonValueKind.Number:
                    if (jElement.TryGetInt64(out var l))
                        return l;
                    return jElement.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }

        internal Dictionary<string, object> FixFieldNames(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                var key = FixFieldName(kvp.Key);
                var value = kvp.Value;

                value = value switch
                {
                    Dictionary<string, object> nestedDict => FixFieldNames(nestedDict),
                    List<object> list => FixFieldNamesInList(list),
                    object[] array => FixFieldNamesInList(array.ToList()),
                    _ => value
                };
                
                result[key] = value;
            }

            return result;
        }

        internal List<object> FixFieldNamesInList(List<object> list)
        {
            var result = new List<object>();

            foreach (var item in list)
            {
                switch (item)
                {
                    case Dictionary<string, object> dict:
                        result.Add(FixFieldNames(dict));
                        break;
                    case List<object> nestedList:
                        result.Add(FixFieldNamesInList(nestedList));
                        break;
                    case object[] array:
                        result.Add(FixFieldNamesInList(array.ToList()));
                        break;
                    default:
                        result.Add(item);
                        break;
                }
            }

            return result;
        }

        internal static string FixFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return fieldName;
            }

            // Replace dots with underscores
            var fixedName = fieldName.Replace('.', '_');

            // Ensure field name doesn't start with '__' (reserved by Firestore)
            while (fixedName.StartsWith("__"))
            {
                fixedName = "_" + fixedName.Substring(2);
            }

            return fixedName;
        }

        /// <summary>
        /// Tests the connection to Firestore by attempting to list collections.
        /// </summary>
        public async Task TestConnectionAsync()
        {
            var db = GetFirestoreDb();
            await db.ListRootCollectionsAsync().Take(1).ToListAsync();
            // If we get here without exception, the connection is working
        }
    }
} 