using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.QLDB.Driver;
using Amazon.QLDBSession.Model;
using Audit.AmazonQLDB.ConfigurationApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Newtonsoft.Json;

namespace Audit.AmazonQLDB.Providers
{
    /// <summary>
    /// Amazon QLDB data provider for Audit.NET. Store the audit events into Amazon QLDB tables.
    /// </summary>
    public class AmazonQldbDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Top-level attributes to be added to the event and document before saving.
        /// </summary>
        public Dictionary<string, Func<AuditEvent, object>> CustomAttributes { get; set; } = new Dictionary<string, Func<AuditEvent, object>>();

        ///// <summary>
        ///// Factory that creates the QLDB Driver.
        ///// </summary>
        public Lazy<IAsyncQldbDriver> QldbDriver { get; set; }

        /// <summary>
        /// The table name to use when saving an audit event in the QLDB table. 
        /// </summary>
        public Setting<string> TableName { get; set; }

        /// <summary>
        /// Gets or sets the JSON serializer settings.
        /// </summary>
        public JsonSerializerSettings JsonSettings { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        /// <summary>
        /// Creates a new AmazonQLDB data provider using the given driver.
        /// </summary>
        /// <param name="driver">The Amazon QLDB driver instance.</param>
        public AmazonQldbDataProvider(IAsyncQldbDriver driver)
        {
            QldbDriver = new Lazy<IAsyncQldbDriver>(() => driver);
        }

        /// <summary>
        /// Creates a new AmazonQLDB data provider using the given driver.
        /// </summary>
        /// <param name="driver">The Amazon QLDB driver instance.</param>
        public AmazonQldbDataProvider(AsyncQldbDriver driver)
        {
            QldbDriver = new Lazy<IAsyncQldbDriver>(() => driver);
        }

        /// <summary>
        /// Creates a new AmazonQLDB data provider.
        /// </summary>
        public AmazonQldbDataProvider()
        {
        }

        /// <summary>
        /// Creates a new AmazonQLDB data provider with the given configuration options.
        /// </summary>
        public AmazonQldbDataProvider(Action<IAmazonQldbProviderConfigurator> config)
        {
            var amazonQldbProviderConfigurator = new AmazonQldbProviderConfigurator();
            if (config != null)
            {
                config.Invoke(amazonQldbProviderConfigurator);
                TableName = amazonQldbProviderConfigurator._tableConfigurator._tableName;
                CustomAttributes = amazonQldbProviderConfigurator._tableConfigurator._attrConfigurator?._attributes;
            }
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
            return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value, JsonSettings), value.GetType(), JsonSettings);
        }

        /// <summary>
        /// Inserts an event into AmazonQLDB
        /// </summary>
        public override object InsertEvent(AuditEvent auditEvent) => InsertEventAsync(auditEvent).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously inserts an event into AmazonQLDB
        /// </summary>
        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var driver = QldbDriver.Value;
            var tableName = GetTableName(auditEvent);
            IIonValue inserted = null;
            await driver.Execute(async txn =>
            {
                var json = JsonConvert.SerializeObject(auditEvent, JsonSettings);
                var insertInto = $@"INSERT INTO {tableName} VALUE ?";
                try
                {
                    inserted = await (await txn.Execute(insertInto, IonLoader.Default.Load(json))).FirstAsync(cancellationToken);
                }
                catch (BadRequestException e) when (e.Message.Contains($"No such variable named '{tableName}'"))
                {
                    await txn.Execute($"CREATE TABLE {tableName}");
                    inserted = await(await txn.Execute(insertInto, IonLoader.Default.Load(json))).FirstAsync(cancellationToken);
                }
            }, cancellationToken);

            var insertDocumentId = inserted?.GetField("documentId").StringValue;
            return (insertDocumentId, tableName);
        }

        /// <summary>
        /// Replaces an event into AmazonQLDB
        /// </summary>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent) => ReplaceEventAsync(eventId, auditEvent).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously replaces an event into AmazonQLDB
        /// </summary>
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            var driver = QldbDriver.Value;
            var (insertDocumentId, tableName) = (ValueTuple<string, string>)eventId;
            return driver.Execute(trx => trx.Execute(
                $@"UPDATE {tableName} AS e BY eid
                      SET e = ?
                      WHERE eid = ?",
                IonLoader.Default.Load(JsonConvert.SerializeObject(auditEvent, JsonSettings)), new ValueFactory().NewString(insertDocumentId)), cancellationToken);
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a AmazonQLDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        public override T GetEvent<T>(object eventId) => GetFromQldb<T>(eventId).GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a AmazonQLDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        /// <param name="cancellationToken">The Cancellation Token.</param>
        public override Task<T> GetEventAsync<T>(object eventId, CancellationToken cancellationToken = default) => GetFromQldb<T>(eventId, cancellationToken);

        private async Task<T> GetFromQldb<T>(object eventId, CancellationToken cancellationToken = default) where T : AuditEvent
        {
            var driver = QldbDriver.Value;
            var (insertDocumentId, tableName) = (ValueTuple<string, string>)eventId;
            IIonValue selectedEvent = null;
            await driver.Execute(async trx =>
            {
                selectedEvent = await (await trx.Execute(
                    $@"SELECT e.*
                      FROM {tableName} AS e BY eid                      
                      WHERE eid = ?",
                    new ValueFactory().NewString(insertDocumentId))).FirstAsync(cancellationToken);
            }, cancellationToken);
            var json = selectedEvent!.ToPrettyString();
            var selectedAuditEvent = JsonConvert.DeserializeObject<T>(json, JsonSettings);
            return selectedAuditEvent;
        }

        private string GetTableName(AuditEvent auditEvent)
        {
            return TableName.GetValue(auditEvent) ?? auditEvent.GetType().Name;
        }
    }
}
