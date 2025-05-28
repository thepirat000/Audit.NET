using System;

using Audit.Core;

using ImmuDB;

namespace Audit.ImmuDB.ConfigurationApi
{
    public class ImmuDbProviderConfigurator : IImmuDbProviderConfigurator
    {
        internal Action<ImmuClientBuilder> _clientBuilderAction;
        internal Setting<string> _databaseName;
        internal Setting<string> _username;
        internal Setting<string> _password;
        internal Setting<TimeSpan?> _timeout;
        internal Func<AuditEvent, byte[]> _keySelector;
        internal Func<AuditEvent, byte[]> _valueSelector;
        internal bool _useVerifiedMethods;

        public IImmuDbProviderConfigurator ClientBuilder(Action<ImmuClientBuilder> clientBuilderAction)
        {
            _clientBuilderAction = clientBuilderAction;
            return this;
        }

        public IImmuDbProviderConfigurator Database(string databaseName)
        {
            _databaseName = databaseName;
            return this;
        }

        public IImmuDbProviderConfigurator Database(Func<AuditEvent, string> databaseNameBuilder)
        {
            _databaseName = databaseNameBuilder;
            return this;
        }
        public IImmuDbProviderConfigurator Username(string username)
        {
            _username = username;
            return this;
        }
        public IImmuDbProviderConfigurator Username(Func<AuditEvent, string> usernameBuilder)
        {
            _username = usernameBuilder;
            return this;
        }
        public IImmuDbProviderConfigurator Password(string password)
        {
            _password = password;
            return this;
        }
        public IImmuDbProviderConfigurator Password(Func<AuditEvent, string> passwordBuilder)
        {
            _password = passwordBuilder;
            return this;
        }
        public IImmuDbProviderConfigurator KeySelector(Func<AuditEvent, byte[]> keySelector)
        {
            _keySelector = keySelector;
            return this;
        }

        public IImmuDbProviderConfigurator KeySelector(Func<AuditEvent, string> keySelector)
        {
            _keySelector = auditEvent => Utils.ToByteArray(keySelector.Invoke(auditEvent));
            return this;
        }

        public IImmuDbProviderConfigurator ValueSelector(Func<AuditEvent, byte[]> valueSelector)
        {
            _valueSelector = valueSelector;
            return this;
        }
        public IImmuDbProviderConfigurator ValueSelector(Func<AuditEvent, string> valueSelector)
        {
            _valueSelector = auditEvent => Utils.ToByteArray(valueSelector.Invoke(auditEvent));
            return this;
        }

        public IImmuDbProviderConfigurator UseVerifiedMethods(bool useVerifiedMethods = true)
        {
            _useVerifiedMethods = useVerifiedMethods;
            return this;
        }

        public IImmuDbProviderConfigurator ExpirationTimeout(TimeSpan timespan)
        {
            _timeout = timespan;
            return this;
        }

        public IImmuDbProviderConfigurator ExpirationTimeout(Func<AuditEvent, TimeSpan?> timespanBuilder)
        {
            _timeout = timespanBuilder;
            return this;
        }
    }
}