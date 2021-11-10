using Audit.Core;
using Audit.Udp.Providers;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Audit.Udp.Configuration
{
    public class UdpProviderConfigurator : IUdpProviderConfigurator
    {
        internal IPAddress _remoteAddress;
        internal int _remotePort;
        internal MulticastMode _multicastMode;
        internal Func<AuditEvent, byte[]> _customSerializer;
        internal Func<byte[], AuditEvent> _customDeserializer;

        public IUdpProviderConfigurator RemoteAddress(IPAddress address)
        {
            _remoteAddress = address;
            return this;
        }

        public IUdpProviderConfigurator RemoteAddress(string address)
        {
            _remoteAddress = GetIPAddress(address);
            return this;
        }

        public IUdpProviderConfigurator RemotePort(int port)
        {
            _remotePort = port;
            return this;
        }

        public IUdpProviderConfigurator MulticastMode(MulticastMode mode)
        {
            _multicastMode = mode;
            return this;
        }

        public IUdpProviderConfigurator CustomSerializer(Func<AuditEvent, byte[]> customSerializer)
        {
            _customSerializer = customSerializer;
            return this;
        }

        public IUdpProviderConfigurator CustomDeserializer(Func<byte[], AuditEvent> customDeserializer)
        {
            _customDeserializer = customDeserializer;
            return this;
        }

        internal static IPAddress GetIPAddress(string address)
        {
            if (!IPAddress.TryParse(address, out IPAddress addr))
            {
                var hostEntry = Dns.GetHostEntryAsync(address).GetAwaiter().GetResult();
                addr = hostEntry.AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            }
            return addr;
        }
    }
}
