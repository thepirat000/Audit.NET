using System.Collections.Generic;
using System.ServiceModel;

namespace Audit.WCF
{
    /// <summary>
    /// Provides a thread-safe way to share data for current WCF call.
    /// Original from: http://stackoverflow.com/a/1895958/122195
    /// </summary>
    /// <remarks>
    /// Used to store the audit scope per each method call, independently of the InstanceContextMode/ConcurrencyMode.
    /// </remarks>
    public class WcfOperationContext : IExtension<OperationContext>
    {
        private readonly IDictionary<string, object> items;

        private WcfOperationContext()
        {
            items = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the items data for this WCF call.
        /// </summary>
        public IDictionary<string, object> Items
        {
            get { return items; }
        }

        /// <summary>
        /// Gets the current context for this WCF call.
        /// </summary>
        public static WcfOperationContext Current
        {
            get
            {
                WcfOperationContext context = OperationContext.Current.Extensions.Find<WcfOperationContext>();
                if (context == null)
                {
                    context = new WcfOperationContext();
                    OperationContext.Current.Extensions.Add(context);
                }
                return context;
            }
        }

        /// <summary>
        /// Enables an extension object to find out when it has been aggregated. Called when the extension is added to the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Attach(OperationContext owner) { }
        /// <summary>
        /// Enables an object to find out when it is no longer aggregated. Called when an extension is removed from the <see cref="P:System.ServiceModel.IExtensibleObject`1.Extensions" /> property.
        /// </summary>
        /// <param name="owner">The extensible object that aggregates this extension.</param>
        public void Detach(OperationContext owner) { }
    }
}