using Audit.Core.ConfigurationApi;
using Audit.Udp.Configuration;
using Audit.Udp.Providers;
using System;
using System.Net;

namespace Audit.Core
{
    public static class UdpProviderConfiguratorExtensions
    {
        /// <summary>
        /// Send the events over a network as UDP packets.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="remoteAddress">The address of the remote host or multicast group to which the underlying UdpClient should send the audit events.</param>
        /// <param name="remotePort">The port number of the remote host or multicast group to which the underlying UdpClient should send the audit events.</param>
        /// <param name="multicastMode">The multicast mode.</param>
        /// <param name="customSerializer">A custom serialization method, or NULL to use the json/UTF-8 default</param>
        /// <param name="customDeserializer">A custom deserialization method, or NULL to use the json/UTF-8 default</param>
        /// <returns></returns>
        public static ICreationPolicyConfigurator UseUdp(this IConfigurator configurator, IPAddress remoteAddress, int remotePort,
            MulticastMode multicastMode = MulticastMode.Auto, Func<AuditEvent, byte[]> customSerializer = null, Func<byte[], AuditEvent> customDeserializer = null)
        {
            Configuration.DataProvider = new UdpDataProvider()
            {
                RemoteAddress = remoteAddress,
                RemotePort = remotePort,
                MulticastMode = multicastMode,
                CustomSerializer = customSerializer,
                CustomDeserializer = customDeserializer
            };
            return new CreationPolicyConfigurator();
        }

        /// <summary>
        /// Send the events over a network as UDP packets.
        /// </summary>
        /// <param name="configurator">The Audit.NET Configurator</param>
        /// <param name="config">The UDP provider configuration.</param>
        public static ICreationPolicyConfigurator UseUdp(this IConfigurator configurator, Action<IUdpProviderConfigurator> config)
        {
            var udpConfig = new UdpProviderConfigurator();
            config.Invoke(udpConfig);
            return UseUdp(configurator, udpConfig._remoteAddress, udpConfig._remotePort, udpConfig._multicastMode,
                udpConfig._customSerializer, udpConfig._customDeserializer);
        }
    }
}