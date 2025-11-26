using Audit.Core;
using Audit.Core.Providers;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Moq;

using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Audit.Grpc.Client.UnitTest
{
    [TestFixture]
    public class AuditClientInterceptorTests
    {
        [Test]
        public void AsyncUnaryCall_ResponseTaskThrows_SetsActionIsSuccessFalseAndException()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeRequestHeaders = _ => true
            };

            var ctx = CreateContext();
            
            var responseTask = Task.FromException<string>(new InvalidOperationException("boom-async"));

            var call = new AsyncUnaryCall<string>(responseTask, Task.FromResult(new Metadata()), () => new Status(StatusCode.Unknown, "err"), () => new Metadata(), () => { });

            Interceptor.AsyncUnaryCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncUnaryCall("req", ctx, cont);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await returned.ResponseAsync);
            Assert.That(ex!.Message, Is.EqualTo("boom-async"));

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.False);
                Assert.That(createdEvent.Action.Exception, Is.Not.Null);
                Assert.That(createdEvent.Action.Exception, Does.Contain("boom-async"));
            });
        }

        [Test]
        public void AsyncUnaryCall_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();
            
            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = ctx => ctx.Method.Type == MethodType.ServerStreaming
            };

            var ctx = CreateContext(null, null, MethodType.DuplexStreaming);
            var call = new AsyncUnaryCall<string>(Task.FromResult("test"), Task.FromResult(new Metadata()), () => new Status(StatusCode.Unknown, "err"), () => new Metadata(), () => { });
            Interceptor.AsyncUnaryCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncUnaryCall("req", ctx, cont);
            returned.Dispose();
            
            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();

            Assert.That(events, Is.Empty);
        }

        [Test]
        public void Constructor_WithConfigurator_SetsProperties()
        {
            var interceptor = new AuditClientInterceptor(cfg => cfg
                .CallFilter(_ => false)
                .IncludeRequestHeaders()
                .IncludeResponseHeaders()
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
                Assert.That(interceptor.IncludeResponseHeaders, Is.Not.Null);
                Assert.That(interceptor.IncludeTrailers, Is.Not.Null);
                Assert.That(interceptor.IncludeRequestPayload, Is.Not.Null);
                Assert.That(interceptor.IncludeResponsePayload, Is.Not.Null);
                Assert.That(interceptor.EventTypeName, Is.Not.Null);
                Assert.That(interceptor.EventCreationPolicy, Is.EqualTo(EventCreationPolicy.InsertOnStartReplaceOnEnd));
            });
        }

        [Test]
        public void IsAuditDisabled_Respects_GlobalFlag_And_Filter()
        {
            var interceptor = new AuditClientInterceptor();

            Configuration.AuditDisabled = true;

            var ctx = CreateContext();
            Assert.That(interceptor.IsAuditDisabled(ctx), Is.True);

            Configuration.AuditDisabled = false;

            Assert.That(interceptor.IsAuditDisabled(ctx), Is.False);

            interceptor.CallFilter = _ => false;
            Assert.That(interceptor.IsAuditDisabled(ctx), Is.True);

            interceptor.CallFilter = _ => true;
            Assert.That(interceptor.IsAuditDisabled(ctx), Is.False);

            Configuration.AuditDisabled = false;
        }

        [Test]
        public void BlockingUnaryCall_SuccessAndException_RecordsAction()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true
            };

            var ctx = CreateContext();

            string response = "ok";
            Interceptor.BlockingUnaryCallContinuation<string, string> cont = (_, _) => response;

            var result = interceptor.BlockingUnaryCall("req", ctx, cont);

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(response));
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.Response, Is.EqualTo(response));
            });

            Interceptor.BlockingUnaryCallContinuation<string, string> contThrow = (_, _) => throw new InvalidOperationException("boom");

            dp.ClearEvents();

            Assert.Throws<InvalidOperationException>(() => interceptor.BlockingUnaryCall("req", ctx, contThrow));

            var events2 = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent2 = events2.Count > 0 ? events2[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events2, Has.Count.EqualTo(1));
                Assert.That(createdEvent2, Is.Not.Null);
                Assert.That(createdEvent2.Action.IsSuccess, Is.False);
                Assert.That(createdEvent2.Action.Exception, Is.Not.Null);
            });
        }

        [Test]
        public void BlockingUnaryCall_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = ctx => ctx.Method.Type == MethodType.ServerStreaming
            };

            var ctx = CreateContext(null, null, MethodType.DuplexStreaming);

            Interceptor.BlockingUnaryCallContinuation<string, string> cont = (_, _) => "ok";

            var returned = interceptor.BlockingUnaryCall("req", ctx, cont);

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();

            Assert.That(events, Is.Empty);
            Assert.That(returned, Is.EqualTo("ok"));
        }

        [Test]
        public async Task AsyncUnaryCall_ResponseAndHeadersAndTrailers_AreCaptured()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeResponseHeaders = _ => true,
                IncludeRequestHeaders = _ => true,
                IncludeTrailers = _ => true
            };

            var headers = new Metadata { { "h1", "v1" }, { "bin-bin", new byte[] { 1, 2 } } };
            var trailers = new Metadata { { "t1", "tv1" } };

            var ctx = CreateContext(headers: new Metadata { { "rh1", "value" } });

            var responseTcs = new TaskCompletionSource<string>();
            responseTcs.SetResult("async-ok");

            var responseHeadersTcs = Task.FromResult(headers);
            Func<Status> getStatus = () => new Status(StatusCode.OK, "detail");
            Func<Metadata> getTrailers = () => trailers;

            var call = new AsyncUnaryCall<string>(responseTcs.Task, responseHeadersTcs, getStatus, getTrailers, () => { });

            Interceptor.AsyncUnaryCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncUnaryCall("r", ctx, cont);

            var resp = await returned.ResponseAsync; // triggers HandleAsyncUnaryCallResponse
            Assert.That(resp, Is.EqualTo("async-ok"));

            var respHeaders = await returned.ResponseHeadersAsync; // triggers HandleResponseHeaders
            Assert.That(respHeaders, Is.SameAs(headers));

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.Response, Is.EqualTo("async-ok"));
                Assert.That(createdEvent.Action.StatusCode, Is.EqualTo(StatusCode.OK.ToString()));
                Assert.That(createdEvent.Action.StatusDetail, Is.EqualTo("detail"));
                Assert.That(createdEvent.Action.Trailers, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestHeaders, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestHeaders.Find(h => h.Key == "rh1")?.Value, Is.EqualTo("value"));
                Assert.That(createdEvent.Action.ResponseHeaders, Is.Not.Null);
            });
        }

        [Test]
        public async Task AsyncClientStreamingCall_Captures_RequestStream_And_Handles_Response()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeResponseHeaders = _ => true,
                IncludeTrailers = _ => true
            };

            var ctx = CreateContext();

            // Inner request stream mock
            var innerWriterMock = CreateInnerWriterMock();

            var responseTask = Task.FromResult("stream-response");
            var responseHeadersTask = Task.FromResult(new Metadata { { "rh", "rv" } });
            Func<Status> getStatus = () => new Status(StatusCode.OK, "ok");
            Func<Metadata> getTrailers = () => new Metadata { { "tr", "tv" } };

            var call = new AsyncClientStreamingCall<string, string>(innerWriterMock.Object, responseTask, responseHeadersTask, getStatus, getTrailers, () => { });

            Interceptor.AsyncClientStreamingCallContinuation<string, string> cont = _ => call;

            var returned = interceptor.AsyncClientStreamingCall(ctx, cont);

            // RequestStream should be wrapper (or original) — writing should be captured
            await returned.RequestStream.WriteAsync("msg1");
            await returned.RequestStream.WriteAsync("msg2");
            await returned.RequestStream.CompleteAsync();

            var resp = await returned.ResponseAsync;
            Assert.That(resp, Is.EqualTo("stream-response"));

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream.Count, Is.EqualTo(2));
                Assert.That(createdEvent.Action.Response, Is.EqualTo("stream-response"));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.StatusCode, Is.EqualTo(StatusCode.OK.ToString()));
                Assert.That(createdEvent.Action.Trailers, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseHeaders, Is.Not.Null);
            });

            innerWriterMock.Verify(w => w.WriteAsync(It.IsAny<string>()), Times.Exactly(2));
            innerWriterMock.Verify(w => w.CompleteAsync(), Times.Once);
        }

        [Test]
        public async Task AsyncClientStreamingCall_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = ctx => ctx.Method.Type == MethodType.ServerStreaming
            };

            var ctx = CreateContext(null, null, MethodType.DuplexStreaming);

            var innerWriterMock = CreateInnerWriterMock();

            var call = new AsyncClientStreamingCall<string, string>(innerWriterMock.Object, Task.FromResult("test"), Task.FromResult(new Metadata()), () => new Status(), () => new Metadata(), () => { });

            Interceptor.AsyncClientStreamingCallContinuation<string, string> cont = _ => call;

            var returned = interceptor.AsyncClientStreamingCall(ctx, cont);
            var response = await returned.ResponseAsync;
            returned.Dispose();

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();

            Assert.That(events, Is.Empty);
            Assert.That(response, Is.EqualTo("test"));
        }

        [Test]
        public async Task AsyncServerStreamingCall_ReadsResponses_And_OnCompleteRuns_Hydration()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true,
                IncludeTrailers = _ => true,
                IncludeResponseHeaders = _ => true
            };

            var ctx = CreateContext();

            var responseReader = new SimpleAsyncStreamReader(new object[] { "r1", "r2" });
            var responseHeadersTask = Task.FromResult(new Metadata { { "hh", "hv" } });
            Func<Status> getStatus = () => new Status(StatusCode.OK, "done");
            Func<Metadata> getTrailers = () => new Metadata { { "tr", "tv" } };

            var call = new AsyncServerStreamingCall<string>(responseReader, responseHeadersTask, getStatus, getTrailers, () => { });

            Interceptor.AsyncServerStreamingCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncServerStreamingCall("req", ctx, cont);

            var reader = returned.ResponseStream;
            // iterate
            var list = new List<string>();
            while (await reader.MoveNext(CancellationToken.None))
            {
                list.Add(reader.Current);
            }

            returned.Dispose();

            await Task.Delay(800);

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;
            
            Assert.Multiple(() =>
            {
                Assert.That(list, Is.EqualTo(new[] { "r1", "r2" }));
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream.Count, Is.EqualTo(2));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.StatusCode, Is.EqualTo(StatusCode.OK.ToString()));
                Assert.That(createdEvent.Action.Trailers, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseHeaders, Is.Not.Null);
            });
        }

        [Test]
        public void AsyncServerStreamingCall_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = ctx => ctx.Method.Type == MethodType.ServerStreaming
            };

            var ctx = CreateContext(null, null, MethodType.DuplexStreaming);

            var responseReader = new SimpleAsyncStreamReader(new object[] { "ok", new InvalidOperationException("boom") });

            var call = new AsyncServerStreamingCall<string>(responseReader, Task.FromResult(new Metadata()), () => new Status(), () => new Metadata(), () => {});

            Interceptor.AsyncServerStreamingCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncServerStreamingCall("req", ctx, cont);

            returned.Dispose();

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();

            Assert.That(events, Is.Empty);
        }

        [Test]
        public async Task AsyncServerStreamingCall_MoveNext_Exception_Path_Sets_ExceptionAnd_DisposesScope()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeResponsePayload = _ => true
            };

            var ctx = CreateContext();

            var responseReader = new SimpleAsyncStreamReader(new object[] { "ok", new InvalidOperationException("boom") });
            var responseHeadersTask = Task.FromResult(new Metadata());
            Func<Status> getStatus = () => new Status(StatusCode.Unknown, "err");
            Func<Metadata> getTrailers = () => new Metadata();

            var call = new AsyncServerStreamingCall<string>(responseReader, responseHeadersTask, getStatus, getTrailers, () => {});

            Interceptor.AsyncServerStreamingCallContinuation<string, string> cont = (_, _) => call;

            var returned = interceptor.AsyncServerStreamingCall("req", ctx, cont);

            var reader = returned.ResponseStream;

            // first MoveNext -> ok
            Assert.That(await reader.MoveNext(CancellationToken.None), Is.True);
            Assert.That(reader.Current, Is.EqualTo("ok"));

            // next MoveNext should throw and cause DisposeAsync on scope
            Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.MoveNext(CancellationToken.None));

            returned.Dispose();

            await Task.Delay(800);

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.IsSuccess, Is.False);
                Assert.That(createdEvent.Action.Exception, Is.Not.Null);
            });
        }

        [Test]
        public async Task AsyncDuplexStreamingCall_Captures_Both_Sides()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                IncludeRequestPayload = _ => true,
                IncludeResponsePayload = _ => true,
                IncludeResponseHeaders = _ => true,
                IncludeTrailers = _ => true
            };

            var ctx = CreateContext();

            var innerWriterMock = new Mock<IClientStreamWriter<string>>();
            innerWriterMock.SetupProperty(w => w.WriteOptions);
            innerWriterMock.Setup(w => w.WriteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            innerWriterMock.Setup(w => w.CompleteAsync()).Returns(Task.CompletedTask);

            var responseReader = new SimpleAsyncStreamReader(new object[] { "d1" });
            var call = new AsyncDuplexStreamingCall<string, string>(
                innerWriterMock.Object,
                responseReader,
                Task.FromResult(new Metadata()),
                () => new Status(StatusCode.OK, "ok"),
                () => new Metadata(),
                () => {});

            Interceptor.AsyncDuplexStreamingCallContinuation<string, string> cont = _ => call;

            var returned = interceptor.AsyncDuplexStreamingCall(ctx, cont);

            await returned.RequestStream.WriteAsync("req1");
            await returned.RequestStream.CompleteAsync();

            Assert.That(await returned.ResponseStream.MoveNext(CancellationToken.None), Is.True);
            Assert.That(returned.ResponseStream.Current, Is.EqualTo("d1"));

            await returned.RequestStream.CompleteAsync();
            
            returned.Dispose();

            await Task.Delay(800);

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();
            var createdEvent = events.Count > 0 ? events[0] : null;

            Assert.Multiple(() =>
            {
                Assert.That(events, Has.Count.EqualTo(1));
                Assert.That(createdEvent, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream, Is.Not.Null);
                Assert.That(createdEvent.Action.RequestStream.Count, Is.EqualTo(1));
                Assert.That(createdEvent.Action.ResponseStream, Is.Not.Null);
                Assert.That(createdEvent.Action.ResponseStream.Count, Is.EqualTo(1));
                Assert.That(createdEvent.Action.IsSuccess, Is.True);
                Assert.That(createdEvent.Action.ResponseHeaders, Is.Not.Null);
                Assert.That(createdEvent.Action.Trailers, Is.Not.Null);
            });
        }

        [Test]
        public async Task AsyncDuplexStreamingCall_AuditDisabled_DoNotAudit()
        {
            var dp = new InMemoryDataProvider();

            var interceptor = new AuditClientInterceptor()
            {
                DataProvider = dp,
                EventCreationPolicy = EventCreationPolicy.InsertOnStartInsertOnEnd,
                CallFilter = ctx => ctx.Method.Type == MethodType.ServerStreaming
            };

            var ctx = CreateContext(null, null, MethodType.DuplexStreaming);

            var innerWriterMock = new Mock<IClientStreamWriter<string>>();
            innerWriterMock.SetupProperty(w => w.WriteOptions);
            innerWriterMock.Setup(w => w.WriteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            innerWriterMock.Setup(w => w.CompleteAsync()).Returns(Task.CompletedTask);

            var responseReader = new SimpleAsyncStreamReader(["d1"]);
            var call = new AsyncDuplexStreamingCall<string, string>(
                innerWriterMock.Object,
                responseReader,
                Task.FromResult(new Metadata()),
                () => new Status(StatusCode.OK, "ok"),
                () => new Metadata(),
                () => { });

            Interceptor.AsyncDuplexStreamingCallContinuation<string, string> cont = _ => call;

            var returned = interceptor.AsyncDuplexStreamingCall(ctx, cont);

            await returned.RequestStream.WriteAsync("req1");
            await returned.RequestStream.CompleteAsync();

            returned.Dispose();

            var events = dp.GetAllEventsOfType<AuditEventGrpcClient>();

            Assert.That(events, Is.Empty);
        }

        #region Helpers

        private class SimpleAsyncStreamReader : IAsyncStreamReader<string>
        {
            private readonly Queue<object> _items;

            public SimpleAsyncStreamReader(IEnumerable<object> items)
            {
                _items = new Queue<object>(items);
            }

            public string Current { get; private set; }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                await Task.Yield();
                if (_items.Count == 0) return false;
                var next = _items.Dequeue();
                if (next is Exception ex) throw ex;
                Current = (string)next;
                return true;
            }
        }

        private static Method<string, string> CreateDummyMethod(MethodType type)
        {
            var marshaller = Marshallers.Create(s => Encoding.UTF8.GetBytes(s ?? string.Empty),
                bytes => Encoding.UTF8.GetString(bytes ?? []));
            return new Method<string, string>(type, "TestService", "TestMethod", marshaller, marshaller);
        }

        private static ClientInterceptorContext<string, string> CreateContext(Metadata headers = null, DateTime? deadline = null, MethodType type = MethodType.Unary)
        {
            var method = CreateDummyMethod(type);
            var options = new CallOptions(headers, deadline);
            return new ClientInterceptorContext<string, string>(method, "localhost", options);
        }

        private static Mock<IClientStreamWriter<string>> CreateInnerWriterMock()
        {
            var innerWriterMock = new Mock<IClientStreamWriter<string>>();
            innerWriterMock.SetupProperty(w => w.WriteOptions);
            innerWriterMock.Setup(w => w.WriteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            innerWriterMock.Setup(w => w.CompleteAsync()).Returns(Task.CompletedTask).Verifiable();
            return innerWriterMock;
        }

        #endregion
    }
}