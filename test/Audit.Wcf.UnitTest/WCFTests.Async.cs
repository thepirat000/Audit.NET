using Audit.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Text.Json;
using Audit.IntegrationTest;
using Polly;

namespace Audit.WCF.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.MonoIncompatible)]
    public class WCFTests_Async
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [Test]
        public async Task WCFTest_CreationPolicy_InsertOnStartReplaceOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            var basePipeAddress =
                new Uri(string.Format(@"http://localhost:{0}/test/", 10001 + new Random().Next(1, 9999)));
            string response;
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    response = await proxy.TestAsync(1000);
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.That(response, Is.EqualTo("1000"));
            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(1));
            Assert.That(inserted[0].WcfEvent.Result, Is.Null);
            Assert.That(replaced[0].WcfEvent.Result.Value.ToString(), Is.EqualTo("1000"));
            Assert.That(replaced[0].Duration >= 1000 && replaced[0].Duration < 2000, Is.True);
        }

        [Test]
        public async Task WCFTest_CreationPolicy_InsertOnStartInsertOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);
            var basePipeAddress =
                new Uri(string.Format(@"http://localhost:{0}/test/", 10002 + new Random().Next(1, 9999)));
            string response;
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    response = await proxy.TestAsync(10);
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.That(response, Is.EqualTo("10"));
            Assert.That(inserted.Count, Is.EqualTo(2));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].WcfEvent.Result, Is.Null);
            Assert.That(inserted[1].WcfEvent.Result.Value.ToString(), Is.EqualTo("10"));
        }
        
        [Test]
        public async Task WCFTest_CreationPolicy_InsertOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            var basePipeAddress =
                new Uri(string.Format(@"http://localhost:{0}/test/", 10003 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    await proxy.TestAsync(5);
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].WcfEvent.Result.Value.ToString(), Is.EqualTo("5"));
            Assert.That(inserted[0].WcfEvent.IsAsync, Is.EqualTo(true));
        }

        [Test]
        public async Task WCFTest_CreationPolicy_Manual_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.Manual);
            var basePipeAddress =
                new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    await proxy.TestAsync(15);
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
        }

        [Category(TestCommon.Category.Integration)]
        [TestCase(2, 10)]
        [TestCase(6, 10)]
        public async Task WCFTest_Concurrency_AuditScopeAsync(int threads, int callsPerThread)
        {
            var provider = new Mock<IAuditDataProvider>();
            provider.Setup(p => p.CloneValue(It.IsAny<object>(), It.IsAny<AuditEvent>())).CallBase();
            var bag = new ConcurrentBag<string>();
            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                var wcfEvent = ev.GetWcfAuditAction();
                var request = wcfEvent.InputParameters[0].Value as GetOrderRequest;
                var result = wcfEvent.Result.Value as GetOrderResponse;
                Assert.NotNull(request.OrderId);
                Assert.That(ev.CustomFields["Test-Field-1"], Is.EqualTo(request.OrderId));
                Assert.False(bag.Contains(request.OrderId));
                bag.Add(request.OrderId);
                Assert.That(ev.CustomFields["Test-Field-2"], Is.EqualTo(ev.CustomFields["Test-Field-1"]));
                Assert.That(result.Order.OrderId, Is.EqualTo(request.OrderId));
                Assert.That(ev.Environment.CallingMethodName.Contains("GetOrder"), Is.True);
                return Guid.NewGuid();
            });

            var auditScopeFactory = new TestAuditScopeFactory();

            Audit.Core.Configuration.AuditScopeFactory = auditScopeFactory;

            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(new OrderService_AsyncConcurrent_Test(provider.Object, auditScopeFactory), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();

                await WCFTest_Concurrency_AuditScope_ThreadRunAsync(threads, callsPerThread, serviceEndpoint.Address);

                host.Close();
            }
            Console.WriteLine("Times: {0}.", threads * callsPerThread);
            Assert.That(bag.Count, Is.EqualTo(bag.Distinct().Count()));
            Assert.That(bag.Count, Is.EqualTo(threads * callsPerThread));
            Assert.That(auditScopeFactory.OnScopeCreatedCount, Is.EqualTo(threads * callsPerThread));
        }

        static async Task WCFTest_Concurrency_AuditScope_ThreadRunAsync(int internalThreads, int callsPerThread, EndpointAddress address)
        {
            var tasks = new List<ValueTask>();

            var rateLimiter = new ResiliencePipelineBuilder()
                .AddConcurrencyLimiter(8, internalThreads)
                .Build();

            for (int i = 0; i < internalThreads; i++)
            {
                tasks.Add(rateLimiter.ExecuteAsync(async ct =>
                {
                    using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                    {
                        for (int j = 0; j < callsPerThread; j++)
                        {
                            var id = Guid.NewGuid();
                            var proxy = factory.CreateChannel(address);
                            await proxy.GetOrder2Async(new GetOrderRequest() { OrderId = id.ToString() });
                        }
                        factory.Close();
                    }
                }));
            }

            await Task.WhenAll(tasks.Select(t => t.AsTask()).ToArray());
        }
        private static BasicHttpBinding CreateBinding()
        {
            var binding = new BasicHttpBinding()
            {
                ReceiveTimeout = TimeSpan.FromSeconds(55),
                SendTimeout = TimeSpan.FromSeconds(55)
            };

            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            return binding;
        }
    }
}