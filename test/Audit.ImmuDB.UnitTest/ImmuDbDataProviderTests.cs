using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.ImmuDB.Providers;

using ImmuDB;
using ImmuDB.Iam;

using NUnit.Framework;

namespace Audit.ImmuDB.UnitTest
{
    [TestFixture]
    [NonParallelizable]
    [Category("Integration")]
    [Category("ImmuDB")]
    public class ImmuDbDataProviderTests
    {
        private const string ServerUrl = "localhost";
        private const string UsernameVerified = "adminv";
        private const string UsernameUnverified = "adminu";
        private const string DatabaseVerified = "dbv";
        private const string DatabaseUnverified = "dbu";
        private const string Password = "adminpassword";
        
        [OneTimeSetUp]
        public async Task Setup()
        {
            // Try to connect with the default user
            var client = await ImmuClient.NewBuilder().Open();

            var dbs = await client.Databases();

            if (!dbs.Contains(DatabaseVerified))
            {
                await client.CreateDatabase(DatabaseVerified);
            }

            if (!dbs.Contains(DatabaseUnverified))
            {
                await client.CreateDatabase(DatabaseUnverified);
            }

            var users = await client.ListUsers();

            if (!users.Exists(u => u.Name == UsernameVerified))
            {
                await client.CreateUser(UsernameVerified, Password, Permission.PERMISSION_ADMIN, DatabaseVerified);
            }

            if (!users.Exists(u => u.Name == UsernameUnverified))
            {
                await client.CreateUser(UsernameUnverified, Password, Permission.PERMISSION_ADMIN, DatabaseUnverified);
            }

            await client.Close();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            ImmuClient.ReleaseSdkResources();
        }

        [SetUp]
        public void SetUp()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public async Task TestDefaultConstructor()
        {
            var dp = new ImmuDbDataProvider();

            var client = await dp.GetClientAsync(null);

            var dbs = await client.Databases();

            Assert.That(dbs, Does.Contain(DatabaseVerified));
            Assert.That(dbs, Does.Contain(DatabaseUnverified));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestImmuDbDataProvider_InsertOnEnd(bool useVerified)
        {
            // Arrange
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";

            var provider = new ImmuDbDataProvider(c => c
                .Database(useVerified ? DatabaseVerified : DatabaseUnverified)
                .Username(useVerified ? UsernameVerified : UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(useVerified)); 

            // Act
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });

            scope.Dispose();

            var eventId = scope.EventId as byte[];
            var client = provider.GetClientAsync(scope.Event).GetAwaiter().GetResult();
            var entry = client.Get(eventId!, 0UL).GetAwaiter().GetResult();

            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestImmuDbDataProvider_InsertOnEndAsync(bool useVerified)
        {
            // Arrange
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";

            var provider = new ImmuDbDataProvider(c => c
                .Database(useVerified ? DatabaseVerified : DatabaseUnverified)
                .Username(useVerified ? UsernameVerified : UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(useVerified));

            // Act
            var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });

            await scope.DisposeAsync();

            var eventId = scope.EventId as byte[];
            var client = await provider.GetClientAsync(scope.Event);
            var entry = await client.Get(eventId!, 0UL);

            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestImmuDbDataProvider_InsertOnStartReplaceOnEnd(bool useVerified)
        {
            // Arrange
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";

            var provider = new ImmuDbDataProvider(c => c
                .Database(useVerified ? DatabaseVerified : DatabaseUnverified)
                .Username(useVerified ? UsernameVerified : UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(useVerified));

            // Act
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                EventType = value
            });

            var eventId = scope.EventId as byte[];

            var client = provider.GetClientAsync(scope.Event).GetAwaiter().GetResult();
            
            var entryBefore = client.Get(eventId!, 0UL).GetAwaiter().GetResult();

            scope.Event.EventType = "new-event-type";

            scope.Dispose();

            var entryAfter = client.Get(eventId!, 0UL).GetAwaiter().GetResult();

            // Assert
            Assert.That(entryBefore, Is.Not.Null);
            Assert.That(entryAfter, Is.Not.Null);
            Assert.That(entryBefore.Key, Is.EqualTo(key));
            Assert.That(entryAfter.Key, Is.EqualTo(key));
            Assert.That(entryBefore.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
            Assert.That(entryAfter.Value, Is.EqualTo("new-event-type"u8.ToArray()));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task TestImmuDbDataProvider_InsertOnStartReplaceOnEndAsync(bool useVerified)
        {
            // Arrange
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";

            var provider = new ImmuDbDataProvider(c => c
                .Database(useVerified ? DatabaseVerified : DatabaseUnverified)
                .Username(useVerified ? UsernameVerified : UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(useVerified));

            // Act
            var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                EventType = value
            });

            var eventId = scope.EventId as byte[];

            var client = await provider.GetClientAsync(scope.Event);

            var entryBefore = await client.Get(eventId!, 0UL);

            scope.Event.EventType = "new-event-type";

            await  scope.DisposeAsync();

            var entryAfter = await client.Get(eventId!, 0UL);

            // Assert
            Assert.That(entryBefore, Is.Not.Null);
            Assert.That(entryAfter, Is.Not.Null);
            Assert.That(entryBefore.Key, Is.EqualTo(key));
            Assert.That(entryAfter.Key, Is.EqualTo(key));
            Assert.That(entryBefore.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
            Assert.That(entryAfter.Value, Is.EqualTo("new-event-type"u8.ToArray()));
        }

        [Test]
        public void TestImmuDbDataProvider_MultipleThreads()
        {
            var tasks = new Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(() =>
                {
                    var key = Guid.NewGuid().ToByteArray();
                    var value = $"TestEvent{index}";
                    var provider = new ImmuDbDataProvider(c => c
                        .Database(DatabaseUnverified)
                        .Username(UsernameUnverified)
                        .Password(Password)
                        .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                        .KeySelector(_ => key)
                        .ValueSelector(ev => ev.EventType));
                    var scope = AuditScope.Create(new AuditScopeOptions()
                    {
                        DataProvider = provider,
                        CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                        EventType = value
                    });
                    Thread.Sleep(5);
                    scope.Dispose();
                    var eventId = scope.EventId as byte[];
                    var client = provider.GetClientAsync(scope.Event).GetAwaiter().GetResult();
                    var entry = client.Get(eventId!, 0UL).GetAwaiter().GetResult();
                    Assert.That(entry, Is.Not.Null);
                    Assert.That(entry.Key, Is.EqualTo(key));
                    Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
                });
            }

            Task.WaitAll(tasks);

            foreach (var task in tasks)
            {
                Assert.That(task.IsCompletedSuccessfully, Is.True, "One or more tasks did not complete successfully.");

            }
        }

        [Test]
        public async Task TestImmuDbDataProvider_MultipleThreadsAsync()
        {
            var tasks = new Task[10];

            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var key = Guid.NewGuid().ToByteArray();
                    var value = $"TestEvent{index}";
                    var provider = new ImmuDbDataProvider(c => c
                        .Database(DatabaseUnverified)
                        .Username(UsernameUnverified)
                        .Password(Password)
                        .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                        .KeySelector(_ => key)
                        .ValueSelector(ev => ev.EventType));
                    var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
                    {
                        DataProvider = provider,
                        CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd,
                        EventType = value
                    });
                    await Task.Delay(5);
                    await scope.DisposeAsync();
                    var eventId = scope.EventId as byte[];
                    var client = await provider.GetClientAsync(scope.Event);
                    var entry = await client.Get(eventId!, 0UL);
                    Assert.That(entry, Is.Not.Null);
                    Assert.That(entry.Key, Is.EqualTo(key));
                    Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
                });
            }

            await Task.WhenAll(tasks);
            
            foreach (var task in tasks)
            {
                Assert.That(task.IsCompletedSuccessfully, Is.True, "One or more tasks did not complete successfully.");

            }
        }

        [Test]
        public void TestImmuDbDataProvider_ClientCache()
        {
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";
            var provider = new ImmuDbDataProvider(c => c
                .Database(DatabaseUnverified)
                .Username(UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType));
            var scope1 = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });
            scope1.Dispose();

            var eventId1 = scope1.EventId as byte[];
            var client1 = provider.GetClientAsync(scope1.Event).GetAwaiter().GetResult();
            var entry1 = client1.Get(eventId1!, 0UL).GetAwaiter().GetResult();
            var scope2 = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });
            scope2.Dispose();
            var eventId2 = scope2.EventId as byte[];
            var client2 = provider.GetClientAsync(scope2.Event).GetAwaiter().GetResult();
            var entry2 = client2.Get(eventId2!, 0UL).GetAwaiter().GetResult();

            Assert.That(entry1, Is.Not.Null);
            Assert.That(entry2, Is.Not.Null);
            Assert.That(client1, Is.SameAs(client2), "The client instance should be reused between scopes.");
            Assert.That(client1.IsClosed, Is.False);
            Assert.That(client2.IsClosed, Is.False);

            ImmuDbDataProvider.ResetClientCacheAsync().GetAwaiter().GetResult();

            Assert.That(client1.IsClosed, Is.True);
            Assert.That(client2.IsClosed, Is.True);
        }

        [Test]
        public async Task TestImmuDbDataProvider_ClientCacheAsync()
        {
            var key = Guid.NewGuid().ToByteArray();
            var value = "TestEvent";
            var provider = new ImmuDbDataProvider(c => c
                .Database(DatabaseUnverified)
                .Username(UsernameUnverified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType));
            var scope1 = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });
            await scope1.DisposeAsync();

            var eventId1 = scope1.EventId as byte[];
            var client1 = await provider.GetClientAsync(scope1.Event);
            var entry1 = await client1.Get(eventId1!, 0UL);
            var scope2 = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });
            await scope2.DisposeAsync();
            var eventId2 = scope2.EventId as byte[];
            var client2 = await provider.GetClientAsync(scope2.Event);
            var entry2 = await client2.Get(eventId2!, 0UL);

            Assert.That(entry1, Is.Not.Null);
            Assert.That(entry2, Is.Not.Null);
            Assert.That(client1, Is.SameAs(client2), "The client instance should be reused between scopes.");
            Assert.That(client1.IsClosed, Is.False);
            Assert.That(client2.IsClosed, Is.False);

            await ImmuDbDataProvider.ResetClientCacheAsync();

            Assert.That(client1.IsClosed, Is.True);
            Assert.That(client2.IsClosed, Is.True);
        }

        [Test]
        public void TestImmuDbDataProvider_Expiration()
        {
            // Arrange
            var key = "key_1";
            var value = "TestEvent_1";

            var provider = new ImmuDbDataProvider(c => c
                .Database(DatabaseVerified)
                .Username(UsernameVerified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(true)
                .ExpirationTimeout(TimeSpan.FromSeconds(3)));

            // Act
            var scope = AuditScope.Create(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });

            var client = provider.GetClientAsync(scope.Event).GetAwaiter().GetResult();

            scope.Dispose();

            var eventId = (byte[])scope.EventId;

            var entry = client.VerifiedGet(eventId!).GetAwaiter().GetResult();

            Thread.Sleep(3000);

            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await client.VerifiedGet(eventId);
            });

            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
        }

        [Test]
        public async Task TestImmuDbDataProvider_ExpirationAsync()
        {
            // Arrange
            var key = "key_1";
            var value = "TestEvent_1";

            var provider = new ImmuDbDataProvider(c => c
                .Database(DatabaseVerified)
                .Username(UsernameVerified)
                .Password(Password)
                .ClientBuilder(b => b.WithServerUrl(ServerUrl))
                .KeySelector(_ => key)
                .ValueSelector(ev => ev.EventType)
                .UseVerifiedMethods(true)
                .ExpirationTimeout(TimeSpan.FromSeconds(3)));
            
            // Act
            var scope = await AuditScope.CreateAsync(new AuditScopeOptions()
            {
                DataProvider = provider,
                CreationPolicy = EventCreationPolicy.InsertOnEnd,
                EventType = value
            });
            
            var client = await provider.GetClientAsync(scope.Event);

            await scope.DisposeAsync();

            var eventId = (byte[])scope.EventId;
            
            var entry = await client.VerifiedGet(eventId!);

            await Task.Delay(3000);
            
            Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await client.VerifiedGet(eventId);
            });

            // Assert
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That(entry.Value, Is.EqualTo(Encoding.UTF8.GetBytes(value)));
        }
    }
}