using Audit.Http;
using Audit.Http.ConfigurationApi;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Audit.UnitTest
{
    public class HttpClientTests
    {
        private HttpClient _httpClient = new HttpClient(new AuditHttpClientHandler()
        {
            IncludeRequestBody = true,
            IncludeContentHeaders = true,
            CreationPolicy = Core.EventCreationPolicy.InsertOnStartInsertOnEnd
        });

        [SetUp]
        public void Setup()
        {
            Audit.Core.Configuration.AuditDisabled = false;
            Audit.Core.Configuration.ResetCustomActions();
        }

        private Action<IAuditClientHandlerConfigurator> config = _ => _
            .IncludeRequestBody()
            .IncludeRequestHeaders()
            .IncludeResponseBody()
            .IncludeResponseHeaders()
            .IncludeContentHeaders()
            .EventType("test");

        [Test]
        public async Task Test_HttpAction_InsertOnStartInsertOnEnd()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual(2, evs.Count);

            Assert.AreEqual("test", evs[0].EventType);
            Assert.AreEqual("POST", actions[0].Method);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNotNull(actions[0].Request.Content);
            Assert.AreEqual("string content", actions[0].Request.Content.Body.ToString());
            Assert.AreEqual("text/plain; charset=utf-8", actions[0].Request.Content.Headers["Content-Type"]);
            Assert.AreEqual("http", actions[1].Request.Scheme);
            Assert.AreEqual("test1, test2", actions[0].Request.Headers["pepe"]);
            Assert.AreEqual("?some=querystring", actions[0].Request.QueryString);
            Assert.AreEqual("/some/path", actions[0].Request.Path);
            Assert.IsNull(actions[0].Response);
            Assert.AreEqual(url, actions[0].Url);

            Assert.AreEqual("POST", actions[1].Method);
            Assert.IsNotNull(actions[1].Request);
            Assert.IsNotNull(actions[1].Response);
            Assert.AreEqual(url, actions[1].Url);
            Assert.IsTrue(actions[1].Response.Headers.Count > 0);
            Assert.AreEqual(false, actions[1].Response.IsSuccess);
            Assert.AreEqual(404, actions[1].Response.StatusCode);
            Assert.AreEqual("Not Found", actions[1].Response.Reason);
            Assert.AreEqual("NotFound", actions[1].Response.Status);
            Assert.IsTrue(actions[1].Response.Content.Body.ToString().Length > 0);
            Assert.IsTrue(actions[1].Response.Content.Headers.Count > 0);
        }

        [Test]
        public async Task Test_HttpAction_InsertOnEnd()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(1, actions.Count);

            Assert.AreEqual("POST", actions[0].Method);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNotNull(actions[0].Request.Content);
            Assert.AreEqual("string content", actions[0].Request.Content.Body.ToString());
            Assert.AreEqual("text/plain; charset=utf-8", actions[0].Request.Content.Headers["Content-Type"]);
            Assert.AreEqual("http", actions[0].Request.Scheme);
            Assert.AreEqual("test1, test2", actions[0].Request.Headers["pepe"]);
            Assert.AreEqual("?some=querystring", actions[0].Request.QueryString);
            Assert.AreEqual("/some/path", actions[0].Request.Path);
            Assert.IsNotNull(actions[0].Response);
            Assert.AreEqual(url, actions[0].Url);
            Assert.IsTrue(actions[0].Response.Headers.Count > 0);
            Assert.AreEqual(false, actions[0].Response.IsSuccess);
            Assert.AreEqual(404, actions[0].Response.StatusCode);
            Assert.AreEqual("Not Found", actions[0].Response.Reason);
            Assert.AreEqual("NotFound", actions[0].Response.Status);
            Assert.IsTrue(actions[0].Response.Content.Body.ToString().Length > 0);
            Assert.IsTrue(actions[0].Response.Content.Headers.Count > 0);
        }

        [Test]
        public async Task Test_HttpAction_Manual()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(1, actions.Count);

            Assert.AreEqual("POST", actions[0].Method);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNotNull(actions[0].Request.Content);
            Assert.AreEqual("string content", actions[0].Request.Content.Body.ToString());
            Assert.AreEqual("text/plain; charset=utf-8", actions[0].Request.Content.Headers["Content-Type"]);
            Assert.AreEqual("http", actions[0].Request.Scheme);
            Assert.AreEqual("test1, test2", actions[0].Request.Headers["pepe"]);
            Assert.AreEqual("?some=querystring", actions[0].Request.QueryString);
            Assert.AreEqual("/some/path", actions[0].Request.Path);
            Assert.IsNotNull(actions[0].Response);
            Assert.AreEqual(url, actions[0].Url);
            Assert.IsTrue(actions[0].Response.Headers.Count > 0);
            Assert.AreEqual(false, actions[0].Response.IsSuccess);
            Assert.AreEqual(404, actions[0].Response.StatusCode);
            Assert.AreEqual("Not Found", actions[0].Response.Reason);
            Assert.AreEqual("NotFound", actions[0].Response.Status);
            Assert.IsTrue(actions[0].Response.Content.Body.ToString().Length > 0);
            Assert.IsTrue(actions[0].Response.Content.Headers.Count > 0);
        }


        [Test]
        public async Task Test_HttpAction_Put()
        {
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
                }))
                .WithCreationPolicy(Core.EventCreationPolicy.InsertOnEnd);

            var cli = ClientFactory.Create(config);
            var url = "http://google.com/some/path?some=querystring";
            await cli.PutAsync(url, new StringContent("string content", Encoding.UTF8));

            Assert.AreEqual(1, actions.Count);

            Assert.AreEqual("PUT", actions[0].Method);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNotNull(actions[0].Request.Content);
            Assert.AreEqual("string content", actions[0].Request.Content.Body.ToString());
            Assert.AreEqual("text/plain; charset=utf-8", actions[0].Request.Content.Headers["Content-Type"]);
            Assert.AreEqual("http", actions[0].Request.Scheme);
            Assert.AreEqual("?some=querystring", actions[0].Request.QueryString);
            Assert.AreEqual("/some/path", actions[0].Request.Path);
            Assert.IsNotNull(actions[0].Response);
            Assert.AreEqual(url, actions[0].Url);
            Assert.IsTrue(actions[0].Response.Headers.Count > 0);
            Assert.AreEqual(false, actions[0].Response.IsSuccess);
            Assert.AreEqual(404, actions[0].Response.StatusCode);
            Assert.AreEqual("Not Found", actions[0].Response.Reason);
            Assert.AreEqual("NotFound", actions[0].Response.Status);
            Assert.IsTrue(actions[0].Response.Content.Body.ToString().Length > 0);
            Assert.IsTrue(actions[0].Response.Content.Headers.Count > 0);
        }

        [Test]
        public async Task Test_HttpAction_Get()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(1, actions.Count, "a");
            Assert.AreEqual(1, evs.Count, "b");

            Assert.AreEqual("GET", actions[0].Method);
            Assert.AreEqual($"GET.{url}", evs[0].EventType);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNull(actions[0].Request.Content?.Body);
            Assert.IsNull(actions[0].Request.Content?.Headers);
            Assert.AreEqual("http", actions[0].Request.Scheme);
            Assert.AreEqual("?q=testGet", actions[0].Request.QueryString);
            Assert.AreEqual("/", actions[0].Request.Path);
            Assert.IsNotNull(actions[0].Response);
            Assert.AreEqual(url, actions[0].Url);
            Assert.IsTrue(actions[0].Response.Headers.Count > 0);
            Assert.AreEqual(true, actions[0].Response.IsSuccess);
            Assert.AreEqual(200, actions[0].Response.StatusCode);
            Assert.AreEqual("OK", actions[0].Response.Reason);
            Assert.AreEqual("OK", actions[0].Response.Status);
            Assert.IsTrue(actions[0].Response.Content.Body.ToString().Length > 10);
            Assert.IsTrue(actions[0].Response.Content.Headers.Count > 0);
        }

        [Test]
        public async Task Test_HttpAction_RequestException()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
                }));

            var cli = ClientFactory.Create(config);
            var url = "http://bad.0/some/path?some=querystring";
            try
            {
                await cli.PostAsync(url, new StringContent("string content", Encoding.UTF8));
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is HttpRequestException);
            }

            Assert.AreEqual(1, actions.Count);

            Assert.AreEqual("POST", actions[0].Method);
            Assert.IsTrue(actions[0].Exception.Contains("HttpRequestException"));
            Assert.IsTrue(evs[0].Environment.Exception.Length > 0);
            Assert.IsNotNull(actions[0].Request);
            Assert.IsNotNull(actions[0].Request.Content);
            Assert.AreEqual("string content", actions[0].Request.Content.Body.ToString());
            Assert.AreEqual("text/plain; charset=utf-8", actions[0].Request.Content.Headers["Content-Type"]);
            Assert.AreEqual("http", actions[0].Request.Scheme);
            Assert.AreEqual("?some=querystring", actions[0].Request.QueryString);
            Assert.AreEqual("/some/path", actions[0].Request.Path);
            Assert.AreEqual(url, actions[0].Url);
            Assert.IsNull(actions[0].Response);
        }

        [Test]
        public async Task Test_HttpAction_RequestFilterOut()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(1, evs.Count);

            Assert.AreEqual("GET", actions[0].Method);
            Assert.AreEqual(url, evs[0].EventType);
            Assert.IsNotNull(actions[0].Response);
        }

        [Test]
        public async Task Test_HttpAction_ResponseFilterOut()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
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

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(1, evs.Count);

            Assert.AreEqual("GET", actions[0].Method);
            Assert.AreEqual("OK", actions[0].Response.Status);
            Assert.IsNotNull(actions[0].Response);
        }

        [Test]
        public async Task Test_HttpAction_AuditDisabled()
        {
            var evs = new List<AuditEventHttpClient>();
            var actions = new List<HttpAction>();
            Audit.Core.Configuration.Setup()
                .Use(_ => _.OnInsertAndReplace(ev =>
                {
                    evs.Add(JsonConvert.DeserializeObject<AuditEventHttpClient>(JsonConvert.SerializeObject(ev)));
                    actions.Add(JsonConvert.DeserializeObject<HttpAction>(JsonConvert.SerializeObject(ev.GetHttpAction())));
                }));

            
            var url = "http://google.com/?q=test";
            using (var cli = new HttpClient(new AuditHttpClientHandler() { RequestFilter = _ => false }))
            {
                await cli.GetAsync(url);
            }

            Assert.AreEqual(0, actions.Count);
        }

    }

}
