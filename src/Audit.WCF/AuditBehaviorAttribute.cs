using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Audit.WCF
{
    /// <summary>
    /// AuditBehavior attribute to enable Audit for WCF Services and Methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AuditBehaviorAttribute : Attribute, IOperationBehavior, IServiceBehavior
    {
        /// <summary>
        /// Gets or sets the event type.
        /// Can contain the following placeholders:
        /// - {contract}: Replaced with the contract name (service interface name)
        /// - {operation}: Replaces with the operation name (service method name)
        /// </summary>
        public string EventType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditBehaviorAttribute"/> class.
        /// </summary>
        public AuditBehaviorAttribute()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditBehaviorAttribute"/> class.
        /// </summary>
        /// <param name="eventType">Type event type.</param>
        public AuditBehaviorAttribute(string eventType)
        {
            EventType = eventType;
        }

        #region IServiceBehavior
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var operation in serviceHostBase.Description.Endpoints.SelectMany(endpoint => endpoint.Contract.Operations))
            {
                if (operation.DeclaringContract.ContractType == typeof(IMetadataExchange))
                {
                    continue;
                }
                operation.Behaviors.Add(new AuditBehaviorAttribute(EventType));
            }
        }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
        #endregion

        #region IOperationBehavior
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            var invoker = new AuditOperationInvoker(dispatchOperation.Invoker, dispatchOperation, operationDescription, EventType);
            dispatchOperation.Invoker = invoker;
        }
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }
        public void Validate(OperationDescription operationDescription)
        {
        }
        #endregion
    }
}

