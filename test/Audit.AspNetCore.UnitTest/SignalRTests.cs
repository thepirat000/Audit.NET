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

namespace Audit.AspNetCore.UnitTest;

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

        Assert.That(messages.Count, Is.EqualTo(1));
        Assert.That(evs.Count, Is.EqualTo(3));
        Assert.That(incomingEvent, Is.Not.Null);

        Assert.That(messages[0], Is.EqualTo("user test: message test"));

        Assert.That(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Connect), Is.True);
        Assert.That(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Incoming), Is.True);
        Assert.That(evs.Any(e => e.GetSignalrEvent().EventType == SignalrEventType.Disconnect), Is.True);

        Assert.That(incomingEvent.Result, Is.EqualTo("return test"));
        Assert.That(incomingEvent.Args.Count, Is.EqualTo(2));
        Assert.That(incomingEvent.Args[0], Is.EqualTo("user test"));
        Assert.That(incomingEvent.Args[1], Is.EqualTo("message test"));
        Assert.That(incomingEvent.HubName, Is.EqualTo("ChatHub"));
        Assert.That(incomingEvent.MethodName, Is.EqualTo("SendMessage"));
        Assert.That(evs.FirstOrDefault(e => e.Event.EventType == SignalrEventType.Incoming)?.CustomFields.GetValueOrDefault("A"), Is.EqualTo(1));
        Assert.That(evs.FirstOrDefault(e => e.Event.EventType == SignalrEventType.Incoming)?.CustomFields.GetValueOrDefault("B"), Is.EqualTo(2));
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