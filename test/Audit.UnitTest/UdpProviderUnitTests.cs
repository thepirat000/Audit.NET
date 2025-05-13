using Audit.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Audit.Udp.Configuration;

namespace Audit.UnitTest
{
    public class UdpProviderUnitTests
    {
        [Test]
        [TestCase("225.0.0.1", 2222, true)]
        [TestCase("127.0.0.1", 12366, false)]
        [TestCase("localhost", 12366, false)]
        public void Test_UdpDataProvider_BasicTest(string ip, int port, bool multicast)
        {
            stop = false;
            events.Clear();
            var p = new Udp.Providers.UdpDataProvider();
            p.RemoteAddress = UdpProviderConfigurator.GetIPAddress(ip);
            p.RemotePort = port;
            using var cts = new CancellationTokenSource();
            var re = new ManualResetEvent(false);
            var listener = Task.Factory.StartNew(() => { Listen(re, ip, port, multicast); }, cts.Token);
            re.WaitOne();
            Task.Delay(1000).Wait();
            using (var scope = new AuditScopeFactory().Create("Test_UdpDataProvider_BasicTest", null, EventCreationPolicy.InsertOnStartReplaceOnEnd, p))
            {
                Task.Delay(100).Wait();
            }
            Task.Delay(2000).Wait();
            stop = true;
            cts.Cancel();
            Task.Delay(2000).Wait();
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].EventType, Is.EqualTo("Test_UdpDataProvider_BasicTest"));
            Assert.That(events[1].EventType, Is.EqualTo("Test_UdpDataProvider_BasicTest"));
            Assert.NotNull(events[0].CustomFields["UdpEventId"]);
            Assert.That(events[1].CustomFields["UdpEventId"].ToString(), Is.EqualTo(events[0].CustomFields["UdpEventId"].ToString()));
        }

        [Test]
        [TestCase("225.0.0.1", 2223, true)]
        [TestCase("127.0.0.1", 12367, false)]
        [TestCase("localhost", 12367, false)]
        public async Task Test_UdpDataProvider_BasicTest_Async(string ip, int port, bool multicast)
        {
            stop = false;
            events.Clear();

            Audit.Core.Configuration.Setup()
                .UseUdp(_ => _
                    .RemoteAddress(ip)
                    .RemotePort(port));

            using var cts = new CancellationTokenSource();
            var re = new ManualResetEvent(false);
            var listener = Task.Factory.StartNew(() => { Listen(re, ip, port, multicast); }, cts.Token);
            re.WaitOne();
            await Task.Delay(1000);
            using (var scope = await new AuditScopeFactory().CreateAsync(new AuditScopeOptions() { EventType = "Test_UdpDataProvider_BasicTest", CreationPolicy = EventCreationPolicy.InsertOnStartReplaceOnEnd }))
            {
                Task.Delay(100).Wait();
                await scope.SaveAsync();
                scope.Discard();
            }
            await Task.Delay(2000);
            stop = true;
            cts.Cancel();
            await Task.Delay(2000);
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].EventType, Is.EqualTo("Test_UdpDataProvider_BasicTest"));
            Assert.That(events[1].EventType, Is.EqualTo("Test_UdpDataProvider_BasicTest"));
            Assert.NotNull(events[0].CustomFields["UdpEventId"]);
            Assert.That(events[1].CustomFields["UdpEventId"].ToString(), Is.EqualTo(events[0].CustomFields["UdpEventId"].ToString()));
        }

        private static bool stop = false;
        private static object locker = new object();
        private static List<AuditEvent> events = new List<AuditEvent>();

        private void Listen(ManualResetEvent re, string address, int port, bool isMulticast)
        {
            var dp = new Udp.Providers.UdpDataProvider();
            dp.RemoteAddress = UdpProviderConfigurator.GetIPAddress(address);
            dp.RemotePort = port;
            dp.MulticastMode = isMulticast ? Udp.Providers.MulticastMode.Enabled : Udp.Providers.MulticastMode.Disabled;
            re.Set();
            while (!stop)
            {
                var ev = dp.ReceiveAsync().Result;
                lock (locker)
                {
                    events.Add(ev);
                }
            }
        }

        [Test]
        [TestCase("225.0.0.1", 2227, true, 10)]
        [TestCase("127.0.0.1", 12369, false, 10)]
        [TestCase("localhost", 12369, false, 10)]
        [TestCase("226.1.2.15", 5569, true, 50)]
        public void Test_UdpDataProvider_MultiThread(string ip, int port, bool multicast, int N)
        {
            stop = false;
            events.Clear();
            var p = new Udp.Providers.UdpDataProvider();
            p.RemoteAddress = UdpProviderConfigurator.GetIPAddress(ip);
            p.RemotePort = port;
            using var cts = new CancellationTokenSource();
            var re = new ManualResetEvent(false);
            var listener = Task.Factory.StartNew(() => { Listen(re, ip, port, multicast); }, cts.Token);
            re.WaitOne();
            Task.Delay(1500).Wait();

            var tasks = new List<Task>();
            for (int i = 0; i < N; i++)
            {
                int a = i;
                tasks.Add(new Task(() =>
                {
                    using (var scope = new AuditScopeFactory().Create("Test_UdpDataProvider_MultiThread_" + a, null, EventCreationPolicy.InsertOnEnd, p))
                    {
                    }
                }));
            }
            for (int i = 0; i < N; i++)
            {
                tasks[i].Start();
            }

            Task.WaitAll(tasks.ToArray());
            Task.Delay(1000).Wait();
            stop = true;
            cts.Cancel();
            Task.Delay(1000).Wait();

            Assert.That(events.Count, Is.EqualTo(N));
            Assert.That(events[0].EventType.StartsWith("Test_UdpDataProvider_MultiThread_"), Is.True);
            Assert.NotNull(events[0].CustomFields["UdpEventId"].ToString());
        }

        [Test]
        [TestCase("226.1.2.15", 6688, true)]
        public void Test_UdpDataProvider_BigPacket(string ip, int port, bool multicast)
        {
            stop = false;
            events.Clear();
            var p = new Udp.Providers.UdpDataProvider();
            p.RemoteAddress = UdpProviderConfigurator.GetIPAddress(ip);
            p.RemotePort = port;

            using var cts = new CancellationTokenSource();
            var re = new ManualResetEvent(false);
            var listener = Task.Factory.StartNew(() => { Listen(re, ip, port, multicast); }, cts.Token);
            re.WaitOne();
            Task.Delay(1000).Wait();

            var target = Enumerable.Range(1, 10000).Select(_ => (byte)255).ToArray();
            using (var scope = new AuditScopeFactory().Create("Test_UdpDataProvider_BigPacket", () => target, EventCreationPolicy.InsertOnEnd, p))
            {
            }

            Task.Delay(1000).Wait();
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        [TestCase("226.3.4.56", 8899, true)]
        public void Test_UdpDataProvider_PacketOverflow(string ip, int port, bool multicast)
        {
            var p = new Udp.Providers.UdpDataProvider();
            p.RemoteAddress = UdpProviderConfigurator.GetIPAddress(ip);
            p.RemotePort = port;
            var target = Enumerable.Range(1, 66000).Select(_ => (byte)255).ToArray();
            Assert.Throws<SocketException>(() =>
            {
                using (var scope = new AuditScopeFactory().Create("Test_UdpDataProvider_BigPacket", () => target, EventCreationPolicy.InsertOnEnd, p))
                {
                }
            });
        }
    }
}
