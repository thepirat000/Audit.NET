using Audit.Core;
using Audit.Core.Providers;
using Audit.Kafka.Providers;
using Audit.Udp.Providers;
using Confluent.Kafka;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
#if NETCOREAPP3_0 || NET5_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Threading;
#endif
#if NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json;
#endif


namespace Audit.IntegrationTest
{
    public class IntegrationTests
    {

        [TestFixture]
        public class AuditTests
        {
           
            [Test]
            [Category("Kafka")]
            public void Test_KafkaDataProvider_FluentApi()
            {
                var x = new KafkaDataProvider<string>(_ => _
                    .ProducerConfig(new ProducerConfig())
                    .Topic("audit-topic")
                    .Partition(0)
                    .KeySelector(ev => "key"));

                Assert.AreEqual("audit-topic", x.TopicSelector.Invoke(null));
                Assert.AreEqual(0, x.PartitionSelector.Invoke(null));
                Assert.AreEqual("key", x.KeySelector.Invoke(null));
            }

            [Test]
            public void Test_FileDataProvider_FluentApi()
            {
                var x = new FileDataProvider(_ => _
                    .Directory(@"c:\t")
                    .FilenameBuilder(ev => "fn")
                    .FilenamePrefix("px"));

                Assert.AreEqual(@"c:\t", x.DirectoryPath);
                Assert.AreEqual("fn", x.FilenameBuilder.Invoke(null));
                Assert.AreEqual("px", x.FilenamePrefix);
            }

#if NET461 || NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
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
                    .TableName("t")
                    .CustomColumn("c1", ev => 1)
                    .CustomColumn("c2", ev => 2));
                Assert.AreEqual("c", x.ConnectionStringBuilder(null));
                Assert.AreEqual("dc", x.DataColumnNameBuilder(null));
                Assert.AreEqual("id", x.IdColumnNameBuilder(null));
                Assert.AreEqual("lud", x.LastUpdatedDateColumnNameBuilder(null));
                Assert.AreEqual("sc", x.SchemaBuilder(null));
                Assert.AreEqual("t", x.TableNameBuilder(null));
                Assert.AreEqual(2, x.CustomColumns.Count);
                Assert.AreEqual("c1", x.CustomColumns[0].Name);
                Assert.AreEqual(1, x.CustomColumns[0].Value.Invoke(null));
                Assert.AreEqual("c2", x.CustomColumns[1].Name);
                Assert.AreEqual(2, x.CustomColumns[1].Value.Invoke(null));
            }

            [Test]
            [Category("PostgreSQL")]
            public void Test_PostgreDataProvider_FluentApiBuilder()
            {
                var x = new PostgreSql.Providers.PostgreSqlDataProvider(_ => _
                    .ConnectionString(ev => "c")
                    .DataColumn(ev => "dc")
                    .IdColumnName(ev => "id")
                    .LastUpdatedColumnName(ev => "lud")
                    .Schema(ev => "sc")
                    .TableName(ev => "t")
                    .CustomColumn("c1", ev => 1)
                    .CustomColumn("c2", ev => 2));
                Assert.AreEqual("c", x.ConnectionStringBuilder(null));
                Assert.AreEqual("dc", x.DataColumnNameBuilder(null));
                Assert.AreEqual("id", x.IdColumnNameBuilder(null));
                Assert.AreEqual("lud", x.LastUpdatedDateColumnNameBuilder(null));
                Assert.AreEqual("sc", x.SchemaBuilder(null));
                Assert.AreEqual("t", x.TableNameBuilder(null));
                Assert.AreEqual(2, x.CustomColumns.Count);
                Assert.AreEqual("c1", x.CustomColumns[0].Name);
                Assert.AreEqual(1, x.CustomColumns[0].Value.Invoke(null));
                Assert.AreEqual("c2", x.CustomColumns[1].Name);
                Assert.AreEqual(2, x.CustomColumns[1].Value.Invoke(null));
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

            public void SetPostgreSqlSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UsePostgreSql(config => config
                        .ConnectionString(_ => AzureSettings.PostgreSqlConnectionString)
                        .TableName("event")
                        .IdColumnName(_ => "id")
                        .DataColumn("data", Audit.PostgreSql.Configuration.DataType.JSONB)
                        .LastUpdatedColumnName("updated_date")
                        .CustomColumn("event_type", ev => ev.EventType)
                        .CustomColumn("some_date", ev => DateTime.UtcNow)
                        .CustomColumn("some_null", ev => null))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();
            }

#endif

            [Test]
            [Category("EF")]
            public void Test_EfDataProvider_FluentApi()
            {
                var ctx = new OtherContextFromDbContext();
                var x = new EntityFramework.Providers.EntityFrameworkDataProvider(_ => _
                    .UseDbContext(ev => ctx)
                    .AuditTypeExplicitMapper(cfg => cfg
                        .Map<Blog, AuditBlog>()
                        .Map<Post, AuditPost>())
                    .IgnoreMatchedProperties(t => t == typeof(string)));

                Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(typeof(string)));
                Assert.AreEqual(false, x.IgnoreMatchedPropertiesFunc(typeof(int)));
                Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
                Assert.AreEqual(typeof(AuditBlog), x.AuditTypeMapper.Invoke(typeof(Blog), null));
                Assert.AreEqual(typeof(AuditPost), x.AuditTypeMapper.Invoke(typeof(Post), null));
                Assert.AreEqual(null, x.AuditTypeMapper.Invoke(typeof(AuditBlog), null));
            }

            [Test]
            [Category("EF")]
            public void Test_EfDataProvider_FluentApi2()
            {
                var ctx = new OtherContextFromDbContext();
                var x = new EntityFramework.Providers.EntityFrameworkDataProvider(_ => _
                    .UseDbContext(ev => ctx)
                    .AuditTypeMapper(t => typeof(AuditBlog))
                    .AuditEntityAction((ev, ent, obj) =>
                    {
                        return (bool) (((dynamic)obj).Id == 1);
                    })
                    .IgnoreMatchedProperties(true));


                Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
                Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
                Assert.AreEqual(typeof(AuditBlog), x.AuditTypeMapper.Invoke(typeof(Blog), null));
                Assert.AreEqual(typeof(AuditBlog), x.AuditTypeMapper.Invoke(typeof(Post), null));
                Assert.AreEqual(true, x.AuditEntityAction.Invoke(new AuditEvent(), new EntityFramework.EventEntry(), new { Id = 1 }).Result);
            }

            [Test]
            [Category("EF")]
            public void Test_EfDataProvider_FluentApi3()
            {
                var ctx = new OtherContextFromDbContext();
                var x = new EntityFramework.Providers.EntityFrameworkDataProvider(_ => _
                    .UseDbContext(ev => ctx)
                    .AuditTypeNameMapper(s => "Audit" + s)
                    .AuditEntityAction((ev, ent, obj) =>
                    {
                        return (bool) (((dynamic)obj).Id == 1);
                    })
                    .IgnoreMatchedProperties(true));


                Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
                Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
                Assert.AreEqual(typeof(AuditBlog), x.AuditTypeMapper.Invoke(typeof(Blog), null));
                Assert.AreEqual(typeof(AuditPost), x.AuditTypeMapper.Invoke(typeof(Post), null));
                Assert.AreEqual(true, x.AuditEntityAction.Invoke(new AuditEvent(), new EntityFramework.EventEntry(), new { Id = 1 }).Result);
            }

            [Test]
            [Category("EF")]
            public void Test_EfDataProvider_FluentApi4()
            {
                var ctx = new OtherContextFromDbContext();
                var x = new EntityFramework.Providers.EntityFrameworkDataProvider(_ => _
                    .UseDbContext(ev => ctx)
                    .AuditTypeExplicitMapper(cfg => cfg
                        .Map<Blog>(entry => entry.Action == "Update" ? typeof(AuditPost) : typeof(AuditBlog))
                        .Map<Post, AuditPost>())
                    .IgnoreMatchedProperties(true));

                Assert.AreEqual(true, x.IgnoreMatchedPropertiesFunc(null));
                Assert.AreEqual(ctx, x.DbContextBuilder.Invoke(null));
                Assert.AreEqual(typeof(AuditPost), x.AuditTypeMapper.Invoke(typeof(Blog), new EntityFramework.EventEntry() { Action = "Update" }));
                Assert.AreEqual(typeof(AuditBlog), x.AuditTypeMapper.Invoke(typeof(Blog), new EntityFramework.EventEntry() { Action = "Insert" }));
                Assert.AreEqual(typeof(AuditPost), x.AuditTypeMapper.Invoke(typeof(Post), null));
                Assert.AreEqual(null, x.AuditTypeMapper.Invoke(typeof(AuditBlog), null));
            }

            [Test]
            public void Test_StrongName_PublicToken()
            {
                var expected = "571d6b80b242c87e";
                ValidateToken(typeof(Audit.Core.AuditEvent), expected);
                ValidateToken(typeof(Audit.AzureCosmos.Providers.AzureCosmosDataProvider), expected);
                ValidateToken(typeof(Audit.EntityFramework.AuditDbContext), expected);
                ValidateToken(typeof(Audit.Mvc.AuditAttribute), expected);
                ValidateToken(typeof(Audit.SqlServer.Providers.SqlDataProvider), expected);
                ValidateToken(typeof(Audit.WebApi.AuditApiAttribute), expected);
            }

            private static void ValidateToken(Type type, string expectedToken)
            {
                var tokenBytes = type.Assembly.GetName().GetPublicKeyToken();
                var publicKeyToken = string.Concat(tokenBytes.Select(i => i.ToString("x2")));
                Assert.AreEqual(expectedToken, publicKeyToken);
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
            [Ignore("Ignored until AD is configured in the test Azure account")]
            public void TestAzureBlob_ActiveDirectory()
            {
                SetAzureBlobSettings_ActiveDirectory();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AzureBlob")]
            [Ignore("Ignored until AD is configured in the test Azure account")]
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
                       .ContainerName(ev => ev.EventType)
                       .BlobName(ev => $"{ev.EventType}_{Guid.NewGuid()}.json"))
                   .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

                var rnd = new Random();

                //Parallel random insert into event1, event2 and event3 containers
                Parallel.ForEach(Enumerable.Range(1, 100), i =>
                {
                    var eventType = "event" + rnd.Next(1, 4); //1..3
                    var x = "start";
                    using (var s = new AuditScopeFactory().Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
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
            [Category("UDP")]
            public void TestUdp()
            {
                SetUdpSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

#if NET461 || NETCOREAPP3_0 || NET5_0_OR_GREATER
            [Test]
            [Category("AmazonQLDB")]
            public void TestAmazonQLDB()
            {
                SetAmazonQLDBSettings();
                TestUpdate();
                TestInsert();
                TestDelete();
            }

            [Test]
            [Category("AmazonQLDB")]
            public async Task TestAmazonQLDBAsync()
            {
                SetAmazonQLDBSettings();
                await TestUpdateAsync();
            }
#endif

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
                AuditEvent evFromApi;
                if (dpType == "AzureCosmosDataProvider")
                {
#if NET452 || NET461
                    evFromApi = Configuration.DataProvider.GetEvent(new Tuple<string, string>(ids[0].ToString(), eventType));
#else
                    evFromApi = Configuration.DataProvider.GetEvent((ids[0].ToString(), eventType));
#endif
                }
                else if (dpType == "UdpDataProvider" || dpType == "EventLogDataProvider" || dpType == "AzureTableDataProvider")
                {
                    evFromApi = ev;
                }
                else
                {
                    evFromApi = Configuration.DataProvider.GetEvent(ids[0]);
                }
                Assert.AreEqual(2, ids.Count);
                Assert.AreEqual(ids[0], ids[1]);
                Assert.AreEqual(ev.EventType, evFromApi.EventType);
                Assert.AreEqual(ev.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.CustomFields["ReferenceId"].ToString(), evFromApi.CustomFields["ReferenceId"].ToString());
                if (evFromApi.Target.Old is TestStruct)
                {
                    Assert.AreEqual(OrderStatus.Created, ((TestStruct?)evFromApi.Target.Old).Value.Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, ((TestStruct?)evFromApi.Target.New).Value.Order.Status);
                }
                else if (evFromApi.Target.Old is ExpandoObject)
                {
                    Assert.AreEqual((int)OrderStatus.Created, (int)((dynamic)evFromApi.Target.Old).Order.Status);
                    Assert.AreEqual((int)OrderStatus.Submitted, (int)((dynamic)evFromApi.Target.New).Order.Status);
                }
                else
                {
                    Assert.AreEqual(OrderStatus.Created, Core.Configuration.JsonAdapter.Deserialize<TestStruct>(evFromApi.Target.Old.ToString()).Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, Core.Configuration.JsonAdapter.Deserialize<TestStruct>(evFromApi.Target.New.ToString()).Order.Status);
                }
                Assert.AreEqual(order.OrderId, evFromApi.CustomFields["ReferenceId"].ToString());

                order = DbCreateOrder();

                //audit multiple 
                using (var a = AuditScope.Create(eventType, () => order, new { ReferenceId = order.OrderId }))
                {
                    ev = a.Event;
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

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

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

                order = DbCreateOrder();

                using (var audit = AuditScope.Create(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

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
                AuditEvent evFromApi;
                if (dpType == "AzureCosmosDataProvider")
                {
#if NET452 || NET461
                    evFromApi = await Configuration.DataProvider.GetEventAsync(new Tuple<string, string>(ids[0].ToString(), eventType));
#else
                    evFromApi = await Configuration.DataProvider.GetEventAsync((ids[0].ToString(), eventType));
#endif
                }
                else if (dpType == "UdpDataProvider" || dpType == "EventLogDataProvider" || dpType == "AzureTableDataProvider")
                {
                    evFromApi = ev;
                }
                else
                {
                    evFromApi = Configuration.DataProvider.GetEvent(ids[0]);
                }
                Assert.AreEqual(2, ids.Count);
                Assert.AreEqual(ids[0], ids[1]);
                Assert.AreEqual(ev.EventType, evFromApi.EventType);
                Assert.AreEqual(ev.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.StartDate.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"), evFromApi.EndDate.Value.ToUniversalTime().ToString("yyyyMMddHHmmss"));
                Assert.AreEqual(ev.CustomFields["ReferenceId"].ToString(), evFromApi.CustomFields["ReferenceId"].ToString());
                if (evFromApi.Target.Old is TestStruct)
                {
                    Assert.AreEqual(OrderStatus.Created, ((TestStruct?)evFromApi.Target.Old).Value.Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, ((TestStruct?)evFromApi.Target.New).Value.Order.Status);
                }
                else if (evFromApi.Target.Old is ExpandoObject)
                {
                    Assert.AreEqual((int)OrderStatus.Created, (int)((dynamic)evFromApi.Target.Old).Order.Status);
                    Assert.AreEqual((int)OrderStatus.Submitted, (int)((dynamic)evFromApi.Target.New).Order.Status);
                }
                else
                { 
                    Assert.AreEqual(OrderStatus.Created, Core.Configuration.JsonAdapter.Deserialize<TestStruct>(evFromApi.Target.Old.ToString()).Order.Status);
                    Assert.AreEqual(OrderStatus.Submitted, Core.Configuration.JsonAdapter.Deserialize<TestStruct>(evFromApi.Target.New.ToString()).Order.Status);
                }
                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

                order = DbCreateOrder();

                //audit multiple 
                using (var a = await AuditScope.CreateAsync(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = a.Event;
                    order = DbOrderUpdateStatus(order, OrderStatus.Submitted);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

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

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

                order = DbCreateOrder();

                using (var audit = await AuditScope.CreateAsync(eventType, () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }))
                {
                    ev = audit.Event;
                    audit.SetCustomField("Reason", "reason");
                    ExecuteStoredProcedure(order, IntegrationTests.OrderStatus.Submitted);
                    order.Status = IntegrationTests.OrderStatus.Submitted;
                    audit.Comment("Status Updated to Submitted");
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());

                Audit.Core.Configuration.ResetCustomActions();
            }

            public void TestInsert()
            {
                var ev = (AuditEvent)null;
                CustomerOrder order = null;
                using (var audit = new AuditScopeFactory().Create("Order:Create", () => new TestStruct() { Id = 123, Order = order }))
                {
                    ev = audit.Event;
                    order = DbCreateOrder();
                    audit.SetCustomField("ReferenceId", order.OrderId);
                }

                Assert.AreEqual(order.OrderId, ev.CustomFields["ReferenceId"].ToString());
            }

            public void TestDelete()
            {
                IntegrationTests.CustomerOrder order = DbCreateOrder();
                var ev = (AuditEvent)null;
                var orderId = order.OrderId;
                using (var audit = new AuditScopeFactory().Create("Order:Delete", () => new TestStruct() { Id = 123, Order = order }, new { ReferenceId = order.OrderId }, null, null))
                {
                    ev = audit.Event;
                    DbDeteleOrder(order.OrderId);
                    order = null;
                }
                Assert.AreEqual(orderId, ev.CustomFields["ReferenceId"].ToString());
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
                        .ConnectionString(AzureSettings.AzureBlobCnnString)
                        .ContainerName(ev => $"events{DateTime.Today:yyyyMMdd}")
                        .BlobName(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json")
                        .WithAccessTier(StandardBlobTier.Hot)
                        .WithMetadata(ev => new Dictionary<string, string>() { { "user", ev.Environment.UserName }, { "machine", ev.Environment.MachineName } }))
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            }

            public void SetAzureBlobSettings_ActiveDirectory()
            {
                Audit.Core.Configuration.Setup()
                    .UseAzureBlobStorage(config => config
                        .AzureActiveDirectory(adConfig => adConfig
                            .AccountName(AzureSettings.BlobAccountName)
                            .TenantId(AzureSettings.BlobTenantId))
                        .ContainerName(ev => $"events{DateTime.Today:yyyyMMdd}")
                        .BlobName(ev => $"{ev.StartDate:yyyy-MM}/{ev.Environment.UserName}/{Guid.NewGuid()}.json"))
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
                var cnnString = TestHelper.GetConnectionString("Audit");

                Audit.Core.Configuration.Setup()
                    .UseSqlServer(config => config
                        .ConnectionString(cnnString)
                        .TableName(ev => "Event")
                        .IdColumnName(ev => "EventId")
                        .JsonColumnName(ev => "Data")
                        .LastUpdatedColumnName("LastUpdatedDate")
                        .CustomColumn("EventType", ev => ev.EventType)
#if NET452 || NET461
                        .SetDatabaseInitializerNull()
#endif
                        )
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

#if NET461 || NETCOREAPP2_0 || NETCOREAPP3_0 || NET5_0_OR_GREATER
            public void SetAmazonQLDBSettings()
            {
                Audit.Core.Configuration.Setup()
                    .UseAmazonQldb(config =>
                    {
                        config
                            .UseLedger("audit-ledger")
                            .UseMaxConcurrentTransactions(5)
                            .And
                            .Table(ev => ev.EventType.Replace(":", ""))
                            .SetAttribute("Source", ev => "Production");
                    })
                    .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
                    .ResetActions();

            }
#endif

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