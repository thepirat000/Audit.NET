using Audit.Core;
using Audit.Wcf.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Microsoft.Extensions.Logging;
#if NETCOREAPP3_1
using Microsoft.AspNetCore.Hosting;
using CoreWCF.Configuration;
#endif

namespace Audit.Wcf.UnitTest
{
    public class WcfClientTests
    {
        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        
        [OneTimeSetUp]
        public void Setup()
        {
#if NET462_OR_GREATER
            ServiceHost host = new ServiceHost(typeof(CatalogService));
            host.Open();
#elif NETCOREAPP3_1
            IWebHost host = CreateWebHostBuilder().Build();
            host.RunAsync(_cancellationToken.Token);
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _cancellationToken.Cancel();
        }

#if NETCOREAPP3_1
        public static IWebHostBuilder CreateWebHostBuilder() =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                .UseKestrel(options => {
                    options.ListenAnyIP(Startup.HTTP_PORT);
                })
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Warning))
                .UseNetTcp(Startup.NETTCP_PORT)
                .UseStartup<Startup>();
#endif

        [Test]
        public void Test_WcfClient_Success()
        {
            var inserted = new List<AuditEvent>();
            var replaced = new List<AuditEvent>();
            var idsReplaced = new List<object>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson(ev.ToJson())))
                    .OnReplace((id, ev) => { replaced.Add(AuditEvent.FromJson(ev.ToJson())); idsReplaced.Add(id); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var channel = GetServiceProxy(out ICatalogService svc);

            using (var scope = new OperationContextScope(channel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = new HttpRequestMessageProperty()
                {
                    Headers =
                    {
                        { "CustomHeader", Environment.UserName }
                    }
                };
                try
                {
                    var r = svc.InsertProduct("001", new Product() { Name = "test name", Price = 12.34M });
                }
                catch (FaultException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("internal error"));
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
            Assert.AreEqual(1, idsReplaced.Count);
            var actionInserted = inserted[0].GetWcfClientAction();
            var actionReplaced = replaced[0].GetWcfClientAction();
            Assert.IsNotNull(actionInserted);
            Assert.IsNotNull(actionReplaced);
            Assert.IsTrue(actionInserted.Action.Contains("InsertProduct"));
            Assert.IsTrue(actionReplaced.Action.Contains("InsertProduct"));

            Assert.IsTrue(inserted[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"));
            Assert.IsTrue(replaced[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"));

            Assert.IsNull(inserted[0].EndDate);
            Assert.IsNotNull(replaced[0].EndDate);

            Assert.IsTrue(actionInserted.RequestBody.Contains("test name") && actionInserted.RequestBody.Contains("001"));
            Assert.IsTrue(actionReplaced.RequestBody.Contains("test name") && actionReplaced.RequestBody.Contains("001"));

            Assert.IsNull(actionInserted.ResponseBody);
            Assert.IsTrue(actionReplaced.ResponseBody.Contains("InsertProductResponse"));

            Assert.IsNull(actionInserted.ResponseHeaders);
            Assert.IsTrue(actionReplaced.ResponseHeaders.Count > 0);

            Assert.IsNull(actionInserted.ResponseStatuscode);
            Assert.AreEqual(200, (int)actionReplaced.ResponseStatuscode);

            Assert.IsTrue(actionInserted.RequestHeaders.ContainsKey("CustomHeader"));
            Assert.IsTrue(actionReplaced.RequestHeaders.ContainsKey("CustomHeader"));

            Assert.AreEqual(Environment.UserName, actionInserted.RequestHeaders["CustomHeader"]);
            Assert.AreEqual(Environment.UserName, actionReplaced.RequestHeaders["CustomHeader"]);

            Assert.AreEqual("POST", actionInserted.HttpMethod);
            Assert.AreEqual("POST", actionReplaced.HttpMethod);

            Assert.IsFalse(actionInserted.IsFault);
            Assert.IsFalse(actionReplaced.IsFault);
        }

        [Test]
        public void Test_WcfClient_Fault()
        {
            var inserted = new List<AuditEvent>();
            var replaced = new List<AuditEvent>();
            var idsReplaced = new List<object>();
            Audit.Core.Configuration.Setup()
                .UseDynamicProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson(ev.ToJson())))
                    .OnReplace((id, ev) => { replaced.Add(AuditEvent.FromJson(ev.ToJson())); idsReplaced.Add(id); }))
                .WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var channel = GetServiceProxy(out ICatalogService svc);

            using (var scope = new OperationContextScope(channel))
            {
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = new HttpRequestMessageProperty()
                {
                    Headers =
                    {
                        { "CustomHeader", Environment.UserName }
                    }
                };
                try
                {
                    var r = svc.InsertProduct("002", new Product() { Name = "should fail", Price = -100M });
                }
                catch (FaultException ex)
                {
                    Assert.IsTrue(ex.Message.Contains("internal error"));
                }
            }

            Assert.AreEqual(1, inserted.Count);
            Assert.AreEqual(1, replaced.Count);
            Assert.AreEqual(1, idsReplaced.Count);
            var actionInserted = inserted[0].GetWcfClientAction();
            var actionReplaced = replaced[0].GetWcfClientAction();
            Assert.IsNotNull(actionInserted);
            Assert.IsNotNull(actionReplaced);
            Assert.IsTrue(actionInserted.Action.Contains("InsertProduct"));
            Assert.IsTrue(actionReplaced.Action.Contains("InsertProduct"));

            Assert.IsTrue(inserted[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"));
            Assert.IsTrue(replaced[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"));

            Assert.IsNull(inserted[0].EndDate);
            Assert.IsNotNull(replaced[0].EndDate);

            Assert.IsTrue(actionInserted.RequestBody.Contains("should fail") && actionInserted.RequestBody.Contains("002"));
            Assert.IsTrue(actionReplaced.RequestBody.Contains("should fail") && actionReplaced.RequestBody.Contains("002"));

            Assert.IsNull(actionInserted.ResponseBody);
            Assert.IsTrue(actionReplaced.ResponseBody.Contains("faultcode"));

            Assert.IsNull(actionInserted.ResponseHeaders);
            Assert.IsTrue(actionReplaced.ResponseHeaders.Count > 0);

            Assert.IsNull(actionInserted.ResponseStatuscode);
            Assert.AreEqual(500, (int)actionReplaced.ResponseStatuscode);

            Assert.IsTrue(actionInserted.RequestHeaders.ContainsKey("CustomHeader"));
            Assert.IsTrue(actionReplaced.RequestHeaders.ContainsKey("CustomHeader"));

            Assert.AreEqual(Environment.UserName, actionInserted.RequestHeaders["CustomHeader"]);
            Assert.AreEqual(Environment.UserName, actionReplaced.RequestHeaders["CustomHeader"]);

            Assert.AreEqual("POST", actionInserted.HttpMethod);
            Assert.AreEqual("POST", actionReplaced.HttpMethod);

            Assert.IsFalse(actionInserted.IsFault);
            Assert.IsTrue(actionReplaced.IsFault);
        }

        public static IContextChannel GetServiceProxy(out ICatalogService svc)
        {
#if NET462_OR_GREATER
            var channelFactory = new ChannelFactory<ICatalogService>(new BasicHttpBinding(), new EndpointAddress("http://localhost:8733/Design_Time_Addresses/Audit.Wcf.UnitTest/CatalogService/"));
#else
            var channelFactory = new ChannelFactory<ICatalogService>(new BasicHttpBinding(), new EndpointAddress("http://localhost:8733/CatalogService/basicHttp"));
#endif
            channelFactory.Endpoint.EndpointBehaviors.Add(new AuditEndpointBehavior()
            {
                EventType = "Catalog:{action}",
                IncludeRequestHeaders = true,
                IncludeResponseHeaders = true
            });
            var x = channelFactory.CreateChannel();
            svc = x;
            return x as IContextChannel;
        }
    }
}
