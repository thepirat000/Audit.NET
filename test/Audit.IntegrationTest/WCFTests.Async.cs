#if NET452
using Audit.Core;
using Audit.WCF;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Audit.IntegrationTest.Wcf
{
    public class WCFTests_Async
    {
        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_CreationPolicy_InsertOnStartReplaceOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.AreEqual("1000", response);
            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual("1000", replaced[0].WcfEvent.Result.Value);
            Assert.IsTrue(replaced[0].Duration >= 1000 && replaced[0].Duration < 2000);
        }

        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_CreationPolicy_InsertOnStartInsertOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.AreEqual("10", response);
            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual("10", inserted[1].WcfEvent.Result.Value);
        }
        
        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_CreationPolicy_InsertOnEnd_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("5", inserted[0].WcfEvent.Result.Value);
            Assert.AreEqual(true, inserted[0].WcfEvent.IsAsync);
        }

        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_CreationPolicy_Manual_Async()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(
                            JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.AreEqual(0, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
        }

        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_AuditScope_Async()
        {
            await WCFTest_Concurrency_AuditScopeAsync(1000, 1, 1);
        }

        [Test]
        [Category("WCF")]
        [Category("Async")]
        public async Task WCFTest_Concurrency_AuditScope_Async()
        {
            await WCFTest_Concurrency_AuditScopeAsync(10500, 10, 5);
            await WCFTest_Concurrency_AuditScopeAsync(10500, 5, 25);
        }

        public async Task WCFTest_Concurrency_AuditScopeAsync(int portBase, int threads, int callsPerThread)
        {
            var provider = new Mock<AuditDataProvider>();
            provider.Setup(p => p.Serialize(It.IsAny<object>())).CallBase();
            var bag = new ConcurrentBag<string>();

            provider.Setup(p => p.InsertEvent(It.IsAny<AuditEvent>())).Returns((AuditEvent ev) =>
            {
                    var wcfEvent = ev.GetWcfAuditAction();
                    var request = wcfEvent.InputParameters[0].Value as GetOrderRequest;
                    var result = wcfEvent.Result.Value as GetOrderResponse;
                    Assert.NotNull(request.OrderId);
                    Assert.AreEqual(request.OrderId, ev.CustomFields["Test-Field-1"]);
                    Assert.False(bag.Contains(request.OrderId));
                    bag.Add(request.OrderId);
                    Assert.AreEqual(ev.CustomFields["Test-Field-1"], ev.CustomFields["Test-Field-2"]);
                    Assert.AreEqual(request.OrderId, result.Order.OrderId);
                    Assert.IsTrue(ev.Environment.CallingMethodName.Contains("GetOrder2Async()"));
                    return (object)Guid.NewGuid();
                });

            var basePipeAddress =
                new Uri(string.Format(@"http://localhost:{0}/test/", portBase + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(new OrderService_AsyncConcurrent_Test(provider.Object), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();

                await WCFTest_Concurrency_AuditScope_ThreadRun(threads, callsPerThread, serviceEndpoint.Address);

                host.Close();
            }

            await Task.Delay(1000);

            Console.WriteLine("Times: {0}. Bag count: {1}", threads * callsPerThread, bag.Count);
            Assert.AreEqual(bag.Distinct().Count(), bag.Count);
            Assert.AreEqual(threads * callsPerThread, bag.Count);

        }

        private static BasicHttpBinding CreateBinding()
        {
            var binding = new BasicHttpBinding()
            {
                ReceiveTimeout = TimeSpan.FromSeconds(55),
                SendTimeout = TimeSpan.FromSeconds(55)
            };

            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Windows;
            return binding;
        }

        static async Task WCFTest_Concurrency_AuditScope_ThreadRun(int internalThreads, int callsPerThread,
            EndpointAddress address)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < internalThreads; i++)
            {
                tasks.Add(Task.Factory.StartNew(async () =>
                {
                    using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                    {
                        for (int j = 0; j < callsPerThread; j++)
                        {
                            var id = Guid.NewGuid();
                            var proxy = factory.CreateChannel(address);
                            var response = await proxy.GetOrder2Async(new GetOrderRequest() {OrderId = id.ToString()}).ConfigureAwait(false);
                        }
                        factory.Close();
                    }
                }).Unwrap());
            }

            await Task.WhenAll(tasks.ToArray());
        }
    }
}
#endif