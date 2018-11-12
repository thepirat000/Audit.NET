using System;
using System.Collections.Generic;
using System.Text;

namespace Audit.EntityFramework.ConfigurationApi
{
    public interface IEntityFrameworkProviderConfiguratorExtra
    {
        /// <summary>
        /// Avoids the property values copy from the entity to the audited entity
        /// </summary>
        /// <param name="ignore">Set to true to avoid the property values copy from the entity to the audited entity (default is true)</param>
        void IgnoreMatchedProperties(bool ignore = true);
    }
}
