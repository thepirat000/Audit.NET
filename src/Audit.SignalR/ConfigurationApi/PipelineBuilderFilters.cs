using System;

namespace Audit.SignalR.Configuration
{
    internal class PipelineBuilderFilters : IPipelineBuilderFilters
    {
        internal Func<SignalrEventIncoming, bool> _incomingEventsFilter;
        internal Func<SignalrEventOutgoing, bool> _outgoingEventsFilter;
        internal Func<SignalrEventConnect, bool> _connectEventsFilter;
        internal Func<SignalrEventDisconnect, bool> _disconnectEventsFilter;
        internal Func<SignalrEventReconnect, bool> _reconnectEventsFilter;
        internal Func<SignalrEventError, bool> _errorEventsFilter;

        public IPipelineBuilderFilters IncludeConnectEvent(bool include = true)
        {
            _connectEventsFilter = connect => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeConnectEvent(Func<SignalrEventConnect, bool> predicate)
        {
            _connectEventsFilter = predicate;
            return this;
        }

        public IPipelineBuilderFilters IncludeDisconnectEvent(bool include = true)
        {
            _disconnectEventsFilter = disconnect => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeDisconnectEvent(Func<SignalrEventDisconnect, bool> predicate)
        {
            _disconnectEventsFilter = predicate;
            return this;
        }

        public IPipelineBuilderFilters IncludeErrorEvent(bool include = true)
        {
            _errorEventsFilter = error => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeErrorEvent(Func<SignalrEventError, bool> predicate)
        {
            _errorEventsFilter = predicate;
            return this;
        }

        public IPipelineBuilderFilters IncludeIncomingEvent(bool include = true)
        {
            _incomingEventsFilter = incoming => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeIncomingEvent(Func<SignalrEventIncoming, bool> predicate)
        {
            _incomingEventsFilter = predicate;
            return this;
        }

        public IPipelineBuilderFilters IncludeOutgoingEvent(bool include = true)
        {
            _outgoingEventsFilter = outgoing => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeOutgoingEvent(Func<SignalrEventOutgoing, bool> predicate)
        {
            _outgoingEventsFilter = predicate;
            return this;
        }

        public IPipelineBuilderFilters IncludeReconnectEvent(bool include = true)
        {
            _reconnectEventsFilter = reconnect => include;
            return this;
        }

        public IPipelineBuilderFilters IncludeReconnectEvent(Func<SignalrEventReconnect, bool> predicate)
        {
            _reconnectEventsFilter = predicate;
            return this;
        }
    }
}