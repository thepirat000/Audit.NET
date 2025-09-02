﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

using Audit.Core;
using Audit.DynamoDB.Providers;
using Audit.IntegrationTest;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable RedundantAssignment
// ReSharper disable RedundantCast
// ReSharper disable AccessToModifiedClosure

namespace Audit.DynamoDB.UnitTest
{
    [Category(TestCommon.Category.Integration)]
    [Category(TestCommon.Category.DynamoDb)]
#if NET462
    [Category(TestCommon.Category.MonoIncompatible)]
#endif
    public class DynamoDbTests
    {
        private const string TableName = "AuditEvents";
        private const string TableNameHashOnly = "AuditEventsHashOnly";
        
        private const string ServiceUrl = "http://localhost:8000";
        private static readonly AWSCredentials Credentials = new BasicAWSCredentials("key", "secret");

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            CreateDynamoTable(TableName, true).GetAwaiter().GetResult();
            CreateDynamoTable(TableNameHashOnly, false).GetAwaiter().GetResult();
        }

        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void TestDynamo_Constructors()
        {
            // Arrange
            var url = "http://test:8000/";
            using var client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { ServiceURL = url });

            // Act
            var defaultConstructor = new DynamoDataProvider();
            var configConstructor = new DynamoDataProvider(cfg => cfg.UseUrl(url));
            var clientConstructor = new DynamoDataProvider(client);
            var clientInterfaceConstructor = new DynamoDataProvider((IAmazonDynamoDB)client);

            // Assert
            Assert.That(defaultConstructor.Client, Is.Null);
            Assert.That(configConstructor.Client.Value.Config.ServiceURL, Is.EqualTo(url));
            Assert.That(clientConstructor.Client.Value.Config.ServiceURL, Is.EqualTo(url));
            Assert.That(clientInterfaceConstructor.Client.Value.Config.ServiceURL, Is.EqualTo(url));
        }

        [Test]
        public void TestDynamo_HashAndRange()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", ev => ev.StartDate.Year))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var hashes = new HashSet<string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                count++;
                hashes.Add((scope.EventId as object[])![0].ToString());
            });

            var eventType = "AuditEvents";
            var x = "start";
            using (new AuditScopeFactory().Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            {
                x = "end";
            }

            Assert.That(hashes.Count, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(2));

            // Assert events
            var hash = hashes.First();
            var ddp = Core.Configuration.DataProviderAs<DynamoDataProvider>();
            var ev = ddp.GetEvent<AuditEvent>(hash, DateTime.Now.Year);

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("AuditEvents"));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo(DateTime.Now.Year.ToString()));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public async Task TestDynamo_HashOnlyAsync()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var hashes = new HashSet<string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                count++;
                hashes.Add((scope.EventId as object[])![0].ToString());
            });

            var eventType = "AuditEvents";
            var x = "start";
            await using (await new AuditScopeFactory().CreateAsync(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            {
                x = "end";
            }

            Assert.That(hashes.Count, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(2));

            // Assert events
            var hash = hashes.First();
            var ddp = Core.Configuration.DataProviderAs<DynamoDataProvider>();
            var ev = await ddp.GetEventAsync<AuditEvent>(hash, null);

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("AuditEvents"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public void TestDynamo_HashOnly()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var hashes = new HashSet<string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                count++;
                hashes.Add((scope.EventId as object[])![0].ToString());
            });

            var eventType = "AuditEvents";
            var x = "start";
            using (new AuditScopeFactory().Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            {
                x = "end";
            }

            Assert.That(hashes.Count, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(2));

            // Assert events
            var hash = hashes.First();
            var ddp = Core.Configuration.DataProviderAs<DynamoDataProvider>();
            var ev = ddp.GetEvent<AuditEvent>(hash, null);

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("AuditEvents"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public async Task TestDynamo_HashAndRangeAsync()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", ev => ev.StartDate.Year))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var hashes = new HashSet<string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                count++;
                hashes.Add((scope.EventId as object[])![0].ToString());
            });

            var eventType = "AuditEvents";
            var x = "start";
            await using (await new AuditScopeFactory().CreateAsync(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
            { 
                x = "end";
            }

            Assert.That(hashes.Count, Is.EqualTo(1));
            Assert.That(count, Is.EqualTo(2));

            // Assert events
            var hash = hashes.First();
            var ddp = Core.Configuration.DataProviderAs<DynamoDataProvider>();
            var ev = await ddp.GetEventAsync<AuditEvent>(hash, DateTime.Now.Year);

            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("AuditEvents"));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo(DateTime.Now.Year.ToString()));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public void TestStressDynamo_HashAndRange()
        {
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .WithClient(new AmazonDynamoDBClient(Credentials, new AmazonDynamoDBConfig()
                    {
                        ServiceURL = ServiceUrl
                    }))
                    .Table(TableName, builder => builder
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", ev => ev.StartDate.Year))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            int n = 16;
            var hashes = new ConcurrentDictionary<string, string>();
            int count = 0;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            { Interlocked.Increment(ref count);
                hashes.TryAdd((scope.EventId as object[])![0].ToString(), null);
            });

            //Parallel random insert into event1, event2 and event3 containers
            Parallel.ForEach(Enumerable.Range(1, n), _ =>
            {
                var eventType = "AuditEvents";
                var x = "start";
                using (new AuditScopeFactory().Create(eventType, () => x, EventCreationPolicy.InsertOnStartReplaceOnEnd, null))
                {
                    x = "end";
                }
            });

            Assert.That(hashes.Count, Is.EqualTo(n));
            Assert.That(count, Is.EqualTo(n * 2));

            // Assert events
            int maxCheck = n / 4;
            int check = 0;
            foreach (var hash in hashes)
            {
                if (check++ > maxCheck)
                {
                    break;
                }
                var ddp = Core.Configuration.DataProviderAs<DynamoDataProvider>();
                var ev = ddp.GetEvent<AuditEvent>(hash.Key, DateTime.Now.Year);

                Assert.That(ev, Is.Not.Null);
                Assert.That(ev.EventType, Is.EqualTo("AuditEvents"));
                Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo(DateTime.Now.Year.ToString()));
                Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash.Key));
            }
        }
        
        [Test]
        public void GetEvent_ByObject_PrimitiveHashOnly()
        {
            // Arrange: Insert an event and get its hash key
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t.AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev1 = ddp.GetEvent<AuditEvent>(new Primitive(hash));
            var ev2 = ddp.GetEvent<AuditEvent>((DynamoDBEntry)new Primitive(hash));


            // Assert
            Assert.That(ev1, Is.Not.Null);
            Assert.That(ev1.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev1.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev2, Is.Not.Null);
            Assert.That(ev2.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev2.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public void GetEvent_ByObject_PrimitiveArrayHashAndRange()
        {
            // Arrange: Insert an event and get its hash and range keys
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(config => config
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", _ => 2025))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev1 = ddp.GetEvent<AuditEvent>(new[] { new Primitive(hash), 2025 });
            var ev2 = ddp.GetEvent<AuditEvent>(new Primitive(hash), 2025);

            // Assert
            Assert.That(ev1, Is.Not.Null);
            Assert.That(ev1.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev1.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev1.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
            Assert.That(ev2, Is.Not.Null);
            Assert.That(ev2.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev2.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev2.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
        }
        
        [Test]
        public void GetEvent_ByObject_Primitive()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t.AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = ddp.GetEvent<AuditEvent>(new Primitive(hash));

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public void UseDynamoDB_Extension()
        {
            // Arrange
            var clientMock = new Mock<IAmazonDynamoDB>();

            // Act
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(new Lazy<IAmazonDynamoDB>(() => clientMock.Object), ev => ev.EventType);
            
            // Assert
            Assert.That(Core.Configuration.DataProvider, Is.TypeOf<DynamoDataProvider>());
        }

        [Test]
        public void GetEvent_ByObject_PrimitiveArray()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", _ => 2025))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = ddp.GetEvent<AuditEvent>(new[] { new Primitive(hash), 2025 });

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
        }

        [Test]
        public void GetEvent_ByObject_DynamoDBEntry()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t.AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = ddp.GetEvent<AuditEvent>(new Primitive(hash) as DynamoDBEntry);

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public void GetEvent_ByObject_DynamoDBEntryArray()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", _ => 2025))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            using (new AuditScopeFactory().Create("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = ddp.GetEvent<AuditEvent>(new DynamoDBEntry[] { new Primitive(hash), (Primitive)2025 });

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
        }

        [Test]
        public void GetEvent_ByObject_Null_ReturnsNull()
        {
            var ddp = new DynamoDataProvider();
            var ev = ddp.GetEvent<AuditEvent>(null);
            Assert.That(ev, Is.Null);
        }

        [Test]
        public void GetEvent_ByObject_InvalidType_Throws()
        {
            var ddp = new DynamoDataProvider();
            var ex = Assert.Throws<ArgumentException>(() => ddp.GetEvent<AuditEvent>(12345));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain("Parameter must be convertible"));
        }

        [Test]
        public async Task GetEventAsync_ByObject_Primitive()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t.AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            await using (await new AuditScopeFactory().CreateAsync("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = await ddp.GetEventAsync<AuditEvent>(new Primitive(hash));

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public async Task GetEventAsync_ByObject_PrimitiveArray()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", _ => 2025))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            await using (await new AuditScopeFactory().CreateAsync("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = await ddp.GetEventAsync<AuditEvent>(new[] { new Primitive(hash), 2025 });

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
        }

        [Test]
        public async Task GetEventAsync_ByObject_DynamoDBEntry()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableNameHashOnly, t => t.AddHashKey("Id", DynamoDBEntryType.String))
                    .SetAttribute("Id", _ => Guid.NewGuid()))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            await using (await new AuditScopeFactory().CreateAsync("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = await ddp.GetEventAsync<AuditEvent>(new Primitive(hash) as DynamoDBEntry);

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
        }

        [Test]
        public async Task GetEventAsync_ByObject_DynamoDBEntryArray()
        {
            // Arrange
            Audit.Core.Configuration.Setup()
                .UseDynamoDB(cfg => cfg
                    .UseUrl(ServiceUrl, Credentials)
                    .Table(_ => TableName, t => t
                        .AddHashKey("Id", DynamoDBEntryType.String)
                        .AddRangeKey("SortKey", DynamoDBEntryType.Numeric))
                    .SetAttribute("Id", _ => Guid.NewGuid())
                    .SetAttribute("SortKey", _ => 2025))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            string hash = null;
            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaved, scope =>
            {
                hash = (scope.EventId as object[])![0].ToString();
            });

            await using (await new AuditScopeFactory().CreateAsync("TestEvent", () => "x", EventCreationPolicy.InsertOnStartReplaceOnEnd, null)) { }

            var ddp = Audit.Core.Configuration.DataProviderAs<DynamoDataProvider>();

            // Act
            var ev = await ddp.GetEventAsync<AuditEvent>(new DynamoDBEntry[] { new Primitive(hash), (Primitive)2025 });

            // Assert
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.EventType, Is.EqualTo("TestEvent"));
            Assert.That(ev.CustomFields["Id"].ToString(), Is.EqualTo(hash));
            Assert.That(ev.CustomFields["SortKey"].ToString(), Is.EqualTo("2025"));
        }

        [Test]
        public async Task GetEventAsync_ByObject_Null_ReturnsNull()
        {
            var ddp = new DynamoDataProvider();
            var ev = await ddp.GetEventAsync<AuditEvent>(null);
            Assert.That(ev, Is.Null);
        }

        [Test]
        public void GetEventAsync_ByObject_InvalidType_Throws()
        {
            var ddp = new DynamoDataProvider();
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => await ddp.GetEventAsync<AuditEvent>(12345));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain("Parameter must be convertible"));
        }
        
        private async Task CreateDynamoTable(string tableName, bool useRange)
        {
            AmazonDynamoDBConfig ddbConfig = new AmazonDynamoDBConfig
            {
                ServiceURL = ServiceUrl
            };
            var client = new AmazonDynamoDBClient(Credentials, ddbConfig);
            try
            {
                await client.DeleteTableAsync(tableName);
            }
            catch
            {
                // ignored
            }

            var createTableRequest = new CreateTableRequest()
            {
                TableName = tableName,
                KeySchema =
                [
                    new("Id", KeyType.HASH)
                ],
                AttributeDefinitions =
                [
                    new("Id", ScalarAttributeType.S)
                ],
                ProvisionedThroughput = new ProvisionedThroughput(100, 100)
            };

            if (useRange)
            {
                createTableRequest.KeySchema.Add(new("SortKey", KeyType.RANGE));
                createTableRequest.AttributeDefinitions.Add(new("SortKey", ScalarAttributeType.N));
            }

            await client.CreateTableAsync(createTableRequest);
        }
    }
}
