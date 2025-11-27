using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Grpc.Core;
using NUnit.Framework;
using Grpc.Core.Testing;

namespace Audit.Grpc.Server.UnitTest
{
    [TestFixture]
    public class AuditServerInterceptorTests
    {
        [SetUp]
        public void SetUp()
        {
            Configuration.Reset();
            Configuration.DataProvider = new NullDataProvider();

            Configuration.AddOnSavingAction(scope =>
            {
                var action = scope.Event.GetServerCallAction();

                action.CustomFields["TraceId"] = "FakeTraceId";
            });
        }

        [Test]
        public void Constructor_WithConfigurator_SetsProperties()
        {
            var interceptor = new AuditServerInterceptor(cfg => cfg
                .CallFilter(_ => false)
                .IncludeRequestHeaders()
                .IncludeTrailers()
                .IncludeRequestPayload()
                .IncludeResponsePayload()
                .EventType("/svc/{service}/{method}")
                .CreationPolicy(EventCreationPolicy.InsertOnStartReplaceOnEnd)
            );

            Assert.Multiple(() =>
            {
                Assert.That(interceptor.CallFilter, Is.Not.Null);
                Assert.That(interceptor.IncludeRequestHeaders, Is.Not.Null);
                Assert.That(interceptor.IncludeTrailers, Is.Not.Null);
                Assert.That(interceptor.IncludeRequestPayload, Is.Not.Null);
                Assert.That(interceptor.IncludeResponsePayload, Is.Not.Null);
                Assert.That(interceptor.EventTypeName, Is.Not.Null);
                Assert.That(interceptor.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnStartReplaceOnEnd));
            });
        }

        [Test]
        public async Task UnaryServerHandler_Success_RecordsAction()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeRequestHeaders = _ => true,
                IncludeTrailers = _ => true
            };

            var ctx = CreateContext(requestHeaders: new Metadata { { "rh1", "v1" } });

            string response = "ok";
            UnaryServerMethod<string, string> cont = (_, _) => Task.FromResult(response);

            var result = await interceptor.UnaryServerHandler("req", ctx, cont);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(response));
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.Response, Is.EqualTo(response));
                Assert.That(createdEvent.Action.RequestHeaders, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestHeaders.Find(h => h.Key == "rh1")?.Value, Is.EqualTo("v1"));
                Assert.That(createdEvent.Action.CustomFields["TraceId"], Is.EqualTo("FakeTraceId"));
            });
        }

        [Test]
        public void UnaryServerHandler_Exception_SetsActionIsSuccessFalseAndException()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeRequestHeaders = _ => true,
                IncludeTrailers = _ => true
            };

            var ctx = CreateContext();

            UnaryServerMethod<string, string> contThrow = (_, _) => throw new InvalidOperationException("boom-server");

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await interceptor.UnaryServerHandler("req", ctx, contThrow));
            Assert.That(ex?.Message, Is.EqualTo("boom-server"));

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.False);
                Assert.That(createdEvent.Action.Exception, Is.Not.Null);
                Assert.That(createdEvent.Action.Exception, Does.Contain("boom-server"));
            });
        }

        [Test]
        public async Task UnaryServerHandler_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = _ => false
            };

            // disable globally
            Configuration.AuditDisabled = true;

            var ctx = CreateContext();
            UnaryServerMethod<string, string> cont = (_, _) => Task.FromResult("ok");

            var result = await interceptor.UnaryServerHandler("req", ctx, cont);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo("ok"));
                Assert.That(events, Is.Empty);
            });

            Configuration.AuditDisabled = false;
        }

        [Test]
        public async Task ClientStreamingServerHandler_Captures_RequestStream_And_Handles_Response()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                IncludeRequestHeaders = _ => true
            };

            var ctx = CreateContext();

            var innerReader = new SimpleAsyncStreamReader<string>(new object[] { "m1", "m2" });

            ClientStreamingServerMethod<string, string> cont = async (reader, _) =>
            {
                var list = new List<string>();
                while (await reader.MoveNext(CancellationToken.None))
                {
                    list.Add(reader.Current);
                }
                return "count:" + list.Count;
            };

            var resp = await interceptor.ClientStreamingServerHandler(innerReader, ctx, cont);

            Assert.That(resp, Is.EqualTo("count:2"));

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream.Count, Is.EqualTo(2));
                Assert.That(createdEvent.Action.Response, Is.EqualTo("count:2"));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
            });
        }

        [Test]
        public async Task ClientStreamingServerHandler_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                IncludeRequestHeaders = _ => true,
                CallFilter = ctx => ctx.Host.StartsWith("not_found")
            };
            
            var ctx = CreateContext();

            var innerReader = new SimpleAsyncStreamReader<string>(new object[] { "m1", "m2" });

            ClientStreamingServerMethod<string, string> cont = async (reader, _) =>
            {
                var list = new List<string>();
                while (await reader.MoveNext(CancellationToken.None))
                {
                    list.Add(reader.Current);
                }
                return "count:" + list.Count;
            };

            var resp = await interceptor.ClientStreamingServerHandler(innerReader, ctx, cont);
            
            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();

            Assert.Multiple(() =>
            {
                Assert.That(resp, Is.EqualTo("count:2"));
                Assert.That(events, Is.Empty);
            });
        }

        [Test]
        public async Task ServerStreamingServerHandler_Captures_ResponseStream_And_Hydrates_Trailers()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                IncludeRequestHeaders = _ => true
            };

            var ctx = CreateContext(requestHeaders: new Metadata { { "rh", "rv" } });

            var written = new List<string>();
            var innerWriter = new FakeServerStreamWriter<string>(written);

            ServerStreamingServerMethod<string, string> cont = async (_, writer, _) =>
            {
                await writer.WriteAsync("r1");
                await writer.WriteAsync("r2");
            };

            await interceptor.ServerStreamingServerHandler("req", innerWriter, ctx, cont);

            // wait small time for scope disposal (scope might be async)
            await Task.Delay(100);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(new[] { "r1", "r2" }));
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream.Count, Is.EqualTo(2));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
            });
        }

        [Test]
        public async Task ServerStreamingServerHandler_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                IncludeRequestHeaders = _ => true,
                CallFilter = ctx => !ctx.RequestHeaders.Any(h => h is { Key: "skip", Value: "true" })
            };

            var ctx = CreateContext(requestHeaders: new Metadata { { "skip", "true" } });

            var written = new List<string>();
            var innerWriter = new FakeServerStreamWriter<string>(written);

            ServerStreamingServerMethod<string, string> cont = async (_, writer, _) =>
            {
                await writer.WriteAsync("r1");
                await writer.WriteAsync("r2");
            };

            await interceptor.ServerStreamingServerHandler("req", innerWriter, ctx, cont);

            // wait small time for scope disposal (scope might be async)
            await Task.Delay(100);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();

            Assert.Multiple(() =>
            {
                Assert.That(events, Is.Empty);
            });
        }

        [Test]
        public async Task DuplexStreamingServerHandler_Captures_Both_Sides()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true
            };

            var ctx = CreateContext();

            var requestReader = new SimpleAsyncStreamReader<string>(new object[] { "rq1" });

            var written = new List<string>();
            var responseWriter = new FakeServerStreamWriter<string>(written);

            DuplexStreamingServerMethod<string, string> cont = async (reqReader, respWriter, _) =>
            {
                while (await reqReader.MoveNext(CancellationToken.None))
                {
                    var v = reqReader.Current;
                    await respWriter.WriteAsync("echo:" + v);
                }
            };

            await interceptor.DuplexStreamingServerHandler(requestReader, responseWriter, ctx, cont);

            // allow finalization
            await Task.Delay(100);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(new[] { "echo:rq1" }));
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream.Count, Is.EqualTo(1));
                Assert.That(createdEvent.Action.ResponseStream, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream.Count, Is.EqualTo(1));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
            });
        }

        [Test]
        public async Task DuplexStreamingServerHandler_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditServerInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                CallFilter = ctx => ctx.Method == "/no_match"
            };

            var ctx = CreateContext();

            var requestReader = new SimpleAsyncStreamReader<string>(new object[] { "rq1" });

            var written = new List<string>();
            var responseWriter = new FakeServerStreamWriter<string>(written);

            DuplexStreamingServerMethod<string, string> cont = async (reqReader, respWriter, _) =>
            {
                while (await reqReader.MoveNext(CancellationToken.None))
                {
                    var v = reqReader.Current;
                    await respWriter.WriteAsync("echo:" + v);
                }
            };

            await interceptor.DuplexStreamingServerHandler(requestReader, responseWriter, ctx, cont);

            // allow finalization
            await Task.Delay(100);

            var events = dp.GetAllEventsOfType<AuditEventGrpcServer>();

            Assert.Multiple(() =>
            {
                Assert.That(events, Is.Empty);
            });
        }

        #region Helpers

        private static ServerCallContext CreateContext(Metadata requestHeaders = null)
        {
            var context = TestServerCallContext.Create(method: "/TestService/TestMethod",
                host: "host", deadline: DateTime.UtcNow.AddDays(5), requestHeaders: requestHeaders ?? new Metadata(),
                cancellationToken: CancellationToken.None,
                peer: "peer", authContext: null, null, null, null, null);

            return context;
        }

        private class SimpleAsyncStreamReader<T> : IAsyncStreamReader<T> where T : class
        {
            private readonly Queue<object> _items;
            public SimpleAsyncStreamReader(IEnumerable<object> items)
            {
                _items = new Queue<object>(items);
            }

            public T Current { get; private set; }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                await Task.Yield();
                if (_items.Count == 0) return false;
                var next = _items.Dequeue();
                if (next is Exception ex) throw ex;
                Current = (T)next;
                return true;
            }
        }

        private class FakeServerStreamWriter<T> : IServerStreamWriter<T> where T : class
        {
            private readonly List<T> _written;
            public FakeServerStreamWriter(List<T> written)
            {
                _written = written;
            }

            public WriteOptions WriteOptions { get; set; }

            public Task WriteAsync(T message)
            {
                _written.Add(message);
                return Task.CompletedTask;
            }
        }

        #endregion
    }
}