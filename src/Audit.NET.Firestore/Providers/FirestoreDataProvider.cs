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
    /// - FirestoreDb: Custom FirestoreDb instance (optional)
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
            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);
            var id = eventId?.ToString() ?? GetDocumentId(auditEvent);

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Event ID cannot be null or empty for replace operation");
            }

            var docRef = collection.Document(id);
            docRef.SetAsync(documentData, SetOptions.MergeAll).GetAwaiter().GetResult();
        }

        public override async Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, auditEvent);
            var documentData = ConvertToFirestoreData(auditEvent);
            var id = eventId?.ToString() ?? GetDocumentId(auditEvent);

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Event ID cannot be null or empty for replace operation");
            }

            var docRef = collection.Document(id);
            await docRef.SetAsync(documentData, SetOptions.MergeAll, cancellationToken);
        }

        public override T GetEvent<T>(object eventId)
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);
            var id = eventId?.ToString();

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Event ID cannot be null or empty");
            }

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
            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);
            var id = eventId?.ToString();

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Event ID cannot be null or empty");
            }

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
        /// Returns an IQueryable that enables querying against the audit events stored in Firestore.
        /// Note: This creates an in-memory query after fetching all documents. For large collections,
        /// consider using QueryEvents with filters instead.
        /// </summary>
        public IQueryable<AuditEvent> QueryEvents()
        {
            return QueryEvents<AuditEvent>();
        }

        /// <summary>
        /// Returns an IQueryable that enables querying against the audit events stored in Firestore.
        /// Note: This creates an in-memory query after fetching all documents. For large collections,
        /// consider using QueryEvents with filters instead.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        public IQueryable<T> QueryEvents<T>() where T : AuditEvent
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);
            var snapshots = collection.GetSnapshotAsync().GetAwaiter().GetResult();

            var events = new List<T>();
            foreach (var doc in snapshots.Documents)
            {
                var json = Configuration.JsonAdapter.Serialize(doc.ToDictionary());
                var auditEvent = Configuration.JsonAdapter.Deserialize<T>(json);
                events.Add(auditEvent);
            }

            return events.AsQueryable();
        }

        /// <summary>
        /// Queries events with specific filters applied to Firestore query.
        /// </summary>
        /// <param name="whereClause">The where clause to apply. Example: q => q.WhereEqualTo("EventType", "Login")</param>
        public async Task<IList<AuditEvent>> QueryEventsAsync(Func<Query, Query> whereClause = null)
        {
            return await QueryEventsAsync<AuditEvent>(whereClause);
        }

        /// <summary>
        /// Queries events with specific filters applied to Firestore query.
        /// </summary>
        /// <typeparam name="T">The AuditEvent type</typeparam>
        /// <param name="whereClause">The where clause to apply. Example: q => q.WhereEqualTo("EventType", "Login")</param>
        public async Task<IList<T>> QueryEventsAsync<T>(Func<Query, Query> whereClause = null) where T : AuditEvent
        {
            var db = GetFirestoreDb();
            var collection = GetCollection(db, null);
            Query query = collection;

            if (whereClause != null)
            {
                query = whereClause(query);
            }

            var snapshots = await query.GetSnapshotAsync();
            var events = new List<T>();

            foreach (var doc in snapshots.Documents)
            {
                var json = Configuration.JsonAdapter.Serialize(doc.ToDictionary());
                var auditEvent = Configuration.JsonAdapter.Deserialize<T>(json);
                events.Add(auditEvent);
            }

            return events;
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

        private string GetDocumentId(AuditEvent auditEvent)
        {
            if (IdBuilder != null)
            {
                return IdBuilder(auditEvent);
            }

            return null; // Let Firestore generate the ID
        }

        private Dictionary<string, object> ConvertToFirestoreData(AuditEvent auditEvent)
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

        private Dictionary<string, object> SanitizeDictionary(Dictionary<string, object> data)
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
            if (value is JsonElement jElement)
            {
                return SanitizeValue(jElement);
            }
            if (value is Dictionary<string, object> dict)
            {
                return SanitizeDictionary(dict);
            }
            if (value is List<object> list)
            {
                return list.Select(SanitizeValue).ToList();
            }
            if (value is object[] array)
            {
                return array.Select(SanitizeValue).ToList();
            }
            return value;
        }

        private object SanitizeValue(JsonElement jElement)
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
                    return null;
                case JsonValueKind.Undefined:
                default:
                    return null;
            }
        }

        private Dictionary<string, object> FixFieldNames(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in data)
            {
                var key = FixFieldName(kvp.Key);
                var value = kvp.Value;

                if (value is Dictionary<string, object> nestedDict)
                {
                    value = FixFieldNames(nestedDict);
                }
                else if (value is List<object> list)
                {
                    value = FixFieldNamesInList(list);
                }
                else if (value is object[] array)
                {
                    value = FixFieldNamesInList(array.ToList());
                }

                result[key] = value;
            }

            return result;
        }

        private List<object> FixFieldNamesInList(List<object> list)
        {
            var result = new List<object>();

            foreach (var item in list)
            {
                if (item is Dictionary<string, object> dict)
                {
                    result.Add(FixFieldNames(dict));
                }
                else if (item is List<object> nestedList)
                {
                    result.Add(FixFieldNamesInList(nestedList));
                }
                else if (item is object[] array)
                {
                    result.Add(FixFieldNamesInList(array.ToList()));
                }
                else
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private string FixFieldName(string fieldName)
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