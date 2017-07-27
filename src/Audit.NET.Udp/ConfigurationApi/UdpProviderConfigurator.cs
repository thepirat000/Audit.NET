using Audit.Core;
using Audit.Udp.Providers;
using System;
using System.Net;

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
        public IUdpProviderConfigurator RemoteAddress(string ipString)
        {
            _remoteAddress = IPAddress.Parse(ipString);
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
    }
}
