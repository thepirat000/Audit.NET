using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private static readonly string[] ReservedPartiQLWords = {
            "ABSOLUTE",
            "ACTION",
            "ADD",
            "ALL",
            "ALLOCATE",
            "ALTER",
            "AND",
            "ANY",
            "ARE",
            "AS",
            "ASC",
            "ASSERTION",
            "AT",
            "AUTHORIZATION",
            "AVG",
            "BAG",
            "BEGIN",
            "BETWEEN",
            "BIT",
            "BIT_LENGTH",
            "BLOB",
            "BOOL",
            "BOOLEAN",
            "BOTH",
            "BY",
            "CASCADE",
            "CASCADED",
            "CASE",
            "CAST",
            "CATALOG",
            "CHAR",
            "CHARACTER",
            "CHARACTER_LENGTH",
            "CHAR_LENGTH",
            "CHECK",
            "CLOB",
            "CLOSE",
            "COALESCE",
            "COLLATE",
            "COLLATION",
            "COLUMN",
            "COMMIT",
            "CONNECT",
            "CONNECTION",
            "CONSTRAINT",
            "CONSTRAINTS",
            "CONTINUE",
            "CONVERT",
            "CORRESPONDING",
            "COUNT",
            "CREATE",
            "CROSS",
            "CURRENT",
            "CURRENT_DATE",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_USER",
            "CURSOR",
            "DATE",
            "DATE_ADD",
            "DATE_DIFF",
            "DAY",
            "DEALLOCATE",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DEFERRABLE",
            "DEFERRED",
            "DELETE",
            "DESC",
            "DESCRIBE",
            "DESCRIPTOR",
            "DIAGNOSTICS",
            "DISCONNECT",
            "DISTINCT",
            "DOMAIN",
            "DOUBLE",
            "DROP",
            "ELSE",
            "END",
            "END-EXEC",
            "ESCAPE",
            "EXCEPT",
            "EXCEPTION",
            "EXEC",
            "EXECUTE",
            "EXISTS",
            "EXTERNAL",
            "EXTRACT",
            "FALSE",
            "FETCH",
            "FIRST",
            "FLOAT",
            "FOR",
            "FOREIGN",
            "FOUND",
            "FROM",
            "FULL",
            "GET",
            "GLOBAL",
            "GO",
            "GOTO",
            "GRANT",
            "GROUP",
            "HAVING",
            "HOUR",
            "IDENTITY",
            "IMMEDIATE",
            "IN",
            "INDEX",
            "INDICATOR",
            "INITIALLY",
            "INNER",
            "INPUT",
            "INSENSITIVE",
            "INSERT",
            "INT",
            "INTEGER",
            "INTERSECT",
            "INTERVAL",
            "INTO",
            "IS",
            "ISOLATION",
            "JOIN",
            "KEY",
            "LANGUAGE",
            "LAST",
            "LEADING",
            "LEFT",
            "LEVEL",
            "LIKE",
            "LIMIT",
            "LIST",
            "LOCAL",
            "LOWER",
            "MATCH",
            "MAX",
            "MIN",
            "MINUTE",
            "MISSING",
            "MODULE",
            "MONTH",
            "NAMES",
            "NATIONAL",
            "NATURAL",
            "NCHAR",
            "NEXT",
            "NO",
            "NOT",
            "NULL",
            "NULLIF",
            "NUMERIC",
            "OCTET_LENGTH",
            "OF",
            "ON",
            "ONLY",
            "OPEN",
            "OPTION",
            "OR",
            "ORDER",
            "OUTER",
            "OUTPUT",
            "OVERLAPS",
            "PAD",
            "PARTIAL",
            "PIVOT",
            "POSITION",
            "PRECISION",
            "PREPARE",
            "PRESERVE",
            "PRIMARY",
            "PRIOR",
            "PRIVILEGES",
            "PROCEDURE",
            "PUBLIC",
            "READ",
            "REAL",
            "REFERENCES",
            "RELATIVE",
            "REMOVE",
            "RESTRICT",
            "REVOKE",
            "RIGHT",
            "ROLLBACK",
            "ROWS",
            "SCHEMA",
            "SCROLL",
            "SECOND",
            "SECTION",
            "SELECT",
            "SESSION",
            "SESSION_USER",
            "SET",
            "SEXP",
            "SIZE",
            "SMALLINT",
            "SOME",
            "SPACE",
            "SQL",
            "SQLCODE",
            "SQLERROR",
            "SQLSTATE",
            "STRING",
            "STRUCT",
            "SUBSTRING",
            "SUM",
            "SYMBOL",
            "SYSTEM_USER",
            "TABLE",
            "TEMPORARY",
            "THEN",
            "TIME",
            "TIMESTAMP",
            "TIMEZONE_HOUR",
            "TIMEZONE_MINUTE",
            "TO",
            "TO_STRING",
            "TO_TIMESTAMP",
            "TRAILING",
            "TRANSACTION",
            "TRANSLATE",
            "TRANSLATION",
            "TRIM",
            "TRUE",
            "TUPLE",
            "TXID",
            "UNDROP",
            "UNION",
            "UNIQUE",
            "UNKNOWN",
            "UNPIVOT",
            "UPDATE",
            "UPPER",
            "USAGE",
            "USER",
            "USING",
            "UTCNOW",
            "VALUE",
            "VALUES",
            "VARCHAR",
            "VARYING",
            "VIEW",
            "WHEN",
            "WHENEVER",
            "WHERE",
            "WITH",
            "WORK",
            "WRITE",
            "YEAR",
            "ZONE"
        };

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
            var tableName = GetTable(auditEvent);

            List<IIonValue> insertResult = null;
            try
            {
                insertResult = driver.Execute(txn =>
                {
                    var json = auditEvent.ToJson();
                    var insertInto = $@"INSERT INTO {tableName} VALUE ?";
                    try
                    {
                        return Insert();
                    }
                    catch (BadRequestException e) when (e.Message.Contains($"No such variable named '{tableName}'"))
                    {
                        txn.Execute($"CREATE TABLE {tableName}");
                        return Insert();
                    }

                    List<IIonValue> Insert()
                    {
                        var result = txn.Execute(insertInto, IonLoader.Default.Load(json));
                        return result.ToList();
                    }
                });
            }
            catch (BadRequestException e) when (e.Message.Contains("The Ledger with name") && e.Message.Contains("is not found"))
            {
                throw new InvalidOperationException(
                    $"{nameof(AmazonQldbDataProvider)} restriction is the Ledger must exist. To create a Ledger database either use the AWS console or the AmazonQLDBClient.CreateLedgerAsync from the `AWS SDK for .NET` packages https://github.com/aws/aws-sdk-net/",
                    e);
            }

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

        private string GetTable(AuditEvent auditEvent)
        {
            var tableName = TableNameBuilder?.Invoke(auditEvent);
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = auditEvent?.GetType().Name;
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = "AuditEvent";
            }

            // sanitize the table names as per Amazon table naming constraints https://docs.aws.amazon.com/qldb/latest/developerguide/ql-reference.create-table.html
            // escape reserved words
            foreach (var reservedWord in ReservedPartiQLWords)
            {
                var isReserved = tableName.Equals(reservedWord, StringComparison.InvariantCultureIgnoreCase);
                if (isReserved)
                {
                    tableName = "_" + tableName;
                }
            }

            // remove non alphanumerics so catches cases like Order:Update are transformed to OrderUpdate
            var alphaNumeric = Array.FindAll(tableName.ToCharArray(),
                c => (char.IsLetterOrDigit(c) || c == '_') && 48 < c && c < 123); // only digits, letters and underscore
            tableName = new string(alphaNumeric);

            // ensure first letter is not a digit
            if (!char.IsLetter(tableName[0]))
            {
                tableName = "_" + tableName;
            }

            // get max 128
            var maxAllowedCharacters = 128;
            if (tableName.Length >= maxAllowedCharacters)
            {
                tableName = tableName.Substring(0, maxAllowedCharacters);
            }

            return tableName;
        }
    }
}
