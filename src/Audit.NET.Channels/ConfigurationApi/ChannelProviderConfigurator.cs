using System.Threading.Channels;

namespace Audit.Channels.Configuration
{
    public class ChannelProviderConfigurator : IChannelProviderConfigurator
    {
        internal BoundedChannelOptions? _boundedChannelOptions;
        internal UnboundedChannelOptions? _unboundedChannelOptions;
        
        public void Bounded(BoundedChannelOptions options)
        {
            _boundedChannelOptions = options;
            _unboundedChannelOptions = null;
        }

        public void Bounded(int capacity)
        {
            _boundedChannelOptions = new BoundedChannelOptions(capacity);
            _unboundedChannelOptions = null;
        }

        public void Unbounded(UnboundedChannelOptions options)
        {
            _boundedChannelOptions = null;
            _unboundedChannelOptions = options;
        }

        public void Unbounded()
        {
            _boundedChannelOptions = null;
            _unboundedChannelOptions = new UnboundedChannelOptions();
        }
    }
}