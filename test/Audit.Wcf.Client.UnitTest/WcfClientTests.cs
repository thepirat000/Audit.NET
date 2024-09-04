using Audit.Core;
using Audit.Wcf.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Audit.Core.Providers;
#if NETCOREAPP3_1
using Microsoft.Extensions.Logging;
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
            _cancellationToken.Dispose();
        }

        [SetUp]
        public void SetupTest()
        {
            Audit.Core.Configuration.Reset();
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

            Audit.Core.Configuration.Setup().UseNullProvider().WithCreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd);

            var dynamicProvider = new DynamicDataProvider(_ => _
                    .OnInsert(ev => inserted.Add(AuditEvent.FromJson(ev.ToJson())))
                    .OnReplace((id, ev) => { replaced.Add(AuditEvent.FromJson(ev.ToJson())); idsReplaced.Add(id); }));

            var auditScopeFactory = new TestAuditScopeFactory();

            var channel = GetServiceProxy(out ICatalogService svc, auditScopeFactory, dynamicProvider);

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
                    Assert.That(ex.Message.Contains("internal error"), Is.True);
                }
            }

            Assert.That(auditScopeFactory.OnScopeCreatedCount, Is.EqualTo(1));
            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(1));
            Assert.That(idsReplaced.Count, Is.EqualTo(1));
            var actionInserted = inserted[0].GetWcfClientAction();
            var actionReplaced = replaced[0].GetWcfClientAction();
            Assert.That(actionInserted, Is.Not.Null);
            Assert.That(actionReplaced, Is.Not.Null);
            Assert.That(actionInserted.Action.Contains("InsertProduct"), Is.True);
            Assert.That(actionReplaced.Action.Contains("InsertProduct"), Is.True);

            Assert.That(inserted[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"), Is.True);
            Assert.That(replaced[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"), Is.True);

            Assert.That(inserted[0].EndDate, Is.Null);
            Assert.That(replaced[0].EndDate, Is.Not.Null);

            Assert.That(actionInserted.RequestBody.Contains("test name") && actionInserted.RequestBody.Contains("001"), Is.True);
            Assert.That(actionReplaced.RequestBody.Contains("test name") && actionReplaced.RequestBody.Contains("001"), Is.True);

            Assert.That(actionInserted.ResponseBody, Is.Null);
            Assert.That(actionReplaced.ResponseBody.Contains("InsertProductResponse"), Is.True);

            Assert.That(actionInserted.ResponseHeaders, Is.Null);
            Assert.That(actionReplaced.ResponseHeaders.Count > 0, Is.True);

            Assert.That(actionInserted.ResponseStatuscode, Is.Null);
            Assert.That((int)actionReplaced.ResponseStatuscode, Is.EqualTo(200));

            Assert.That(actionInserted.RequestHeaders.ContainsKey("CustomHeader"), Is.True);
            Assert.That(actionReplaced.RequestHeaders.ContainsKey("CustomHeader"), Is.True);

            Assert.That(actionInserted.RequestHeaders["CustomHeader"], Is.EqualTo(Environment.UserName));
            Assert.That(actionReplaced.RequestHeaders["CustomHeader"], Is.EqualTo(Environment.UserName));

            Assert.That(actionInserted.HttpMethod, Is.EqualTo("POST"));
            Assert.That(actionReplaced.HttpMethod, Is.EqualTo("POST"));

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

            var channel = GetServiceProxy(out ICatalogService svc, null, null);

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
                    Assert.That(ex.Message.Contains("internal error"), Is.True);
                }
            }

            Assert.That(inserted.Count, Is.EqualTo(1));
            Assert.That(replaced.Count, Is.EqualTo(1));
            Assert.That(idsReplaced.Count, Is.EqualTo(1));
            var actionInserted = inserted[0].GetWcfClientAction();
            var actionReplaced = replaced[0].GetWcfClientAction();
            Assert.That(actionInserted, Is.Not.Null);
            Assert.That(actionReplaced, Is.Not.Null);
            Assert.That(actionInserted.Action.Contains("InsertProduct"), Is.True);
            Assert.That(actionReplaced.Action.Contains("InsertProduct"), Is.True);

            Assert.That(inserted[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"), Is.True);
            Assert.That(replaced[0].EventType.Contains("Catalog:") && inserted[0].EventType.Contains("InsertProduct"), Is.True);

            Assert.That(inserted[0].EndDate, Is.Null);
            Assert.That(replaced[0].EndDate, Is.Not.Null);

            Assert.That(actionInserted.RequestBody.Contains("should fail") && actionInserted.RequestBody.Contains("002"), Is.True);
            Assert.That(actionReplaced.RequestBody.Contains("should fail") && actionReplaced.RequestBody.Contains("002"), Is.True);

            Assert.That(actionInserted.ResponseBody, Is.Null);
            Assert.That(actionReplaced.ResponseBody.Contains("faultcode"), Is.True);

            Assert.That(actionInserted.ResponseHeaders, Is.Null);
            Assert.That(actionReplaced.ResponseHeaders.Count > 0, Is.True);

            Assert.That(actionInserted.ResponseStatuscode, Is.Null);
            Assert.That((int)actionReplaced.ResponseStatuscode, Is.EqualTo(500));

            Assert.That(actionInserted.RequestHeaders.ContainsKey("CustomHeader"), Is.True);
            Assert.That(actionReplaced.RequestHeaders.ContainsKey("CustomHeader"), Is.True);

            Assert.That(actionInserted.RequestHeaders["CustomHeader"], Is.EqualTo(Environment.UserName));
            Assert.That(actionReplaced.RequestHeaders["CustomHeader"], Is.EqualTo(Environment.UserName));

            Assert.That(actionInserted.HttpMethod, Is.EqualTo("POST"));
            Assert.That(actionReplaced.HttpMethod, Is.EqualTo("POST"));

            Assert.IsFalse(actionInserted.IsFault);
            Assert.That(actionReplaced.IsFault, Is.True);
        }

        public static IContextChannel GetServiceProxy(out ICatalogService svc, IAuditScopeFactory scopeFactory, AuditDataProvider dataProvider)
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
                IncludeResponseHeaders = true,
                AuditScopeFactory = scopeFactory,
                AuditDataProvider = dataProvider

            });
            var x = channelFactory.CreateChannel();
            svc = x;
            return x as IContextChannel;
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
