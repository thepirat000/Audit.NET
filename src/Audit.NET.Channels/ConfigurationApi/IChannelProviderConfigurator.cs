using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Audit.Channels.Configuration
{
    public interface IChannelProviderConfigurator
    {
        /// <summary>
        /// Creates a channel to store the audit events with an explicit limit on the number of items that can be stored.
        /// </summary>
        /// <param name="options">The bounded channel options to use</param>
        void Bounded(BoundedChannelOptions options);

        /// <summary>
        /// Creates a channel to store the audit events with an explicit limit on the number of items that can be stored.
        /// </summary>
        /// <param name="capacity">The bounded channel capacity</param>
        void Bounded(int capacity);

        /// <summary>
        /// Creates a channel to store the audit events with no imposed limit on the number of items that can be stored.
        /// </summary>
        /// <param name="options">The unbounded channel options to use</param>
        void Unbounded(UnboundedChannelOptions options);

        /// <summary>
        /// Creates a channel to store the audit events with no imposed limit on the number of items and using the default options.
        /// </summary>
        void Unbounded();
    }
}
