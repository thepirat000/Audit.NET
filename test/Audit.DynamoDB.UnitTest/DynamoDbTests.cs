using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Audit.Core;
using Audit.DynamoDB.Providers;
using NUnit.Framework;

namespace Audit.DynamoDB.UnitTest
{
    public class DynamoDbTests
    {
        [Test]
        [Category("Integration")]
        [Category("Dynamo")]
        public void TestStressDynamo()
        {
            int N = 32;
            SetDynamoSettings();
            var hashes = new HashSet<string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                count++;
                hashes.Add((scope.EventId as object[])[0].ToString());
            });

            var rnd = new Random();

            //Parallel random insert into event1, event2 and event3 containers
            Parallel.ForEach(Enumerable.Range(1, N), i =>
            {
                var eventType = "AuditEvents";
                var x = "start";
                using (var s = new AuditScopeFactory().Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
                {
                    x = "end";
                }
            });

            Assert.AreEqual(N, hashes.Count);
            Assert.AreEqual(N * 2, count);

            // Assert events
            int maxCheck = N / 4;
            int check = 0;
            foreach (var hash in hashes)
            {
                if (check++ > maxCheck)
                {
                    break;
                }
                var ddp = (Audit.Core.Configuration.DataProvider as DynamoDataProvider);
                var ev = ddp.GetEvent<AuditEvent>((Primitive)hash, (Primitive)DateTime.Now.Year);

                Assert.NotNull(ev);
                Assert.AreEqual("AuditEvents", ev.EventType);
                Assert.AreEqual(DateTime.Now.Year.ToString(), ev.CustomFields["SortKey"].ToString());
                Assert.AreEqual(hash, ev.CustomFields["HashKey"].ToString());
            }
        }


        public void SetDynamoSettings()
        {
            var url = "http://localhost:8000";
            var tableName = "AuditEvents";
            CreateDynamoTable(tableName).GetAwaiter().GetResult();

            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(url)
                    .Table(tableName)
                    .SetAttribute("HashKey", ev => Guid.NewGuid())
                    .SetAttribute("SortKey", ev => ev.StartDate.Year))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                .ResetActions();
        }

        private async Task CreateDynamoTable(string tableName)
        {
            AmazonDynamoDBConfig ddbConfig = new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8000"
            };
            var client = new AmazonDynamoDBClient(ddbConfig);
            try
            {
                await client.DeleteTableAsync(tableName);
            }
            catch
#pragma warning disable S108
            {
            }
#pragma warning restore S108

            await client.CreateTableAsync(new CreateTableRequest()
            {
                TableName = tableName,
                KeySchema = new List<KeySchemaElement>()
                    {
                        new KeySchemaElement("HashKey", KeyType.HASH),
                        new KeySchemaElement("SortKey", KeyType.RANGE)
                    },
                AttributeDefinitions = new List<AttributeDefinition>()
                    {
                        new AttributeDefinition("HashKey", ScalarAttributeType.S),
                        new AttributeDefinition("SortKey", ScalarAttributeType.N)
                    },
                ProvisionedThroughput = new ProvisionedThroughput(100, 100)
            });
        }
    }
}
