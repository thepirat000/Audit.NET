using System;
using Audit.Core;

namespace Audit.DynamoDB.Configuration
{
    /// <summary>
    /// Provides a Table Attribute level configuration for DynamoDB provider
    /// </summary>
    public interface IDynamoProviderAttributeConfigurator
    {
        /// <summary>
        /// Adds an extra attribute to the document
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <param name="valueBuilder">A function of the audit event that returns the attribute value</param>
        IDynamoProviderAttributeConfigurator SetAttribute(string attributeName, Func<AuditEvent, object> valueBuilder);
    }

}
