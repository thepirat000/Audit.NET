using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Audit.Channels.Configuration;
using Audit.Core;

namespace Audit.Channels.Providers
{
    /// <summary>
    /// Data provider to store the audit events in a Channel (from System.Threading.Channels) that can be accessed to consume the events.
    /// This data provider does not allow replacing events, the CreationPolicy InsertOnStartReplaceOnEnd is not allowed when using this data provider.
    /// </summary>
    public class ChannelDataProvider : AuditDataProvider
    {
        private readonly Channel<AuditEvent> _channel;

        /// <summary>
        /// Gets the number of audit events currently stored in memory, or -1 if the count is not available for the channel.
        /// </summary>
        public int Count => _channel.Reader.CanCount ? _channel.Reader.Count : -1;
        
        public ChannelDataProvider()
        {
            _channel = Channel.CreateUnbounded<AuditEvent>();
        }

        public ChannelDataProvider(Action<IChannelProviderConfigurator> config)
        {
            var chConfig = new ChannelProviderConfigurator();
            if (config != null)
            {
                config.Invoke(chConfig);
                if (chConfig._boundedChannelOptions != null)
                {
                    _channel = Channel.CreateBounded<AuditEvent>(chConfig._boundedChannelOptions);
                }
                else if (chConfig._unboundedChannelOptions != null)
                {
                    _channel = Channel.CreateUnbounded<AuditEvent>(chConfig._unboundedChannelOptions);
                }
                else
                {
                    _channel = Channel.CreateUnbounded<AuditEvent>();
                }
            }
            else
            {
                _channel = Channel.CreateUnbounded<AuditEvent>();
            }
        }

        public ChannelDataProvider(Channel<AuditEvent> channel)
        {
            _channel = channel;
        }

        public override object InsertEvent(AuditEvent auditEvent)
        {
            return InsertEventAsync(auditEvent).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override async Task<object> InsertEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            await _channel.Writer.WriteAsync(auditEvent, cancellationToken).ConfigureAwait(false);

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed while observing the cancellation token.
        /// </summary>
        public AuditEvent Take(CancellationToken cancellationToken = default)
        {
            return TakeAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed while observing the cancellation token.
        /// </summary>
        public async Task<AuditEvent> TakeAsync(CancellationToken cancellationToken = default)
        {
            return await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Takes an audit event from the internal collection and remove it from the queue.
        /// It will block until there is an audit event to be consumed, the timeout is reached or the cancellation token is triggered.
        /// Returns NULL if no audit event is available.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or (-1) to wait indefinitely.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task<AuditEvent> TryTakeAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(millisecondsTimeout).Token);
            var dataAvailable = false;
            try
            {
                dataAvailable = await _channel.Reader.WaitToReadAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Throw if Cancelled
                cancellationToken.ThrowIfCancellationRequested();

                // Timeout
                return default;
            }
            
            if (dataAvailable && _channel.Reader.TryRead(out var auditEvent))
            {
                return auditEvent;
            }

            return default;
        }

        /// <summary>
        /// Gets the channel used by this provider.
        /// </summary>
        public Channel<AuditEvent> GetChannel()
        {
            return _channel;
        }
    }
}
