using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IonDotnet.Builders;
using Amazon.IonDotnet.Tree;
using Amazon.IonDotnet.Tree.Impl;
using Amazon.QLDB.Driver;
using Amazon.QLDBSession.Model;
using Audit.Core;
using Audit.NET.AmazonQLDB.ConfigurationApi;

namespace Audit.NET.AmazonQLDB.Providers
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
        public Lazy<IQldbDriver> QldbDriver { get; set; }

        /// <summary>
        /// The table name to use when saving an audit event in the QLDB table. 
        /// </summary>
        public Func<AuditEvent, string> TableNameBuilder { get; set; }

        /// <summary>
        /// Creates a new AmazonQLDB data provider using the given driver.
        /// </summary>
        /// <param name="driver">The Amazon QLDB driver instance.</param>
        public AmazonQldbDataProvider(IQldbDriver driver)
        {
            QldbDriver = new Lazy<IQldbDriver>(() => driver);
        }

        /// <summary>
        /// Creates a new AmazonQLDB data provider using the given driver.
        /// </summary>
        /// <param name="driver">The Amazon QLDB driver instance.</param>
        public AmazonQldbDataProvider(QldbDriver driver)
        {
            QldbDriver = new Lazy<IQldbDriver>(() => driver);
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
                TableNameBuilder = amazonQldbProviderConfigurator._tableConfigurator?._tableNameBuilder;
                CustomAttributes = amazonQldbProviderConfigurator._tableConfigurator?._attrConfigurator?._attributes;
            }
        }

        /// <summary>
        /// Inserts an event into AmazonQLDB
        /// </summary>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var driver = QldbDriver.Value;
            var tableName = GetTableName(auditEvent);

            List<IIonValue> insertResult = driver.Execute(txn =>
            {
                var json = auditEvent.ToJson();
                var insertInto = $@"INSERT INTO {tableName} VALUE ?";
                try
                {
                    return txn.Execute(insertInto, IonLoader.Default.Load(json)).ToList();
                }
                catch (BadRequestException e) when (e.Message.Contains($"No such variable named '{tableName}'"))
                {
                    txn.Execute($"CREATE TABLE {tableName}");
                    return txn.Execute(insertInto, IonLoader.Default.Load(json)).ToList();
                }
            });

            var insertDocumentId = insertResult.First().GetField("documentId").StringValue;
            return (insertDocumentId, tableName);
        }

        /// <summary>
        /// Asynchronously inserts an event into AmazonQLDB
        /// </summary>
        public override Task<object> InsertEventAsync(AuditEvent auditEvent)
        {
            var result = InsertEvent(auditEvent);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Replaces an event into AmazonQLDB
        /// </summary>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            var driver = QldbDriver.Value;
            var (insertDocumentId, tableName) = (ValueTuple<string, string>)eventId;
            driver.Execute(trx => trx.Execute(
                $@"UPDATE {tableName} AS e BY eid
                      SET e = ?
                      WHERE eid = ?",
                IonLoader.Default.Load(auditEvent.ToJson()), new ValueFactory().NewString(insertDocumentId)));
        }

        /// <summary>
        /// Asynchronously replaces an event into AmazonQLDB
        /// </summary>
        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent)
        {
            ReplaceEvent(eventId, auditEvent);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a AmazonQLDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        public override T GetEvent<T>(object eventId) => GetFromQldb<T>(eventId);

        /// <summary>
        /// Asynchronously gets an audit event from its primary key
        /// </summary>
        /// <typeparam name="T">The audit event type</typeparam>
        /// <param name="eventId">The event ID to retrieve. 
        /// Must be a Primitive, a AmazonQLDBEntry or an array of any of these two types. The first (or only) element must be the Hash key, and the second element is the range key.
        /// </param>
        public override Task<T> GetEventAsync<T>(object eventId) => Task.FromResult(GetFromQldb<T>(eventId));

        private T GetFromQldb<T>(object eventId) where T : AuditEvent
        {
            var driver = QldbDriver.Value;
            var (insertDocumentId, tableName) = (ValueTuple<string, string>)eventId;
            IResult selectResult = null;
            driver.Execute(trx =>
            {
                selectResult = trx.Execute(
                    $@"SELECT e.*
                      FROM {tableName} AS e BY eid                      
                      WHERE eid = ?",
                    new ValueFactory().NewString(insertDocumentId));
            });

            var selectedEvent = selectResult.First();
            var selectedAuditEvent = AuditEvent.FromJson<T>(selectedEvent.ToPrettyString());
            return selectedAuditEvent;
        }

        private string GetTableName(AuditEvent auditEvent)
        {
            return TableNameBuilder?.Invoke(auditEvent) ?? auditEvent.GetType().Name;
        }
    }
}
