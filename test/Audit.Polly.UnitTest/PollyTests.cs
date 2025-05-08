using System.Threading.Tasks;
using System.Threading;
using System;
using Audit.Core;
using Audit.Core.Providers;
using Audit.Polly;
using Audit.Polly.Providers;
using Moq;
using NUnit.Framework;
using Polly;
using System.Collections.Generic;

namespace Audit.UnitTest
{
    public class PollyTests
    {
        [SetUp]
        public void Setup()
        {
            FailingDataProvider.ConstructorCount = 0;
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public void Test_Retry_With_Fallback()
        {
            // Arrange
            var fallbackDataProvider = new Mock<IAuditDataProvider>();

            var predicateBuilder = new PredicateBuilder().Handle<Exception>();

            var primaryDataProvider = new FailingDataProvider(3);

            // Act
            var pollyDataProvider = new PollyDataProvider(cfg => cfg
                .DataProvider(primaryDataProvider)
                .WithResilience(r => r
                    .AddFallback(new()
                    {
                        ShouldHandle = predicateBuilder,
                        FallbackAction = args => args.FallbackToDataProvider(fallbackDataProvider.Object)
                    })
                    .AddRetry(new()
                    {
                        ShouldHandle = predicateBuilder,
                        MaxRetryAttempts = 2,
                        Delay = TimeSpan.Zero
                    })));

            pollyDataProvider.InsertEvent(new AuditEvent());
            pollyDataProvider.InsertEventAsync(new AuditEvent()).GetAwaiter().GetResult();

            pollyDataProvider.ReplaceEvent(1, new AuditEvent());
            pollyDataProvider.ReplaceEventAsync(1, new AuditEvent()).GetAwaiter().GetResult();

            // Assert
            Assert.That(FailingDataProvider.ConstructorCount, Is.EqualTo(1));

            Assert.That(primaryDataProvider.FailCountInsert, Is.EqualTo(3));
            Assert.That(primaryDataProvider.FailCountInsertAsync, Is.EqualTo(3));
            Assert.That(primaryDataProvider.FailCountReplace, Is.EqualTo(3));
            Assert.That(primaryDataProvider.FailCountReplaceAsync, Is.EqualTo(3));

            Assert.That(primaryDataProvider.SuccessCountInserted, Is.Zero);
            Assert.That(primaryDataProvider.SuccessCountInsertedAsync, Is.Zero);
            Assert.That(primaryDataProvider.SuccessCountReplaced, Is.Zero);
            Assert.That(primaryDataProvider.SuccessCountReplacedAsync, Is.Zero);

            fallbackDataProvider.Verify(x => x.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            fallbackDataProvider.Verify(x => x.InsertEventAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
            fallbackDataProvider.Verify(x => x.ReplaceEvent(It.IsAny<object>(), It.IsAny<AuditEvent>()), Times.Once);
            fallbackDataProvider.Verify(
                x => x.ReplaceEventAsync(It.IsAny<object>(), It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void Test_Hedging()
        {
            var primaryDataProvider = new FailingDataProvider(2);
            var anotherDataProvider = new FailingDataProvider(1);

            Audit.Core.Configuration.Setup()
                .UsePolly(p => p
                    .DataProvider(primaryDataProvider)
                    .WithResilience(r => r
                        .AddHedging(new()
                        {
                            ShouldHandle = new PredicateBuilder().Handle<ArithmeticException>(),
                            MaxHedgedAttempts = 2,
                            Delay = TimeSpan.Zero,
                            ActionGenerator = args => args.FallbackToDataProvider(anotherDataProvider)
                        })))
                .WithInsertOnEndCreationPolicy();

            for (int i = 0; i < 10; i++)
            {
                var scope = AuditScope.Create($"Test_{i}", null, new { index = i });
                scope.Dispose();
            }

            var e1 = primaryDataProvider.GetAllEvents();
            var e2 = anotherDataProvider.GetAllEvents();

            Assert.That(e1.Count, Is.EqualTo(8));
            Assert.That(e2.Count, Is.EqualTo(2));
            Assert.That(primaryDataProvider.FailCountInsert, Is.EqualTo(2));
            Assert.That(anotherDataProvider.FailCountInsert, Is.EqualTo(1));
        }

        [Test]
        public void Test_Hedging_To_Primary()
        {
            var primaryDataProvider = new FailingDataProvider(2);

            Audit.Core.Configuration.Setup()
                .UsePolly(p => p
                    .DataProvider(primaryDataProvider)
                    .WithResilience(r => r
                        .AddHedging(new()
                        {
                            ShouldHandle = new PredicateBuilder().Handle<ArithmeticException>(),
                            MaxHedgedAttempts = 2,
                            Delay = TimeSpan.Zero
                        })))
                .WithInsertOnEndCreationPolicy();

            for (int i = 0; i < 10; i++)
            {
                var scope = AuditScope.Create($"Test_{i}", null, new { index = i });
                scope.Dispose();
            }

            var e1 = primaryDataProvider.GetAllEvents();

            Assert.That(e1.Count, Is.EqualTo(10));
            Assert.That(primaryDataProvider.FailCountInsert, Is.EqualTo(2));
            Assert.That(primaryDataProvider.SuccessCountInserted, Is.EqualTo(10));
        }

        [Test]
        public void Test_Fallback_OnFallback_GetAuditEvent()
        {
            var events = new List<AuditEvent>();
            var primaryDataProvider = new FailingDataProvider(10);

            Audit.Core.Configuration.Setup()
                .UsePolly(p => p
                    .DataProvider(primaryDataProvider)
                    .WithResilience(r => r
                        .AddFallback(new()
                        {
                            ShouldHandle = new PredicateBuilder().Handle<ArithmeticException>(),
                            FallbackAction = args => default,
                            OnFallback = args =>
                            {
                                events.Add(args.Context.GetAuditEvent());

                                return default;
                            }
                        })))
                .WithInsertOnEndCreationPolicy();
            
            AuditScope.Log("Test", new { Test = 1 });
            
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].EventType, Is.EqualTo("Test"));

            Assert.That(primaryDataProvider.FailCountInsert, Is.EqualTo(1));
            Assert.That(primaryDataProvider.SuccessCountInserted, Is.Zero);
        }

        public class DelayedDataProvider : InMemoryDataProvider
        {
            public int _delayMs;

            public DelayedDataProvider(int delayMs)
            {
                _delayMs = delayMs;
            }

            public override object InsertEvent(AuditEvent auditEvent)
            {
                Thread.Sleep(_delayMs);
                return base.InsertEvent(auditEvent);
            }

            public override async Task<object> InsertEventAsync(AuditEvent auditEvent,
                CancellationToken cancellationToken = default)
            {
                await Task.Delay(_delayMs);
                return await base.InsertEventAsync(auditEvent, cancellationToken);
            }
        }
    }

    public class FailingDataProvider : InMemoryDataProvider
    {
        public static int ConstructorCount { get; set; }
        public int FailCountInsert { get; set; }
        public int FailCountInsertAsync { get; set; }
        public int FailCountReplace { get; set; }
        public int FailCountReplaceAsync { get; set; }

        public int SuccessCountInserted { get; set; }
        public int SuccessCountReplaced { get; set; }
        public int SuccessCountInsertedAsync { get; set; }
        public int SuccessCountReplacedAsync { get; set; }

        private int _maxFails;

        public FailingDataProvider(int maxFails)
        {
            ConstructorCount++;
            _maxFails = maxFails;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            if (FailCountInsert < _maxFails)
            {
                FailCountInsert++;
                throw new ArithmeticException("Test");
            }

            SuccessCountInserted++;
            return base.InsertEvent(auditEvent);
        }

        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            if (FailCountReplace < _maxFails)
            {
                FailCountReplace++;
                throw new ArithmeticException("Test");
            }

            SuccessCountReplaced++;

            base.ReplaceEvent(eventId, auditEvent);
        }

        public override Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (FailCountInsertAsync < _maxFails)
            {
                FailCountInsertAsync++;
                throw new ArithmeticException("Test");
            }

            SuccessCountInsertedAsync++;
            return base.InsertEventAsync(auditEvent, cancellationToken);
        }

        public override Task ReplaceEventAsync(object eventId, AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            if (FailCountReplaceAsync < _maxFails)
            {
                FailCountReplaceAsync++;
                throw new ArithmeticException("Test");
            }

            SuccessCountReplacedAsync++;
            
            return base.ReplaceEventAsync(eventId, auditEvent, cancellationToken);
        }
    }
    
}