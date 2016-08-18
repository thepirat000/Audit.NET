using Audit.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Audit.MongoDB.Providers
{
    /// <summary>
    /// Mongo DB data access
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - AuditConnectionString: Mongo connection string
    /// - AuditEventDatabase: Database name
    /// - AuditEventTable: Collection name
    /// </remarks>
    public class MongoDataProvider : AuditDataProvider
    {
        private string _connectionString = "mongodb://localhost:27017";
        private string _database = "Audit";
        private string _collection = "Event";
        private bool _shouldTestConnection = true;

        static MongoDataProvider()
        {
            ConfigureBsonMapping();
        }

        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }

        public string Collection
        {
            get { return _collection; }
            set { _collection = value; }
        }

        public bool ShouldTestConnection
        {
            get { return _shouldTestConnection; }
            set { _shouldTestConnection = value; }
        }

        private static void ConfigureBsonMapping()
        {
            var pack = new ConventionPack();
            pack.Add(new IgnoreIfNullConvention(true));
            ConventionRegistry.Register("Ignore null properties for AuditEvent", pack, type => type == typeof (AuditEvent));

            BsonClassMap.RegisterClassMap<AuditEvent>(cm =>
            {
                cm.AutoMap();
                cm.MapExtraElementsMember(c => c.CustomFields);
            });

            BsonClassMap.RegisterClassMap<AuditTarget>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(x => x.SerializedOld).SetElementName("Old");
                cm.MapProperty(x => x.SerializerNew).SetElementName("New");
            });
        }

        public override void WriteEvent(AuditEvent auditEvent)
        {
            var db = GetDatabase();
            var col = db.GetCollection<AuditEvent>(_collection);
            col.InsertOne(auditEvent);
        }

        public override object Serialize<T>(T value)
        {
            // if can be converted to bsonvalue, return the value
            try
            {
                BsonValue bsonValue;
                if (BsonTypeMapper.TryMapToBsonValue(value, out bsonValue))
                {
                    return value;
                }
            }
            catch
            {
                // ignored. TryMapToBsonValue can throw exception (i.e. when the type is an array of objects that cannot be mapped to a bsonvalue)
            }
            return value.ToBsonDocument(typeof(object));
        }

        public override bool TestConnection()
        {
            if (!_shouldTestConnection)
            {
                return true;
            }
            try
            {
                var db = GetDatabase();
                var test = db.RunCommand((Command<BsonDocument>)"{ping:1}");
                return test["ok"].ToInt64() == 1;
            }
            catch
            {
                return false;                
            }
        }

        private IMongoDatabase GetDatabase()
        {
            var client = new MongoClient(_connectionString);
            var db = client.GetDatabase(_database);
            return db;
        }
    }
}