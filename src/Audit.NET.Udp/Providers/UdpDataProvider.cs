using Audit.Core;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Audit.Udp.Providers
{
    /// <summary>
    /// Send Audit Logs as UDP datagrams in a network
    /// </summary>
    /// <remarks>
    /// Settings:
    /// - RemoteAddress: remote host or multicast group to send the events.
    /// - RemotePort: remote port to send the events.
    /// - MulticastMode: to indicate if the RemoteAddress is a multicast group.
    /// - CustomSerializer: to specify a custom serialization method to send UDP packets.
    /// - CustomDeserializer: to specify a custom deserialization method to receive UDP packets.
    /// </remarks>
    public class UdpDataProvider : AuditDataProvider
    {
        /// <summary>
        /// Gets or sets the address of the remote host or multicast group to which the underlying UdpClient should send the audit events.
        /// </summary>
        public IPAddress RemoteAddress { get; set; }
        /// <summary>
        /// Gets or sets the port number of the remote host or multicast group to which the underlying UdpClient should send the audit events.
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// Gets or sets the multicast mode.
        /// Auto: (default) Multicast is automatically detected from the IP address.
        /// Enabled: Multicast explicitly enabled.
        /// Disabled: Multicast explicitly disabled.
        /// </summary>
        public MulticastMode MulticastMode { get; set; }
        /// <summary>
        /// Gets or sets a custom serialization method to send UDP packets.
        /// </summary>
        public Func<AuditEvent, byte[]> CustomSerializer { get; set; }
        /// <summary>
        /// Gets or sets a custom deserialization method to receive UDP packets.
        /// </summary>
        public Func<byte[], AuditEvent> CustomDeserializer { get; set; }

        private UdpClient _clientSend;
        private object _lockerSend = new object();

        private UdpClient _clientReceive;
        private object _lockReceive = new object();

        /// <summary>
        /// Sends an event to the network as an UDP datagram
        /// </summary>
        /// <param name="auditEvent">The audit event being created.</param>
        public override object InsertEvent(AuditEvent auditEvent)
        {
            var eventId = Guid.NewGuid();
            Send(eventId, auditEvent);
            return eventId;
        }

        /// <summary>
        /// Sends an event to the network as an UDP datagram, related to a previous event
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <param name="eventId">The event id being replaced.</param>
        public override void ReplaceEvent(object eventId, AuditEvent auditEvent)
        {
            Send(eventId, auditEvent);
        }

        /// <summary>
        /// Receives an event from the network as an UDP datagram. 
        /// The returned Task object will complete after the UDP packet has been received.
        /// </summary>
        public async Task<AuditEvent> ReceiveAsync()
        {
            var client = GetReceiverClient();
            var result = await client.ReceiveAsync();
            return DeserializeEvent(result.Buffer);
        }

        private void Send(object eventId, AuditEvent auditEvent)
        {
            var client = GetSendClient();
            var ep = new IPEndPoint(RemoteAddress, RemotePort);
            auditEvent.CustomFields["UdpEventId"] = eventId;
            var buffer = SerializeEvent(auditEvent);
            client.SendAsync(buffer, buffer.Length, ep).GetAwaiter().GetResult();
        }

        private byte[] SerializeEvent(AuditEvent auditEvent)
        {
            if (CustomSerializer != null)
            {
                return CustomSerializer.Invoke(auditEvent);
            }
            return Encoding.UTF8.GetBytes(auditEvent.ToJson());
        }

        private AuditEvent DeserializeEvent(byte[] data)
        {
            if (CustomDeserializer != null)
            {
                return CustomDeserializer.Invoke(data);
            }
            return JsonConvert.DeserializeObject<AuditEvent>(Encoding.UTF8.GetString(data));
        }

        private UdpClient GetSendClient()
        {
            lock (_lockerSend)
            {
                if (_clientSend == null)
                {
                    _clientSend = new UdpClient();
                    if (IsMulticast())
                    {
                        _clientSend.JoinMulticastGroup(RemoteAddress);
                    }
                }
            }
            return _clientSend;
        }

        private UdpClient GetReceiverClient()
        {
            lock (_lockReceive)
            {
                if (_clientReceive == null)
                {
                    _clientReceive = new UdpClient();
                    _clientReceive.ExclusiveAddressUse = false;
                    var ep = new IPEndPoint(IPAddress.Any, RemotePort);
                    _clientReceive.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _clientReceive.Client.Bind(ep);
                    if (IsMulticast())
                    {
                        _clientReceive.JoinMulticastGroup(RemoteAddress);
                    }
                }
            }
            return _clientReceive;
        }

        private bool IsMulticast()
        {
            if (MulticastMode == MulticastMode.Auto)
            {
                // 224.0.0.0 to 239.255.255.255 are multicast (https://en.wikipedia.org/wiki/Multicast_address)
                byte firstByte = RemoteAddress.GetAddressBytes()[0];
                return firstByte >= 224 && firstByte < 240;
            }
            else
            {
                return MulticastMode == MulticastMode.Enabled;
            }
        }
    }
}
