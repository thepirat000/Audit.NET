using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Audit.Core;
using Audit.IntegrationTest;
using NUnit.Framework;


namespace Audit.WCF.UnitTest
{
    [TestFixture]
    [Category(TestCommon.Category.MonoIncompatible)]
    public class WCFTests
    {
        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Assert.Ignore("WCF is unsupported on non-Windows platforms.");
            }
        }

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
        private IAuditDataProvider _auditDataProvider;

        public OrderService_AsyncConcurrent_Test(IAuditDataProvider dp, IAuditScopeFactory auditScopeFactory)
        {
            _auditDataProvider = dp;
            _auditScopeFactory = auditScopeFactory;
        }

        public IAuditDataProvider AuditDataProvider => _auditDataProvider;
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
        public int OnScopeCreatedCount;
        private static object locker = new object();

        public override void OnScopeCreated(AuditScope auditScope)
        {
            lock (locker)
            {
                OnScopeCreatedCount++;
            }
        }
    }
}