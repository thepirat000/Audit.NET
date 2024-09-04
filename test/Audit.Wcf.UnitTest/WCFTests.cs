using Audit.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;


namespace Audit.WCF.UnitTest
{
    public class WCFTests
    {
        [Test]
        public void WCFTest_CreationPolicy_InsertOnStartReplaceOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev => 
                    {
                        inserted.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) => 
                    {
                        replaced.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(1));
            Assert.That(inserted[0].WcfEvent.Result, Is.Null);
            Assert.That(replaced[0].WcfEvent.Result.Value.ToString(), Is.EqualTo("100"));
        }

        [Test]
        public void WCFTest_CreationPolicy_InsertOnStartInsertOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.That(inserted.Count, Is.EqualTo(2));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].WcfEvent.Result, Is.Null);
            Assert.That(inserted[1].WcfEvent.Result.Value.ToString(), Is.EqualTo("100"));
        }

        [Test]
        public void WCFTest_CreationPolicy_InsertOnEnd()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(0));
            Assert.That(inserted[0].WcfEvent.Result.Value.ToString(), Is.EqualTo("100"));
            Assert.That(inserted[0].WcfEvent.IsAsync, Is.EqualTo(false));
        }

        [Test]
        public void WCFTest_CreationPolicy_Manual()
        {
            var inserted = new List<AuditEventWcfAction>();
            var replaced = new List<AuditEventWcfAction>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(x => x
                    .OnInsert(ev =>
                    {
                        inserted.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
                    })
                    .OnReplace((evId, ev) =>
                    {
                        replaced.Add(JsonSerializer.Deserialize<AuditEventWcfAction>((ev as AuditEventWcfAction).ToJson()));
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

            Assert.That(inserted.Count, Is.EqualTo(0));
            Assert.That(replaced.Count, Is.EqualTo(0));
        }

        [TestCase(2, 10)]
        [TestCase(6, 10)]
        public void WCFTest_Concurrency_AuditScope(int threads, int callsPerThread)
        {
            var provider = new Mock<AuditDataProvider>();
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
                Assert.That(ev.Environment.CallingMethodName.Contains("GetOrder()"), Is.True);
                return Guid.NewGuid();
            });

            var auditScopeFactory = new TestAuditScopeFactory();

            var basePipeAddress = new Uri(string.Format(@"http://localhost:{0}/test/", 10000 + new Random().Next(1, 9999)));
            using (var host = new ServiceHost(new OrderService_AsyncConcurrent_Test(provider.Object, auditScopeFactory), basePipeAddress))
            {
                var serviceEndpoint = host.AddServiceEndpoint(typeof(IOrderService), CreateBinding(), string.Empty);
                host.Open();

                WCFTest_Concurrency_AuditScope_ThreadRun(threads, callsPerThread, serviceEndpoint.Address);

                host.Close();
            }
            Console.WriteLine("Times: {0}.", threads * callsPerThread);
            Assert.That(bag.Count, Is.EqualTo(bag.Distinct().Count()));
            Assert.That(bag.Count, Is.EqualTo(threads * callsPerThread));
            Assert.That(auditScopeFactory.OnScopeCreatedCount, Is.EqualTo(threads * callsPerThread));

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
        private IAuditScopeFactory _auditScopeFactory;
        private AuditDataProvider _auditDataProvider;

        public OrderService_AsyncConcurrent_Test(AuditDataProvider dp, IAuditScopeFactory auditScopeFactory)
        {
            _auditDataProvider = dp;
            _auditScopeFactory = auditScopeFactory;
        }

        public AuditDataProvider AuditDataProvider => _auditDataProvider;
        public IAuditScopeFactory AuditScopeFactory => _auditScopeFactory;
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
    
    public class TestAuditScopeFactory : AuditScopeFactory
    {
        public int OnScopeCreatedCount { get; set; }

        public override void OnScopeCreated(AuditScope auditScope)
        {
            OnScopeCreatedCount++;
        }
    }
}