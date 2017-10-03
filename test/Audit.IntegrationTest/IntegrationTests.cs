using System;
using System.Collections.Generic;
using System.Linq;
using Audit.Core;
using Audit.Core.Providers;
using Audit.Udp.Providers;
using Audit.MongoDB.Providers;
using Audit.SqlServer.Providers;
using Newtonsoft.Json.Linq;
using Audit.MongoDB.ConfigurationApi;
using Audit.AzureTableStorage.ConfigurationApi;
using System.Threading.Tasks;
using Audit.AzureTableStorage.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using Audit.AzureDocumentDB.Providers;
using Audit.AzureDocumentDB.ConfigurationApi;

namespace Audit.IntegrationTest
{
    public class IntegrationTests
    {
        private const string AzureBlobCnnString = "DefaultEndpointsProtocol=https;AccountName=thepirat;AccountKey=xxxxxxxxxxxxxxxxxxx==";
        private const string AzureDocDbUrl = "https://thepirat.documents.azure.com:443/";
        private const string AzureDocDbAuthKey = "xxxxxxxxxx==";

        [TestFixture]
        public class AuditTests
        {

#if NET451
            [Test]
            public void Test_StrongName_PublicToken()
            {
                var expected = "571d6b80b242c87e";
                ValidateToken(typeof(Audit.Core.AuditEvent), expected);
                ValidateToken(typeof(Audit.AzureDocumentDB.Providers.AzureDbDataProvider), expected);
                ValidateToken(typeof(Audit.DynamicProxy.AuditProxy), expected);
                ValidateToken(typeof(Audit.EntityFramework.AuditDbContext), expected);
                ValidateToken(typeof(Audit.Mvc.AuditAttribute), expected);
                ValidateToken(typeof(Audit.SqlServer.Providers.SqlDataProvider), expected);
                ValidateToken(typeof(Audit.WCF.AuditBehaviorAttribute), expected);
                ValidateToken(typeof(Audit.WebApi.AuditApiAttribute), expected);
            }
            private void ValidateToken(Type type, string expectedToken)
            {
                var tokenBytes = type.Assembly.GetName().GetPublicKeyToken();
                string pkt = String.Concat(tokenBytes.Select(i => i.ToString("x2")));
                Assert.AreEqual(expectedToken, pkt);
            }


            [Test]
            public void TestEventLog()
            {
                SetEventLogSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }
#endif

            [Test]
            [Category("AzureDocDb")]
            public void TestAzure()
            {
                SetAzureSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            public void TestFile()
            {
                SetFileSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AzureBlob")]
            public void TestAzureBlob()
            {
                SetAzureBlobSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            public void TestStressAzureBlob()
            {
                Audit.Core.Configuration.Setup()
                   .UseAzureBlobStorage(config => config
                       .ConnectionString(AzureBlobCnnString)
                       .ContainerNameBuilder(ev => ev.EventType)
                       .BlobNameBuilder(ev => $"{ev.EventType}_{Guid.NewGuid()}.json"))
                   .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

                var rnd = new Random();
                
                //Parallel random insert into event1, event2 and event3 containers
                Parallel.ForEach(Enumerable.Range(1, 100), i =>
                {
                    var eventType =  "event" + rnd.Next(1, 4); //1..3
                    var x = "start";
                    using (var s = AuditScope.Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd))
                    {
                        x = "end";
                    }
                });

                // Assert events are on correct container 
                var storageAccount = CloudStorageAccount.Parse(AzureBlobCnnString);
                var blobClient = storageAccount.CreateCloudBlobClient();
                for (int i = 1; i <= 3; i++)
                {
                    var eventId = "event" + i;
                    BlobContinuationToken continuationToken = null;
                    BlobResultSegment resultSegment = null;
                    do
                    {
                        resultSegment = blobClient.ListBlobsSegmentedAsync(eventId + "/", continuationToken).Result;
                        foreach (CloudBlockBlob blob in resultSegment.Results)
                        {
                            Assert.True(blob.Name.StartsWith(eventId + "_"));
                        }
                        continuationToken = resultSegment.ContinuationToken;
                    } while (continuationToken != null);
                }
            }

            [Test]
            [Category("SQL")]
            public void TestSql()
            {
                SetSqlSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("PostgreSQL")]
            public void PostgreSql()
            {
                SetPostgreSqlSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("Mongo")]
            public void TestMongo()
            {
                SetMongoSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("MySql")]
            public void TestMySql()
            {
                SetMySqlSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("UDP")]
            public void TestUdp()
            {
                SetUdpSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            public struct TestStruct
            {
                public int Id { get; set; }
                public CustomerOrder Order { get; set; }
            }

            public void TestUpdate()
            {
                var order = DbCreateOrder();
                var reasonText = "the order was updated because ...";
                var eventType = "Order:Update";
                var ev = (AuditEvent)null;
                //struct
                using (var a = AuditScope.Create(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = a.Event;
                    a.SetCustomField("$TestGuid", Guid.NewGuid());

                    a.SetCustomField("$null", (string)null);
                    a.SetCustomField("$array.dicts", new[]
                    {
                        new Dictionary<string, string>()
                        {
                            {"some.dots", "hi!"}
                        }
                    });


                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }
                
                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                //audit multiple 
                using (var a = AuditScope.Create(eventType, () => new { OrderStatus = order.Status, Items = order.OrderItems }, new { ReferenceId = order.OrderId }))
                { 
                   ev = a.Event;
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                using (var audit = AuditScope.Create("Order:Update", () => order.Status, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", reasonText);
                    audit.SetCustomField("ItemsBefore", order.OrderItems);
                    audit.SetCustomField("FirstItem", order.OrderItems.FirstOrDefault());

                    order = DbOrderUpdateStatus(order, IntegrationTests.OrderStatus.Submitted);
                    audit.SetCustomField("ItemsAfter", order.OrderItems);
                    audit.Comment("Status Updated to Submitted");
                    audit.Comment("Another Comment");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                using (var audit = AuditScope.Create(eventType, () => order, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);
            }

            public void TestInsert()
            {
                var ev = (AuditEvent)null;
                CustomerOrder order = null;
                using (var audit = AuditScope.Create("Order:Create", () => order))
                {
                    ev = audit.Event;
                    order = DbCreateOrder();
                    audit.SetCustomField("ReferenceId", order.OrderId);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);
            }

            public void TestDelete()
            {
                IntegrationTests.CustomerOrder order = DbCreateOrder();
                var ev = (AuditEvent)null;
                var orderId = order.OrderId;
                using (var audit = AuditScope.Create("Order:Delete", () => order, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    DbDeteleOrder(order.OrderId);
                    order = null;
                }
                Assert.AreEqual(orderId, ev.CustomFields["ReferenceId"]);
            }

#if NET451
            public void SetEventLogSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseEventLogProvider(config => config
                        .LogName("Application")
                        .SourcePath("TestApplication")
                        .MachineName("."))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }
#endif

            public void SetAzureSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureDocumentDB(config => config
                        .ConnectionString(AzureDocDbUrl)
                        .AuthKey(AzureDocDbAuthKey))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetFileSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseFileLogProvider(fl => fl
                        .FilenameBuilder(_ => $"{_.Environment.UserName}_{DateTime.Now.Ticks}.json")
                        .DirectoryBuilder(_ => $@"C:\Temp\Logs\{DateTime.Now:yyyy-MM-dd}"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetAzureBlobSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureBlobStorage(config => config
                        .ConnectionString(AzureBlobCnnString)
                        //.ContainerName("event")
                        .ContainerNameBuilder(ev => $"events{ev.StartDate:yyyyMMdd}")
                        .BlobNameBuilder(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetSqlSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseSqlServer(config => config
                        .ConnectionString("data source=localhost;initial catalog=Audit;integrated security=true;")
                        .TableName("Event")
                        .IdColumnName("EventId")
                        .JsonColumnName("Data")
                        .LastUpdatedColumnName("LastUpdatedDate"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetPostgreSqlSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UsePostgreSql(config => config
                        .ConnectionString("Server=127.0.0.1;Port=5432;User Id=fede;Password=fede;Database=postgres;")
                        .TableName("eventb")
                        .IdColumnName("id")
                        .DataColumn("data", Audit.PostgreSql.Configuration.DataType.JSONB)
                        .LastUpdatedColumnName("updated_date"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetMongoSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseMongoDB(config => config
                        .ConnectionString("mongodb://localhost:27017")
                        .Database("Audit")
                        .Collection("Event"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetMySqlSettings()
            { 
                Audit.Core.Configuration.Setup()
                    .UseMySql(config => config
                        .ConnectionString("Server=localhost; Database=events; Uid=admin; Pwd=admin;")
                        .TableName("event")
                        .IdColumnName("id")
                        .JsonColumnName("data"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetUdpSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseUdp(config => config
                        .RemoteAddress("127.0.0.1")
                        .RemotePort(12349)
                        .MulticastMode(MulticastMode.Disabled))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }


            public static void ExecuteStoredProcedure(IntegrationTests.CustomerOrder order, IntegrationTests.OrderStatus status)
            {
            }

            public static void DbDeteleOrder(string id)
            {
            }

            public static IntegrationTests.CustomerOrder DbCreateOrder()
            {
                var order = new IntegrationTests.CustomerOrder()
                {
                    OrderId = Guid.NewGuid().ToString(),
                    CustomerId = "customer 123 some 'quotes' to test's. double ''. some double \"quotes\" \"",
                    Status = IntegrationTests.OrderStatus.Created,
                    OrderItems = new List<IntegrationTests.CustomerOrderItem>()
                    {
                        new IntegrationTests.CustomerOrderItem()
                        {
                            Sku = "1002",
                            Quantity = 3
                        }
                    }
                };
                return order;
            }

            public static IntegrationTests.CustomerOrder DbOrderUpdateStatus(IntegrationTests.CustomerOrder order,
                IntegrationTests.OrderStatus newStatus)
            {
                order.Status = newStatus;
                order.OrderItems = null;
                return order;
            }
        }

        public class CustomerOrder
        {
            public string OrderId { get; set; }
            public IntegrationTests.OrderStatus Status { get; set; }
            public string CustomerId { get; set; }
            public IEnumerable<IntegrationTests.CustomerOrderItem> OrderItems { get; set; }
        }

        public class CustomerOrderItem
        {
            public string Sku { get; set; }
            public double Quantity { get; set; }
        }

        public enum OrderStatus
        {
            Created = 2,
            Submitted = 4,
            Cancelled = 10
        }
    }
}