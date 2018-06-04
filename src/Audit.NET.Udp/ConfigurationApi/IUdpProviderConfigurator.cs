using Audit.Core;
using Audit.Udp.Providers;
using System;
using System.Net;

namespace Audit.Udp.Configuration
{
    /// <summary>
    /// Provides a configuration for the Udp data provider
    /// </summary>
    public interface IUdpProviderConfigurator
    {
        /// <summary>
        /// Specifies the address of the remote host or multicast group to which the underlying UdpClient should send the audit events.
        /// </summary>
        /// <param name="address">The IP address.</param>
        IUdpProviderConfigurator RemoteAddress(IPAddress address);
        /// <summary>
        /// Specifies the address of the remote host or multicast group to which the underlying UdpClient should send the audit events.
        /// </summary>
        /// <param name="address">The host name or IP address.</param>
        IUdpProviderConfigurator RemoteAddress(string address);
        /// <summary>
        /// Specifies the port of the remote host or multicast group to which the underlying UdpClient should send the audit events.
        /// </summary>
        /// <param name="port">The port number.</param>
        IUdpProviderConfigurator RemotePort(int port);
        /// <summary>
        /// Specifies the multicast mode.
        /// Auto: (default) Multicast is automatically detected from the IP address.
        /// Enabled: Multicast explicitly enabled.
        /// Disabled: Multicast explicitly disabled.
        /// </summary>
        /// <param name="mode">The multicast mode.</param>
        IUdpProviderConfigurator MulticastMode(MulticastMode mode);
        /// <summary>
        /// Specifies a custom serialization method for the UDP packets.
        /// </summary>
        /// <param name="customSerializer">The custom serialization method</param>
        IUdpProviderConfigurator CustomSerializer(Func<AuditEvent, byte[]> customSerializer);
        /// <summary>
        /// Specifies a custom deserialization method for the UDP packets.
        /// </summary>
        /// <param name="customDeserializer">The custom deserialization method</param>
        IUdpProviderConfigurator CustomDeserializer(Func<byte[], AuditEvent> customDeserializer);
    }
}
