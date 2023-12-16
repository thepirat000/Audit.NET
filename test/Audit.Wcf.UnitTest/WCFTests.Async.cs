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

namespace Audit.WCF.UnitTest
{
    public class WCFTests_Async
    {
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

            Assert.AreEqual("1000", response);
            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual("1000", replaced[0].WcfEvent.Result.Value.ToString());
            Assert.IsTrue(replaced[0].Duration >= 1000 && replaced[0].Duration < 2000);
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

            Assert.AreEqual("10", response);
            Assert.AreEqual(2, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.IsNull(inserted[0].WcfEvent.Result);
            Assert.AreEqual("10", inserted[1].WcfEvent.Result.Value.ToString());
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

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
            Assert.AreEqual("5", inserted[0].WcfEvent.Result.Value.ToString());
            Assert.AreEqual(true, inserted[0].WcfEvent.IsAsync);
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

            Assert.AreEqual(0, inserted.Count);
            Assert.AreEqual(0, replaced.Count);
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
    }
}