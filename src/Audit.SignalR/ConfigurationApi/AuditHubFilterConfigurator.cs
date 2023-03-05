using System;

namespace Audit.SignalR.Configuration
{
    internal class AuditHubFilterConfigurator : IAuditHubFilterConfigurator
    {
        internal Func<SignalrEventIncoming, bool> _incomingEventsFilter;
        internal Func<SignalrEventConnect, bool> _connectEventsFilter;
        internal Func<SignalrEventDisconnect, bool> _disconnectEventsFilter;
#if ASP_NET
        internal Func<SignalrEventOutgoing, bool> _outgoingEventsFilter;
        internal Func<SignalrEventReconnect, bool> _reconnectEventsFilter;
        internal Func<SignalrEventError, bool> _errorEventsFilter;
#endif

        public IAuditHubFilterConfigurator IncludeConnectEvent(bool include = true)
        {
            _connectEventsFilter = connect => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeConnectEvent(Func<SignalrEventConnect, bool> predicate)
        {
            _connectEventsFilter = predicate;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeDisconnectEvent(bool include = true)
        {
            _disconnectEventsFilter = disconnect => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeDisconnectEvent(Func<SignalrEventDisconnect, bool> predicate)
        {
            _disconnectEventsFilter = predicate;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeIncomingEvent(bool include = true)
        {
            _incomingEventsFilter = incoming => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeIncomingEvent(Func<SignalrEventIncoming, bool> predicate)
        {
            _incomingEventsFilter = predicate;
            return this;
        }

#if ASP_NET
        public IAuditHubFilterConfigurator IncludeErrorEvent(bool include = true)
        {
            _errorEventsFilter = error => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeErrorEvent(Func<SignalrEventError, bool> predicate)
        {
            _errorEventsFilter = predicate;
            return this;
        }
        
        public IAuditHubFilterConfigurator IncludeOutgoingEvent(bool include = true)
        {
            _outgoingEventsFilter = outgoing => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeOutgoingEvent(Func<SignalrEventOutgoing, bool> predicate)
        {
            _outgoingEventsFilter = predicate;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeReconnectEvent(bool include = true)
        {
            _reconnectEventsFilter = reconnect => include;
            return this;
        }

        public IAuditHubFilterConfigurator IncludeReconnectEvent(Func<SignalrEventReconnect, bool> predicate)
        {
            _reconnectEventsFilter = predicate;
            return this;
        }
#endif
    }
}