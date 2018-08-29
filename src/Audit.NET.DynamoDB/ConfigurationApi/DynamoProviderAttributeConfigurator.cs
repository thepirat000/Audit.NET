using System;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DocumentModel;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    public class DynamoProviderAttributeConfigurator : IDynamoProviderAttributeConfigurator
    {
        internal Dictionary<string, Func<AuditEvent, Primitive>> _attributes = new Dictionary<string, Func<AuditEvent, Primitive>>();

        public IDynamoProviderAttributeConfigurator SetAttribute(string attributeName, Func<AuditEvent, Primitive> valueBuilder)
        {
            _attributes[attributeName] = valueBuilder;
            return this;
        }

        public IDynamoProviderAttributeConfigurator SetAttribute(string attributeName, Primitive value)
        {
            _attributes[attributeName] = _ => value;
            return this;
        }
    }

}
