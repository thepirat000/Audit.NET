using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using Audit.Core.Providers;
using Audit.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Audit.Integration.AspNetCore;

[TestFixture]
public class SignalRTests
{
    [Test]
    public async Task Test_SignalR_HappyPath()
    {
        var dp = new InMemoryDataProvider();

        var webHostBuilder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSignalR(c =>
                {
                    c.AddAuditFilter(config => config
                        .IncludeHeaders()
                        .IncludeQueryString()
                        .WithDataProvider(dp)
                        .WithCreationPolicy(EventCreationPolicy.InsertOnEnd)
                        .Filters(f => f
                            .IncludeIncomingEvent(true)
                            .IncludeConnectEvent(true)
                            .IncludeDisconnectEvent(true))
                    );
                });
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ChatHub>("/chatHub");
                });
            });

        var server = new TestServer(webHostBuilder);
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/chatHub", o => o.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        var messages = new List<string>();

        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            var newMessage = $"{user}: {message}";
            messages.Add(newMessage);
        });

        await connection.StartAsync();
        await connection.InvokeAsync("SendMessage", "user test", "message test");
        await connection.StopAsync();
        await connection.DisposeAsync();

        await Task.Delay(500);

        var evs = dp.GetAllEventsOfType<AuditEventSignalr>();
        var incomingEvent = evs.FirstOrDefault(e => e.Event.EventType == SignalrEventType.Incoming)?.Event as SignalrEventIncoming;

        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(3, evs.Count);
        Assert.IsNotNull(incomingEvent);

        Assert.AreEqual("user test: message test", messages[0]);

        Assert.IsTrue(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Connect));
        Assert.IsTrue(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Incoming));
        Assert.IsTrue(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Disconnect));

        Assert.AreEqual("return test", incomingEvent.Result);
        Assert.AreEqual(2, incomingEvent.Args.Count);
        Assert.AreEqual("user test", incomingEvent.Args[0]);
        Assert.AreEqual("message test", incomingEvent.Args[1]);
        Assert.AreEqual("ChatHub", incomingEvent.HubName);
        Assert.AreEqual("SendMessage", incomingEvent.MethodName);
        Assert.AreEqual(1, evs.FirstOrDefault(e => e.Event.EventType == SignalrEventType.Incoming)?.CustomFields.GetValueOrDefault("A"));
        Assert.AreEqual(2, evs.FirstOrDefault(e => e.Event.EventType == SignalrEventType.Incoming)?.CustomFields.GetValueOrDefault("B"));
    }

    public class ChatHub : Hub
    {
        public async Task<string> SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);

            var scope = this.GetAuditScope();
            scope.SetCustomField("A", 1);
            this.AddCustomField("B", 2);

            return "return test";
        }
    }
}