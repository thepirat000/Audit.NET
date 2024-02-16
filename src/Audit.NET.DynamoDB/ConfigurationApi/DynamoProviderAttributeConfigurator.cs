using System;
using System.Collections.Generic;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderAttributeConfigurator : IDynamoProviderAttributeConfigurator
    {
        internal Dictionary<string, Setting<object>> _attributes = new Dictionary<string, Setting<object>>();

        public IDynamoProviderAttributeConfigurator SetAttribute(string attributeName, Func<AuditEvent, object> valueBuilder)
        {
            _attributes[attributeName] = valueBuilder;
            return this;
        }
    }
}
