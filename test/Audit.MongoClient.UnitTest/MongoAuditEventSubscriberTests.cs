using System;
using System.Net;
using Audit.Core;
using Audit.Core.Providers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;
using NUnit.Framework;

namespace Audit.MongoClient.UnitTest
{
    [TestFixture]
    public class MongoAuditEventSubscriberTests
    {
        private readonly ConnectionId _connectionId = new ConnectionId(new ServerId(new ClusterId(10), new IPEndPoint(IPAddress.Parse("192.168.1.100"), 81)), 20);
        
        [SetUp]
        public void Setup()
        {
            Configuration.Reset();
        }

        [Test]
        public void Test_Command_Succeeded()
        {
            // Arrange
            var dp = new InMemoryDataProvider();
            var sut = new MongoAuditEventSubscriber(cfg => cfg.IncludeReply().EventType("test").AuditDataProvider(dp));

            var testCommandName = $"name-{Guid.NewGuid()}";
            var testCommandValue = $"value-{Guid.NewGuid()}";
            var replyTest = $"reply-{Guid.NewGuid()}";
            var requestId = new Random().Next();

            var cmdStart = new CommandStartedEvent(testCommandName, new { cmd = testCommandValue }.ToBsonDocument(), DatabaseNamespace.Admin, operationId: 1, requestId, _connectionId);
            var cmdSuccess = new CommandSucceededEvent(testCommandName, new { value = replyTest }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId, _connectionId, TimeSpan.FromSeconds(1));

            // Act
            sut.Handle(cmdStart);
            sut.Handle(cmdSuccess);

            // Assert
            var evs = dp.GetAllEventsOfType<AuditEventMongoCommand>();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Command.CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.Success, Is.True);
            Assert.That(evs[0].Command.CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.GetCommandStartedEvent().CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.Duration, Is.EqualTo(1000));
            Assert.That(evs[0].Command.Error, Is.Null);
            Assert.That(evs[0].Command.OperationId, Is.EqualTo(1));
            Assert.That(evs[0].Command.Reply, Is.Not.Null);
            Assert.That(((dynamic)evs[0].Command.Reply)["value"], Is.EqualTo(replyTest));
            Assert.That(evs[0].Command.RequestId, Is.EqualTo(requestId));
            Assert.That(evs[0].Command.Body, Is.Not.Null);
            Assert.That(((dynamic)evs[0].Command.Body)["cmd"], Is.EqualTo(testCommandValue));
            Assert.That(evs[0].Command.Connection.ClusterId, Is.EqualTo(10));
            Assert.That(evs[0].Command.Connection.Endpoint, Is.EqualTo(_connectionId.ServerId.EndPoint.ToString()));
            Assert.That(evs[0].Command.Connection.LocalConnectionId, Is.EqualTo(_connectionId.LongLocalValue));
        }

        [Test]
        public void Test_Command_Failure()
        {
            // Arrange
            Configuration.Setup().UseInMemoryProvider();

            var sut = new MongoAuditEventSubscriber(cfg => cfg
                .IncludeReply(_ => true)
                .EventType(_ => "test")
                .CommandFilter(_ => true)
                .AuditScopeFactory(new AuditScopeFactory())
                .CreationPolicy(EventCreationPolicy.InsertOnEnd));

            var cnnId = new ConnectionId(new ServerId(new ClusterId(10), new IPEndPoint(IPAddress.Parse("192.168.1.100"), 81)), 20);
            var testCommandName = $"name-{Guid.NewGuid()}";
            var testCommandValue = $"value-{Guid.NewGuid()}";
            var requestId = new Random().Next();

            var cmdStart = new CommandStartedEvent(testCommandName, new { cmd = testCommandValue }.ToBsonDocument(), DatabaseNamespace.Admin, operationId: 1, requestId, cnnId);

            var exception = new MongoCommandException(cnnId, "test-exception", cmdStart.Command);
            var cmdFailed = new CommandFailedEvent(testCommandName, DatabaseNamespace.Admin, exception, 1, requestId, cnnId, TimeSpan.FromSeconds(1));

            // Act
            sut.Handle(cmdStart);
            sut.Handle(cmdFailed);
            
            // Assert
            var evs = Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();

            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Command.CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.Success, Is.False);
            Assert.That(evs[0].Command.CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.GetCommandStartedEvent().CommandName, Is.EqualTo(cmdStart.CommandName));
            Assert.That(evs[0].Command.Duration, Is.EqualTo(1000));
            Assert.That(evs[0].Command.Error, Is.Not.Null);
            Assert.That(evs[0].Command.Error, Contains.Substring("test-exception"));
            Assert.That(evs[0].Command.OperationId, Is.EqualTo(1));
            Assert.That(evs[0].Command.Reply, Is.Null);
            Assert.That(evs[0].Command.RequestId, Is.EqualTo(requestId));
            Assert.That(evs[0].Command.Body, Is.Not.Null);
            Assert.That(((dynamic)evs[0].Command.Body)["cmd"], Is.EqualTo(testCommandValue));
            Assert.That(evs[0].Command.Connection.ClusterId, Is.EqualTo(10));
            Assert.That(evs[0].Command.Connection.Endpoint, Is.EqualTo(_connectionId.ServerId.EndPoint.ToString()));
            Assert.That(evs[0].Command.Connection.LocalConnectionId, Is.EqualTo(_connectionId.LongLocalValue));
        }

        [TestCase(EventCreationPolicy.InsertOnEnd, 1)]
        [TestCase(EventCreationPolicy.InsertOnStartInsertOnEnd, 2)]
        [TestCase(EventCreationPolicy.InsertOnStartReplaceOnEnd, 1)]
        [TestCase(EventCreationPolicy.Manual, 0)]
        public void Test_Creation_Policy(EventCreationPolicy policy, int expectedEventsCount)
        {
            // Arrange
            Configuration.Setup().UseInMemoryProvider();

            var sut = new MongoAuditEventSubscriber()
            {
                CreationPolicy = policy
            };

            var requestId = new Random().Next();

            var cmdStart = new CommandStartedEvent("test", new { cmd = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId,  _connectionId);
            var cmdSuccess = new CommandSucceededEvent("test", new { value = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId, _connectionId, TimeSpan.FromSeconds(1));

            // Act
            sut.Handle(cmdStart);
            sut.Handle(cmdSuccess);

            // Assert
            var evs = Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();
            
            Assert.That(evs, Is.Not.Null);
            Assert.That(evs.Count, Is.EqualTo(expectedEventsCount));
            if (expectedEventsCount > 0)
            {
                Assert.That(evs[0].Command.RequestId, Is.EqualTo(requestId));
            }
        }

        [Test]
        public void Test_Custom_EventTypeName()
        {
            // Arrange
            Configuration.Setup().UseNullProvider();

            var commandName = Guid.NewGuid().ToString();

            var sut = new MongoAuditEventSubscriber()
            {
                EventType = "test-{command}"
            };

            var requestId = new Random().Next();
            var cmdStart = new CommandStartedEvent(commandName, new { cmd = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId, _connectionId);

            // Act
            sut.Handle(cmdStart);

            // Assert
            Assert.That(sut._requestBuffer[requestId].Event.EventType, Is.EqualTo($"test-{commandName}"));
        }

        [Test]
        public void Test_Custom_AuditDataProvider()
        {
            // Arrange
            Configuration.Setup().UseNullProvider();

            var commandName = Guid.NewGuid().ToString();

            var sut = new MongoAuditEventSubscriber()
            {
                AuditDataProvider = new InMemoryDataProvider()
            };

            var requestId = new Random().Next();
            var cmdStart = new CommandStartedEvent(commandName, new { cmd = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId, _connectionId);

            // Act
            sut.Handle(cmdStart);

            // Assert
            Assert.That(sut._requestBuffer[requestId].DataProvider, Is.InstanceOf<InMemoryDataProvider>());
        }

        [Test]
        public void Test_Custom_AuditScopeFactory()
        {
            // Arrange
            Configuration.Setup().UseNullProvider();

            var eventType = Guid.NewGuid().ToString();
            var commandName = Guid.NewGuid().ToString();

            var mockFactory = new Mock<IAuditScopeFactory>(MockBehavior.Strict);
            mockFactory.Setup(x => x.Create(It.IsAny<AuditScopeOptions>()))
                .Returns(AuditScope.Create(new AuditScopeOptions() { EventType = eventType }));

            var sut = new MongoAuditEventSubscriber()
            {
                AuditScopeFactory = mockFactory.Object
            };

            var requestId = new Random().Next();
            var cmdStart = new CommandStartedEvent(commandName, new { cmd = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, requestId, _connectionId);

            // Act
            sut.Handle(cmdStart);

            // Assert
            Assert.That(sut._requestBuffer[requestId].EventType, Is.EqualTo(eventType));
            mockFactory.Verify(x => x.Create(It.IsAny<AuditScopeOptions>()), Times.Once);
        }

        [Test]
        public void Test_Custom_CommandFilter()
        {
            // Arrange
            Configuration.Setup().UseInMemoryProvider();

            var sut = new MongoAuditEventSubscriber()
            {
                CommandFilter = cmd => cmd.CommandName == "delete"
            };

            var commandName1 = Guid.NewGuid().ToString();
            var commandName2 = "delete";
            
            var cmdStart1 = new CommandStartedEvent(commandName1, new { cmd = 10 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId);
            var cmdStart2 = new CommandStartedEvent(commandName2, new { cmd = 20 }.ToBsonDocument(), DatabaseNamespace.Admin, 3, 4, _connectionId);
            var cmdSuccess1 = new CommandSucceededEvent(commandName1, new { value = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId, TimeSpan.FromSeconds(1));
            var cmdSuccess2 = new CommandSucceededEvent(commandName2, new { value = 2 }.ToBsonDocument(), DatabaseNamespace.Admin, 3, 4, _connectionId, TimeSpan.FromSeconds(1));

            // Act
            sut.Handle(cmdStart1);
            sut.Handle(cmdStart2);
            sut.Handle(cmdSuccess1);
            sut.Handle(cmdSuccess2);

            // Assert
            var evs = Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();
            Assert.That(evs, Is.Not.Null);
            Assert.That(evs.Count, Is.EqualTo(1));
            Assert.That(evs[0].Command.CommandName, Is.EqualTo("delete"));
        }

        [Test]
        public void Test_IgnoredCommands()
        {
            // Arrange
            Configuration.Setup().UseInMemoryProvider();

            var ignoredCommands = new[] { "isMaster", "buildInfo", "getLastError", "saslStart", "saslContinue" };

            var sut = new MongoAuditEventSubscriber();

            // Act
            foreach (var ignoredCommandName in ignoredCommands)
            {
                var cmdStart = new CommandStartedEvent(ignoredCommandName, new { cmd = 10 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId);
                var cmdFailed = new CommandFailedEvent(ignoredCommandName, DatabaseNamespace.Admin, new Exception(), 1, 2, _connectionId, TimeSpan.FromSeconds(1));
                var cmdSuccess = new CommandSucceededEvent(ignoredCommandName, new { value = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId, TimeSpan.FromSeconds(1));

                sut.Handle(cmdStart);
                sut.Handle(cmdFailed);
                sut.Handle(cmdSuccess);
            }

            // Assert
            var evs = Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();
            Assert.That(evs, Is.Not.Null);
            Assert.That(evs.Count, Is.EqualTo(0));
        }

        [Test]
        public void Test_AuditDisabled()
        {
            // Arrange
            Configuration.Setup().UseInMemoryProvider();
            Configuration.AuditDisabled = true;
            var sut = new MongoAuditEventSubscriber();

            var cmdStart = new CommandStartedEvent("insert", new { cmd = 10 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId);
            var cmdFailed = new CommandFailedEvent("insert", DatabaseNamespace.Admin, new Exception(), 1, 2, _connectionId, TimeSpan.FromSeconds(1));
            var cmdSuccess = new CommandSucceededEvent("insert", new { value = 1 }.ToBsonDocument(), DatabaseNamespace.Admin, 1, 2, _connectionId, TimeSpan.FromSeconds(1));

            // Act
            sut.Handle(cmdStart);
            sut.Handle(cmdFailed);
            sut.Handle(cmdSuccess);

            // Assert
            var evs = Configuration.DataProviderAs<InMemoryDataProvider>().GetAllEventsOfType<AuditEventMongoCommand>();
            Assert.That(evs, Is.Not.Null);
            Assert.That(evs.Count, Is.EqualTo(0));
        }
    }
}