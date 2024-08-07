﻿using Audit.Core;
using Audit.Core.Providers;
using Audit.Http;
using Audit.Http.ConfigurationApi;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Audit.HttpClientUnitTest
{
    public class HttpClientTests
    {
        private static JsonAdapter JsonAdapter = new JsonAdapter();

        private HttpClient _httpClient = new HttpClient(new AuditHttpClientHandler()
        {
            IncludeRequestBody = true,
            IncludeContentHeaders = true,
            CreationPolicy = Core.EventCreationPolicy.InsertOnStartInsertOnEnd
        });

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.Reset();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
        }

        private Action<IAuditClientHandlerConfigurator> config = _ => _
            .IncludeRequestBody()
            .IncludeRequestHeaders()
            .IncludeResponseBody()
            .IncludeResponseHeaders()
            .IncludeContentHeaders()
            .EventType("test");


        [Test]
        public void Test_Handler_Constructor_Parameterless()
        {
            var handler = new AuditHttpClientHandler();
            Assert.That(handler.InnerHandler, Is.Not.Null);
        }

        [Test]
        public void Test_Handler_Constructor_Config()
        {
            var handler = new AuditHttpClientHandler(_ => _.IncludeResponseBody());
            Assert.That(handler.InnerHandler, Is.Not.Null);
        }

        [Test]
        public void Test_Handler_Constructor_Config_Inner()
        {
            var inner = new HttpClientHandler();
            inner.Properties["x"] = 123;
            var handler = new AuditHttpClientHandler(_ => _.IncludeResponseBody(), inner);
            var handlerNullInner = new AuditHttpClientHandler(_ => _.IncludeResponseBody(), null);
            Assert.That(handler.InnerHandler, Is.Not.Null);
            Assert.That(handlerNullInner.InnerHandler, Is.Null);
            Assert.That((handler.InnerHandler as HttpClientHandler).Properties["x"], Is.EqualTo(123));
        }

        [Test]
        public async Task Test_HttpAction_InsertOnStartInsertOnEnd()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnStartInsertOnEnd);

            var cli = ClientFactory.Create(config);
            var url = "http://google.com/some/path?some=querystring";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "123456");
                requestMessage.Headers.Add("pepe", new[] { "test1", "test2" });
                requestMessage.Content = new StringContent("string content", Encoding.UTF8);
                await cli.SendAsync(requestMessage);
            }

            Assert.That(actions.Count, Is.EqualTo(2));
            Assert.That(evs.Count, Is.EqualTo(2));

            Assert.That(evs[0].EventType, Is.EqualTo("test"));
            Assert.That(actions[0].Method, Is.EqualTo("POST"));
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content, Is.Not.Null);
            Assert.That(actions[0].Request.Content.Body.ToString(), Is.EqualTo("string content"));
            Assert.That(actions[0].Request.Content.Headers["Content-Type"], Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(actions[1].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.Headers["pepe"], Is.EqualTo("test1, test2"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?some=querystring"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/some/path"));
            Assert.That(actions[0].Response, Is.Null);
            Assert.That(actions[0].Url, Is.EqualTo(url));

            Assert.That(actions[1].Method, Is.EqualTo("POST"));
            Assert.That(actions[1].Request, Is.Not.Null);
            Assert.That(actions[1].Response, Is.Not.Null);
            Assert.That(actions[1].Url, Is.EqualTo(url));
            Assert.That(actions[1].Response.Headers.Count > 0, Is.True);
            Assert.That(actions[1].Response.IsSuccess, Is.EqualTo(false));
            Assert.That(actions[1].Response.StatusCode, Is.EqualTo(404));
            Assert.That(actions[1].Response.Reason, Is.EqualTo("Not Found"));
            Assert.That(actions[1].Response.Status, Is.EqualTo("NotFound"));
            Assert.That(actions[1].Response.Content.Body.ToString().Length > 0, Is.True);
            Assert.That(actions[1].Response.Content.Headers.Count > 0, Is.True);
        }

        [Test]
        public async Task Test_HttpAction_InsertOnEnd()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnEnd);

            var cli = ClientFactory.Create(config);
            var url = "http://google.com/some/path?some=querystring";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "123456");
                requestMessage.Headers.Add("pepe", new[] { "test1", "test2" });
                requestMessage.Content = new StringContent("string content", Encoding.UTF8);
                await cli.SendAsync(requestMessage);
            }

            Assert.That(actions.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("POST"));
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content, Is.Not.Null);
            Assert.That(actions[0].Request.Content.Body.ToString(), Is.EqualTo("string content"));
            Assert.That(actions[0].Request.Content.Headers["Content-Type"], Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(actions[0].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.Headers["pepe"], Is.EqualTo("test1, test2"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?some=querystring"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/some/path"));
            Assert.That(actions[0].Response, Is.Not.Null);
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Response.Headers.Count > 0, Is.True);
            Assert.That(actions[0].Response.IsSuccess, Is.EqualTo(false));
            Assert.That(actions[0].Response.StatusCode, Is.EqualTo(404));
            Assert.That(actions[0].Response.Reason, Is.EqualTo("Not Found"));
            Assert.That(actions[0].Response.Status, Is.EqualTo("NotFound"));
            Assert.That(actions[0].Response.Content.Body.ToString().Length > 0, Is.True);
            Assert.That(actions[0].Response.Content.Headers.Count > 0, Is.True);
        }

        [Test]
        public async Task Test_HttpAction_Manual()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.Manual);

            var cli = ClientFactory.Create(config);
            var url = "http://google.com/some/path?some=querystring";
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "123456");
                requestMessage.Headers.Add("pepe", new[] { "test1", "test2" });
                requestMessage.Content = new StringContent("string content", Encoding.UTF8);
                await cli.SendAsync(requestMessage);
            }

            Assert.That(actions.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("POST"));
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content, Is.Not.Null);
            Assert.That(actions[0].Request.Content.Body.ToString(), Is.EqualTo("string content"));
            Assert.That(actions[0].Request.Content.Headers["Content-Type"], Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(actions[0].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.Headers["pepe"], Is.EqualTo("test1, test2"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?some=querystring"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/some/path"));
            Assert.That(actions[0].Response, Is.Not.Null);
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Response.Headers.Count > 0, Is.True);
            Assert.That(actions[0].Response.IsSuccess, Is.EqualTo(false));
            Assert.That(actions[0].Response.StatusCode, Is.EqualTo(404));
            Assert.That(actions[0].Response.Reason, Is.EqualTo("Not Found"));
            Assert.That(actions[0].Response.Status, Is.EqualTo("NotFound"));
            Assert.That(actions[0].Response.Content.Body.ToString().Length > 0, Is.True);
            Assert.That(actions[0].Response.Content.Headers.Count > 0, Is.True);
        }


        [Test]
        public async Task Test_HttpAction_Put()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnEnd);

            var cli = ClientFactory.Create(config);
            var url = "http://google.com/some/path?some=querystring";
            await cli.PutAsync(url, new StringContent("string content", Encoding.UTF8));

            Assert.That(actions.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("PUT"));
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content, Is.Not.Null);
            Assert.That(actions[0].Request.Content.Body.ToString(), Is.EqualTo("string content"));
            Assert.That(actions[0].Request.Content.Headers["Content-Type"], Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(actions[0].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?some=querystring"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/some/path"));
            Assert.That(actions[0].Response, Is.Not.Null);
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Response.Headers.Count > 0, Is.True);
            Assert.That(actions[0].Response.IsSuccess, Is.EqualTo(false));
            Assert.That(actions[0].Response.StatusCode, Is.EqualTo(404));
            Assert.That(actions[0].Response.Reason, Is.EqualTo("Not Found"));
            Assert.That(actions[0].Response.Status, Is.EqualTo("NotFound"));
            Assert.That(actions[0].Response.Content.Body.ToString().Length > 0, Is.True);
            Assert.That(actions[0].Response.Content.Headers.Count > 0, Is.True);
        }

        [Test]
        public async Task Test_HttpAction_Get()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnEnd);

            var url = "http://google.com/?q=testGet";
            using (var cli = new HttpClient(new AuditHttpClientHandler()
            {
                EventTypeName = "{verb}.{url}",
                IncludeRequestBody = true,
                IncludeRequestHeaders = true,
                IncludeResponseBody = true,
                IncludeResponseHeaders = true,
                IncludeContentHeaders = true
            }))
            {
                await cli.GetAsync(url);
            }

            Assert.That(actions.Count, Is.EqualTo(1), "a");
            Assert.That(evs.Count, Is.EqualTo(1), "b");

            Assert.That(actions[0].Method, Is.EqualTo("GET"));
            Assert.That(evs[0].EventType, Is.EqualTo($"GET.{url}"));
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content?.Body, Is.Null);
            Assert.That(actions[0].Request.Content?.Headers, Is.Null);
            Assert.That(actions[0].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?q=testGet"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/"));
            Assert.That(actions[0].Response, Is.Not.Null);
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Response.Headers.Count > 0, Is.True);
            Assert.That(actions[0].Response.IsSuccess, Is.EqualTo(true));
            Assert.That(actions[0].Response.StatusCode, Is.EqualTo(200));
            Assert.That(actions[0].Response.Reason, Is.EqualTo("OK"));
            Assert.That(actions[0].Response.Status, Is.EqualTo("OK"));
            Assert.That(actions[0].Response.Content.Body.ToString().Length > 10, Is.True);
            Assert.That(actions[0].Response.Content.Headers.Count > 0, Is.True);
        }

        [Test]
        public async Task Test_HttpAction_RequestException()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }));

            var cli = ClientFactory.Create(config);
            var url = "http://bad.0/some/path?some=querystring";
            try
            {
                await cli.PostAsync(url, new StringContent("string content", Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Assert.That(ex is HttpRequestException, Is.True);
            }

            Assert.That(actions.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("POST"));
            Assert.That(actions[0].Exception.Contains("HttpRequestException"), Is.True);
            Assert.That(evs[0].Environment.Exception.Length > 0, Is.True);
            Assert.That(actions[0].Request, Is.Not.Null);
            Assert.That(actions[0].Request.Content, Is.Not.Null);
            Assert.That(actions[0].Request.Content.Body.ToString(), Is.EqualTo("string content"));
            Assert.That(actions[0].Request.Content.Headers["Content-Type"], Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(actions[0].Request.Scheme, Is.EqualTo("http"));
            Assert.That(actions[0].Request.QueryString, Is.EqualTo("?some=querystring"));
            Assert.That(actions[0].Request.Path, Is.EqualTo("/some/path"));
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Response, Is.Null);
        }

        [Test]
        public async Task Test_HttpAction_RequestFilterOut()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }));

            var cli = ClientFactory.Create(_ => _
                .IncludeRequestBody()
                .IncludeRequestHeaders()
                .IncludeResponseBody()
                .IncludeResponseHeaders()
                .EventType(msg => msg.RequestUri.ToString())
                .FilterByRequest(req => req.Method.Method == "GET"));
            var url = "http://google.com/";

            await cli.PostAsync(url, new StringContent("string content", Encoding.UTF8));
            await cli.GetAsync(url);

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("GET"));
            Assert.That(evs[0].EventType, Is.EqualTo(url));
            Assert.That(actions[0].Response, Is.Not.Null);
        }

        [Test]
        public async Task Test_HttpAction_ResponseFilterOut()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }));

            var cli = ClientFactory.Create(_ => _
                .IncludeRequestBody()
                .IncludeRequestHeaders()
                .IncludeResponseBody()
                .IncludeResponseHeaders()
                .EventType(msg => msg.RequestUri.ToString())
                .FilterByResponse(res => res.StatusCode == HttpStatusCode.OK));
            var url = "http://google.com/";

            await cli.PostAsync(url, new StringContent("string content", Encoding.UTF8));
            await cli.GetAsync(url);

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(evs.Count, Is.EqualTo(1));

            Assert.That(actions[0].Method, Is.EqualTo("GET"));
            Assert.That(actions[0].Response.Status, Is.EqualTo("OK"));
            Assert.That(actions[0].Response, Is.Not.Null);
        }

        [Test]
        public async Task Test_HttpAction_AuditDisabled()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonAdapter.Deserialize<AuditEventHttpClient>(JsonAdapter.Serialize(ev)));
                    actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
                }));


            var url = "http://google.com/?q=test";
            using (var cli = new HttpClient(new AuditHttpClientHandler() { RequestFilter = _ => false }))
            {
                await cli.GetAsync(url);
            }

            Assert.That(actions.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Test_HttpAction_CustomAuditScopeFactory()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.DataProvider = null;

            var dp = new DynamicDataProvider();
            dp.AttachOnInsertAndReplace(ev =>
            {
                actions.Add(JsonAdapter.Deserialize<HttpAction>(JsonAdapter.Serialize(ev.GetHttpAction())));
            });

            var factory = new Mock<IAuditScopeFactory>();
            factory.Setup(_ => _.CreateAsync(It.IsAny<AuditScopeOptions>(), It.IsAny<CancellationToken>()))
                .Returns(async () => await AuditScope.CreateAsync(new AuditScopeOptions() { DataProvider = dp, AuditEvent = new AuditEventHttpClient() }));

            var url = "http://google.com/?q=test";
            using (var cli = new HttpClient(new AuditHttpClientHandler() { RequestFilter = _ => true, AuditScopeFactory = factory.Object }))
            {
                await cli.GetAsync(url);
            }

            Assert.That(actions.Count, Is.EqualTo(1));
            Assert.That(actions[0].Url, Is.EqualTo(url));
            Assert.That(actions[0].Method, Is.EqualTo("GET"));
        }

        [Test]
        public async Task Test_IncludeOptions()
        {
            // Arrange
            var dataProvider = new InMemoryDataProvider();
            using var client = new HttpClient(new AuditHttpClientHandler()
            {
                IncludeRequestBody = true,
                IncludeContentHeaders = true,
                IncludeOptions = key => !key.StartsWith("_"),
                AuditDataProvider = dataProvider
            });

            var request = new HttpRequestMessage(HttpMethod.Get, "http://google.com/?q=test");
            
#if NET6_0_OR_GREATER
            request.Options.Set(new HttpRequestOptionsKey<int>("test"), 12345);
            request.Options.Set(new HttpRequestOptionsKey<int>("_untracked"), 1);
#else
            request.Properties["test"] = 12345;
            request.Properties["_untracked"] = 1;
#endif

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var events = dataProvider.GetAllEvents();

            Assert.That(response, Is.Not.Null);
            Assert.That(events, Is.Not.Null);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<AuditEventHttpClient>());
            var ev = events[0] as AuditEventHttpClient;
            Assert.That(ev, Is.Not.Null);
            Assert.That(ev.Action.Method, Is.EqualTo("GET"));
            Assert.That(ev.Action.Request.Options, Has.Count.EqualTo(1));
            Assert.That(ev.Action.Request.Options, Contains.Key("test"));
            Assert.That(ev.Action.Request.Options["test"], Is.EqualTo(12345));
        }

        [Test]
        public async Task Test_GetRequestMessage_GetResponseMessage()
        {
            // Arrange
            var dataProvider = new InMemoryDataProvider();
            var response = default(HttpResponseMessage);
            using (var client = new HttpClient(new AuditHttpClientHandler() { AuditDataProvider = dataProvider, CreationPolicy = EventCreationPolicy.InsertOnEnd }))
            {
                var requestMessage = default(HttpRequestMessage);
                var responseMessage = default(HttpResponseMessage);

                Audit.Core.Configuration.AddCustomAction(ActionType.OnScopeCreated, scope =>
                {
                    requestMessage ??= scope.GetHttpAction().GetRequestMessage();
                    responseMessage ??= scope.GetHttpAction().GetResponseMessage();

                    Assert.That(requestMessage, Is.Not.Null);
                    Assert.That(responseMessage, Is.Null);
                });

                Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
                {
                    requestMessage ??= scope.GetHttpAction().GetRequestMessage();
                    responseMessage ??= scope.GetHttpAction().GetResponseMessage();

                    Assert.That(requestMessage, Is.Not.Null);
                    Assert.That(responseMessage, Is.Not.Null);
                });

                var request = new HttpRequestMessage(HttpMethod.Get, "http://google.com/?q=test");

                // Act
                response = await client.SendAsync(request);
            }
            
            // Assert
            var events = dataProvider.GetAllEvents();

            Assert.That(response, Is.Not.Null);
            Assert.That(events, Is.Not.Null);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<AuditEventHttpClient>());
        }
    }
}
