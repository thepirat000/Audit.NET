using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.NET.AmazonQLDB.ConfigurationApi
{
    public class AmazonQldbProviderAttributeConfigurator : IAmazonQldbProviderAttributeConfigurator
    {
        internal Dictionary<string, Func<AuditEvent, object>> _attributes = new Dictionary<string, Func<AuditEvent, object>>();

        public IAmazonQldbProviderAttributeConfigurator SetAttribute(string attributeName, Func<AuditEvent, object> valueBuilder)
        {
            _attributes[attributeName] = valueBuilder;
            return this;
        }
    }
}
