using System;
using Audit.Core;

namespace Audit.AmazonQLDB.ConfigurationApi
{
    /// <summary>
    /// Provides a Ledger Attribute level configuration for AmazonQLDB provider
    /// </summary>
    public interface IAmazonQldbProviderAttributeConfigurator
    {
        /// <summary>
        /// Adds an extra attribute to the document
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <param name="valueBuilder">A function of the audit event that returns the attribute value</param>
        IAmazonQldbProviderAttributeConfigurator SetAttribute(string attributeName, Func<AuditEvent, object> valueBuilder);
    }

}
