using System;
using System.Collections.Generic;
using System.Linq;
using Audit.Core;
using Audit.Core.Providers;
using Audit.Core.ConfigurationApi;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Audit.DynamoDB.Providers;
using Amazon.DynamoDBv2.DocumentModel;

namespace Audit.IntegrationTest
{
    public class IntegrationTests
    {

        [TestFixture]
        public class AuditTests
        {
            [Test]
            public void Test_FileDataProvider_FluentApi()
            {
                var x = new FileDataProvider(_ => _
                    .Directory(@"c:\t")
                    .FilenameBuilder(ev => "fn")
                    .FilenamePrefix("px")
                    .JsonSettings(new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Populate }));
                Assert.AreEqual(@"c:\t", x.DirectoryPath);
                Assert.AreEqual("fn", x.FilenameBuilder.Invoke(null));
                Assert.AreEqual("px", x.FilenamePrefix);
                Assert.AreEqual(DefaultValueHandling.Populate, x.JsonSettings.DefaultValueHandling);
            }

            [Test]
            [Category("Elasticsearch")]
            public void Test_ElasticSearchDataProvider_FluentApi()
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var x = new Elasticsearch.Providers.ElasticsearchDataProvider(_ => _
                    .ConnectionSettings(new Elasticsearch.Providers.AuditConnectionSettings(new Uri("http://server/")))
                    .Id(ev => "id")
                    .Index("ix")
                    .Type(ev => Nest.TypeName.From<int>()));
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.AreEqual("http://server/", (x.ConnectionSettings.ConnectionPool.Nodes.First().Uri.ToString()));
                Assert.IsTrue(x.IdBuilder.Invoke(null).Equals(new Nest.Id("id")));
                Assert.AreEqual("ix", x.IndexBuilder.Invoke(null).Name);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.AreEqual(Nest.TypeName.From<int>(), x.TypeNameBuilder.Invoke(null));
#pragma warning restore CS0618 // Type or member is obsolete
            }

            [Test]
            [Category("Mongo")]
            public void Test_MongoDataProvider_FluentApi()
            {
                var x = new MongoDB.Providers.MongoDataProvider(_ => _
                    .ConnectionString("c")
                    .Collection("col")
                    .CustomSerializerSettings(new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate })
                    .Database("db"));
                Assert.AreEqual("c", x.ConnectionString);
                Assert.AreEqual("col", x.Collection);
                Assert.AreEqual(DefaultValueHandling.IgnoreAndPopulate, x.JsonSerializerSettings.DefaultValueHandling);
                Assert.AreEqual("db", x.Database);
            }

            [Test]
            [Category("MySql")]
            public void Test_MySqlDataProvider_FluentApi()
            {
                var x = new MySql.Providers.MySqlDataProvider(_ => _
                    .ConnectionString("c")
                    .IdColumnName("id")
                    .JsonColumnName("j")
                    .TableName("t"));
                Assert.AreEqual("c", x.ConnectionString);
                Assert.AreEqual("id", x.IdColumnName);
                Assert.AreEqual("j", x.JsonColumnName);
                Assert.AreEqual("t", x.TableName);
            }

            [Test]
            [Category("PostgreSQL")]
            public void Test_PostgreDataProvider_FluentApi()
            {
                var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                    .ConnectionString("c")
                    .DataColumn("dc")
                    .IdColumnName("id")
                    .LastUpdatedColumnName("lud")
                    .Schema("sc")
                    .TableName("t"));
                Assert.AreEqual("c", x.ConnectionString);
                Assert.AreEqual("dc", x.DataColumnName);
                Assert.AreEqual("id", x.IdColumnName);
                Assert.AreEqual("lud", x.LastUpdatedDateColumnName);
                Assert.AreEqual("sc", x.Schema);
                Assert.AreEqual("t", x.TableName);
            }

            [Test]
            [Category("SQL")]
            public void Test_SqlDataProvider_FluentApi()
            {
                var x = new SqlDataProvider(_ => _
                    .ConnectionString("cnnString")
                    .IdColumnName(ev => ev.EventType)
                    .JsonColumnName("json")
                    .LastUpdatedColumnName("last")
                    .Schema(ev => "schema")
                    .TableName("table"));
                Assert.AreEqual("cnnString", x.ConnectionStringBuilder.Invoke(null));
                Assert.AreEqual("evType", x.IdColumnNameBuilder.Invoke(new AuditEvent() { EventType = "evType" }));
                Assert.AreEqual("json", x.JsonColumnNameBuilder.Invoke(null));
                Assert.AreEqual("last", x.LastUpdatedDateColumnNameBuilder.Invoke(null));
                Assert.AreEqual("schema", x.SchemaBuilder.Invoke(null));
                Assert.AreEqual("table", x.TableNameBuilder.Invoke(null));
            }

#if NET452
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
#endif

            [Test]
            [Category("AzureDocDb")]
            public void TestAzure()
            {
                SetAzureDocDbSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AzureDocDb")]
            public async Task TestAzureAsync()
            {
                SetAzureDocDbSettings();
                await TestUpdateAsync();
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
            public async Task TestFileAsync()
            {
                SetFileSettings();
                await TestUpdateAsync();
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
            [Category("AzureBlob")]
            public async Task TestAzureBlobAsync()
            {
                SetAzureBlobSettings();
                await TestUpdateAsync();
            }

            [Test]
            [Category("AzureBlob")]
            public void TestAzureBlob_ActiveDirectory()
            {
                SetAzureBlobSettings_ActiveDirectory();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AzureBlob")]
            public async Task TestAzureBlobAsync_ActiveDirectory()
            {
                SetAzureBlobSettings_ActiveDirectory();
                await TestUpdateAsync();
            }

            [Test]
            [Category("AzureBlob")]
            public void TestStressAzureBlob()
            {
                Audit.Core.Configuration.Setup()
                   .UseAzureBlobStorage(config => config
                       .ConnectionString(AzureSettings.AzureBlobCnnString)
                       .ContainerNameBuilder(ev => ev.EventType)
                       .BlobNameBuilder(ev => $"{ev.EventType}_{Guid.NewGuid()}.json"))
                   .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

                var rnd = new Random();

                //Parallel random insert into event1, event2 and event3 containers
                Parallel.ForEach(Enumerable.Range(1, 100), i =>
                {
                    var eventType = "event" + rnd.Next(1, 4); //1..3
                    var x = "start";
                    using (var s = AuditScope.Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd))
                    {
                        x = "end";
                    }
                });

                // Assert events are on correct container 
                var storageAccount = CloudStorageAccount.Parse(AzureSettings.AzureBlobCnnString);
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
                    using (var s = AuditScope.Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd))
                    {
                        x = "end";
                    }
                });

                Assert.AreEqual(N, hashes.Count);
                Assert.AreEqual(N*2, count);

                // Assert events
                int maxCheck = N / 4;
                int check = 0;
                foreach(var hash in hashes)
                {
                    if (check++ > maxCheck)
                    {
                        break;
                    }
                    var ddp = (Configuration.DataProvider as DynamoDataProvider);
                    var ev = ddp.GetEvent<AuditEvent>((Primitive)hash, (Primitive)DateTime.Now.Year);

                    Assert.NotNull(ev);
                    Assert.AreEqual("AuditEvents", ev.EventType);
                    Assert.AreEqual(DateTime.Now.Year, ev.CustomFields["SortKey"]);
                    Assert.AreEqual(hash, ev.CustomFields["HashKey"]);
                }
            }


            [Test]
            [Category("AzureBlob")]
            public void TestAzureTable()
            {
                SetAzureTableSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AzureBlob")]
            public async Task TestAzureTableAsync()
            {
                SetAzureTableSettings();
                await TestUpdateAsync();
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
            [Category("SQL")]
            public async Task TestSqlAsync()
            {
                SetSqlSettings();
                await TestUpdateAsync();
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
            [Category("PostgreSQL")]
            public async Task PostgreSqlAsync()
            {
                SetPostgreSqlSettings();
                await TestUpdateAsync();
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
            [Category("Mongo")]
            public async Task TestMongoAsync()
            {
                SetMongoSettings();
                await TestUpdateAsync();
            }

            [Test]
            [Category("Mongo")]
            public void TestMongoDateSerialization()
            {
                Audit.Core.Configuration.Setup()
                    .UseMongoDB(config => config
                        .ConnectionString("mongodb://localhost:27017")
                        .Database("Audit")
                        .Collection("Event"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                    .ResetActions();

                object evId = null;
                Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, s =>
                {
                    if (evId != null)
                    {
                        Assert.Fail("evId should be null");
                    }
                    evId = s.EventId;
                });
                var now = DateTime.UtcNow;
                using (var s = AuditScope.Create("test", null, new { someDate = now }))
                {
                }
                Audit.Core.Configuration.ResetCustomActions();
                var dp = Audit.Core.Configuration.DataProvider as MongoDataProvider;
                var evt = dp.GetEvent(evId);

                Assert.AreEqual(now.ToString("yyyyMMddHHmmss"), (evt.CustomFields["someDate"] as DateTime?).Value.ToString("yyyyMMddHHmmss"));
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
            [Category("MySql")]
            public async Task TestMySqlAsync()
            {
                SetMySqlSettings();
                await TestUpdateAsync();
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

            [Test]
            [Category("Elasticsearch")]
            public void TestElasticsearch()
            {
                SetElasticsearchSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("Elasticsearch")]
            public async Task TestElasticsearchAsync()
            {
                SetElasticsearchSettings();
                await TestUpdateAsync();
            }

            [Test]
            [Category("Dynamo")]
            public void TestDynamo()
            {
                SetDynamoSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("Dynamo")]
            public async Task TestDynamoAsync()
            {
                SetDynamoSettings();
                await TestUpdateAsync();
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
                var ids = new List<object>();
                Audit.Core.Configuration.ResetCustomActions();
                Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
                {
                    ids.Add(scope.EventId);
                });
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

                var dpType = Configuration.DataProvider.GetType().Name;
                var evFromApi = (dpType == "UdpDataProvider" || dpType == "EventLogDataProvider" || dpType == "AzureTableDataProvider") ? ev : Configuration.DataProvider.GetEvent(ids[0]);
                Assert.AreEqual(2, ids.Count);
                Assert.AreEqual(ids[0], ids[1]);
                Assert.AreEqual(ev.EventType, evFromApi.EventType);
                Assert.AreEqual(ev.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.CustomFields["ReferenceId"], evFromApi.CustomFields["ReferenceId"]);
                if (dpType != "ElasticsearchDataProvider")
                {
                    Assert.AreEqual((int)OrderStatus.Created, (int)((dynamic)ev.Target.SerializedOld).Order.Status);
                    Assert.AreEqual((int)OrderStatus.Submitted, (int)((dynamic)ev.Target.SerializedNew).Order.Status);
                }
                else
                {
                    Assert.AreEqual(OrderStatus.Created, JsonConvert.DeserializeObject<TestStruct>(ev.Target.SerializedOld.ToString()).Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, JsonConvert.DeserializeObject<TestStruct>(ev.Target.SerializedNew.ToString()).Order.Status);
                }
                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                //audit multiple 
                using (var a = AuditScope.Create(eventType, () => order.Status, new { ReferenceId = order.OrderId }))
                {
                    ev = a.Event;
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                using (var audit = AuditScope.Create("Order:Update", () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
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

                using (var audit = AuditScope.Create(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                Audit.Core.Configuration.ResetCustomActions();
            }

            public async Task TestUpdateAsync()
            {
                var order = DbCreateOrder();
                var reasonText = "the order was updated because ...";
                var eventType = "Order:Update";
                var ev = (AuditEvent)null;
                var ids = new List<object>();
                Audit.Core.Configuration.ResetCustomActions();
                Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
                {
                    ids.Add(scope.EventId);
                });
                //struct
                using (var a = await AuditScope.CreateAsync(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
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
                    await a.DisposeAsync();
                }

                var dpType = Configuration.DataProvider.GetType().Name;
                var evFromApi = (dpType == "UdpDataProvider" || dpType == "EventLogDataProvider" || dpType == "AzureTableDataProvider") ? ev : await Audit.Core.Configuration.DataProvider.GetEventAsync(ids[0]);
                Assert.AreEqual(2, ids.Count);
                Assert.AreEqual(ids[0], ids[1]);
                Assert.AreEqual(ev.EventType, evFromApi.EventType);
                Assert.AreEqual(ev.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.CustomFields["ReferenceId"], evFromApi.CustomFields["ReferenceId"]);
                if (dpType != "ElasticsearchDataProvider")
                {
                    Assert.AreEqual((int)OrderStatus.Created, (int)((dynamic)ev.Target.SerializedOld).Order.Status);
                    Assert.AreEqual((int)OrderStatus.Submitted, (int)((dynamic)ev.Target.SerializedNew).Order.Status);
                }
                else
                {
                    Assert.AreEqual(OrderStatus.Created, JsonConvert.DeserializeObject<TestStruct>(ev.Target.SerializedOld.ToString()).Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, JsonConvert.DeserializeObject<TestStruct>(ev.Target.SerializedNew.ToString()).Order.Status);
                }
                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                //audit multiple 
                using (var a = await AuditScope.CreateAsync(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = a.Event;
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                order = DbCreateOrder();

                using (var audit = await AuditScope.CreateAsync("Order:Update", () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
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

                using (var audit = await AuditScope.CreateAsync(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"]);

                Audit.Core.Configuration.ResetCustomActions();
            }

            public void TestInsert()
            {
                var ev = (AuditEvent)null;
                CustomerOrder order = null;
                using (var audit = AuditScope.Create("Order:Create", () => new TestStruct() { Id = 123, Order = order }))
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
                using (var audit = AuditScope.Create("Order:Delete", () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    DbDeteleOrder(order.OrderId);
                    order = null;
                }
                Assert.AreEqual(orderId, ev.CustomFields["ReferenceId"]);
            }

#if NET452 || NETCOREAPP2_0 || NETCOREAPP2_1
            [Test]
            public void TestEventLog()
            {
                SetEventLogSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            public void SetEventLogSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseEventLogProvider(config => config
                        .LogName("Application")
                        .SourcePath("TestApplication")
                        .MachineName(".")
                        .MessageBuilder(ev => $"{ev.StartDate} - {ev.EndDate} - {ev.EventType} - {ev.Environment.UserName}"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }
#endif

            public void SetAzureDocDbSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureDocumentDB(config => config
                        .ConnectionString(ev => AzureSettings.AzureDocDbUrl)
                        .AuthKey(ev => AzureSettings.AzureDocDbAuthKey))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetFileSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseFileLogProvider(fl => fl
                        .FilenameBuilder(_ => $"{_.Environment.UserName}_{DateTime.Now.Ticks}.json")
                        .DirectoryBuilder(_ => $@"C:\Temp\Logs\{DateTime.Now:yyyy-MM-dd}")
                        .JsonSettings(new JsonSerializerSettings()))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetAzureBlobSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureBlobStorage(config => config
                        .ConnectionString(AzureSettings.AzureBlobCnnString)
                        //.ContainerName("event")
                        .ContainerNameBuilder(ev => $"events{DateTime.Today:yyyyMMdd}")
                        .BlobNameBuilder(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetAzureBlobSettings_ActiveDirectory()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureBlobStorage(config => config
                        .AzureActiveDirectory(adConfig => adConfig
                            .AccountName(AzureSettings.BlobAccountName)
                            .TenantId(AzureSettings.BlobTenantId))
                        .ContainerNameBuilder(ev => $"events{DateTime.Today:yyyyMMdd}")
                        .BlobNameBuilder(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }


            public void SetAzureTableSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureTableStorage(config => config
                        .ConnectionString(AzureSettings.AzureBlobCnnString)
                        .TableName($"events{DateTime.Today:yyyyMMdd}")
                        .EntityBuilder(_ => _
                            .PartitionKey("testpart")
                            .Columns(cols => cols.FromObject(ev => new { ev.EventType, ev.Environment.UserName, ev.StartDate, ev.Duration }))))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetSqlSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseSqlServer(config => config
                        .ConnectionString("data source=localhost;initial catalog=Audit;integrated security=true;")
                        .TableName(ev => "Event")
                        .IdColumnName(ev => "EventId")
                        .JsonColumnName(ev => "Data")
                        .LastUpdatedColumnName("LastUpdatedDate"))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

            public void SetPostgreSqlSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UsePostgreSql(config => config
                        .ConnectionString("Server=localhost;Port=5432;User Id=fede;Password=fede;Database=postgres;")
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

            public void SetElasticsearchSettings()
            {
                var uri = new Uri(AzureSettings.ElasticSearchUrl);
                var ec = new Nest.ElasticClient(uri);
                ec.DeleteIndex(Nest.Indices.AllIndices, x => x.Index("auditevent"));

                Audit.Core.Configuration.Setup()
                    .UseElasticsearch(config => config
                        .ConnectionSettings(uri)
                        .Index("auditevent")
                        .Id(ev => Guid.NewGuid()))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
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
                {
                }

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

        public class Loop
        {
            public int Id { get; set; }
            public Loop Inner { get; set; }
        }

    }
}