﻿#if NET452
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
    public class WCFTests
    {
        [Test]
        [Category("WCF")]
        public void WCFTest_CreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => 
                    {
                        inserted.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) => 
                    {
                        replaced.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);
            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    proxy.DoSomething();
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual(100, replaced[0].WcfEvent.Result.Value);
        }

        [Test]
        [Category("WCF")]
        public void WCFTest_CreationPolicy_InsertOnStartInsertOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartInsertOnEnd);
            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    proxy.DoSomething();
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual(100, inserted[1].WcfEvent.Result.Value);
        }

        [Test]
        [Category("WCF")]
        public void WCFTest_CreationPolicy_InsertOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnEnd);
            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    proxy.DoSomething();
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual(100, inserted[0].WcfEvent.Result.Value);
            Assert.AreEqual(false, inserted[0].WcfEvent.IsAsync);
        }

        [Test]
        [Category("WCF")]
        public void WCFTest_CreationPolicy_Manual()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonConvert.DeserializeObject<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    }))
                .WithCreationPolicy(EventCreationPolicy.Manual);
            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(typeof(OrderService), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();
                using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                {
                    var proxy = factory.CreateChannel(serviceEndpoint.Address);
                    proxy.DoSomething();
                }
                host.Close();
            }
            Thread.Sleep(100);

            Assert.AreEqual(0, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
        }

        private static object Locker = new object();
        
        [Test]
        [Category("WCF")]
        public void WCFTest_AuditScope()
        {
            lock (Locker)
            {
                WCFTest_Concurrency_AuditScope(1, 1);
            }
        }

        [Test]
        [Category("WCF")]
        public void WCFTest_Concurrency_AuditScope()
        {
            lock (Locker)
            {
                WCFTest_Concurrency_AuditScope(1, 1);
            }
            lock (Locker)
            {
                WCFTest_Concurrency_AuditScope(5, 10);
            }
            lock (Locker)
            {
                WCFTest_Concurrency_AuditScope(5, 25);
            }
        }
        
        public void WCFTest_Concurrency_AuditScope(int threads, int callsPerThread)
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
                Assert.IsTrue(ev.Environment.CallingMethodName.Contains("GetOrder()"));
                return Guid.NewGuid();
            });

            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(new OrderService_AsyncConcurrent_Test(provider.Object), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();

                WCFTest_Concurrency_AuditScope_ThreadRun(threads, callsPerThread, serviceEndpoint.Address);

                host.Close();
            }
            Console.WriteLine("Times: {0}.", threads * callsPerThread);
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

        static void WCFTest_Concurrency_AuditScope_ThreadRun(int internalThreads, int callsPerThread, EndpointAddress address)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < internalThreads; i++)
            {
                tasks.Add(Task.Factory.StartNew(() =>
                {
                    using (var factory = new ChannelFactory<IOrderService>(CreateBinding()))
                    {
                        for (int j = 0; j < callsPerThread; j++)
                        {
                            var id = Guid.NewGuid();
                            var proxy = factory.CreateChannel(address);
                            var response = proxy.GetOrder(new GetOrderRequest() { OrderId = id.ToString() });
                        }
                        factory.Close();
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }

    [ServiceContract]
    public interface IOrderService
    {
        [OperationContract]
        GetOrderResponse GetOrder(GetOrderRequest request);
        [OperationContract]
        Task<GetOrderResponse> GetOrder2Async(GetOrderRequest request);
        [OperationContract]
        int DoSomething();
        [OperationContract]
        Task<string> TestAsync(int sleep);
    }

    [DataContract]
    public class GetOrderRequest
    {
        [DataMember]
        public string OrderId { get; set; }
    }
    [DataContract]
    public class GetOrderResponse
    {
        [DataMember]
        public bool Success { get; set; }
        [DataMember]
        public List<string> Errors { get; set; }
        [DataMember]
        public Order Order { get; set; }
    }
    [DataContract]
    public class Order
    {
        [DataMember]
        public string OrderId { get; set; }
        [DataMember]
        public string CustomerName { get; set; }
        [DataMember]
        public decimal Total { get; set; }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    public class OrderService_AsyncConcurrent_Test : OrderService, IOrderService
    {
        private AuditDataProvider _auditDataProvider;

        public OrderService_AsyncConcurrent_Test(AuditDataProvider dp)
        {
            _auditDataProvider = dp;
        }

        public AuditDataProvider AuditDataProvider => _auditDataProvider;
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    public class OrderService : IOrderService
    {
        [AuditBehavior]
        public int DoSomething()
        {
            Thread.Sleep(1100);
            return 100;
        }

        [AuditBehavior]
        public GetOrderResponse GetOrder(GetOrderRequest request)
        {
            var rnd = new Random();
            var ctx1 = Audit.WCF.AuditBehavior.CurrentAuditScope;
            Thread.Sleep(rnd.Next(2, 100));
            ctx1.SetCustomField("Test-Field-1", request.OrderId);
            Thread.Sleep(rnd.Next(2, 100));
            var ctx2 = Audit.WCF.AuditBehavior.CurrentAuditScope;
            ctx2.SetCustomField("Test-Field-2", request.OrderId);

            return new GetOrderResponse()
            {
                Success = true,
                Order = new Order() { OrderId = request.OrderId }
            };
        }

        [AuditBehavior]
        public async Task<GetOrderResponse> GetOrder2Async(GetOrderRequest request)
        {
            var rnd = new Random();
            var ctx1 = Audit.WCF.AuditBehavior.CurrentAuditScope;
            ctx1.SetCustomField("Test-Field-1", request.OrderId);
            await Task.Delay(rnd.Next(2, 100));
            var ctx2 = Audit.WCF.AuditBehavior.CurrentAuditScope;
            ctx2.SetCustomField("Test-Field-2", request.OrderId);
            await Task.Delay(rnd.Next(2, 100));
            return new GetOrderResponse()
            {
                Success = true,
                Order = new Order() { OrderId = request.OrderId }
            };
        }

        [AuditBehavior]
        public async Task<string> TestAsync(int sleep)
        {
            await Task.Delay(sleep);
            return sleep.ToString();
        }

    }
}
#endif