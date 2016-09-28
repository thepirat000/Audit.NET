using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Audit.Core;
using System.Threading;
using Audit.Core.Extensions;

namespace Audit.WCF
{
    /// <summary>
    /// Operation invoker to intercept service calls to generate Audit Logs
    /// </summary>
    public class AuditOperationInvoker : IOperationInvoker
    {
        #region Private Fields
        private string _eventType;
        private IOperationInvoker _baseInvoker;
        private DispatchOperation _operation;
        private OperationDescription _operationDescription;
        #endregion

        #region Constructors
        public AuditOperationInvoker(IOperationInvoker baseInvoker, DispatchOperation operation, OperationDescription operationDescription,
            string eventType)
        {
            _baseInvoker = baseInvoker;
            _operationDescription = operationDescription;
            _operation = operation;
            _eventType = eventType ?? "{operation}";
        }
        #endregion

        #region Public Methods
        public bool IsSynchronous
        {
            get { return true; }
        }

        public object[] AllocateInputs()
        {
            return _baseInvoker.AllocateInputs();
        }

        /// <summary>
        /// Returns an object and a set of output objects from an instance and set of input objects.
        /// </summary>
        /// <param name="instance">The object to be invoked.</param>
        /// <param name="inputs">The inputs to the method.</param>
        /// <param name="outputs">The outputs from the method.</param>
        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            object result = null;
            // Create the Wcf audit event
            var auditWcfEvent = CreateWcfAuditEvent(instance, inputs);
            // Create the audit scope
            var eventType = _eventType.Replace("{contract}", auditWcfEvent.ContractName).Replace("{operation}", auditWcfEvent.OperationName);
            var auditScope = AuditScope.Create(eventType, null, EventCreationPolicy.Manual, GetAuditDataProvider(instance));
            // Set the initial WcfEvent data
            auditScope.SetCustomField(AuditBehavior.CustomFieldName, auditWcfEvent);
            // Store a reference to this audit scope on a thread static field
            AuditBehavior.CurrentAuditScope = auditScope;
            try
            {
                result = _baseInvoker.Invoke(instance, inputs, out outputs);
            }
            catch (Exception ex)
            {
                AuditBehavior.CurrentAuditScope = null;
                auditWcfEvent.Fault = GetWcfFaultData(ex);
                auditWcfEvent.Success = false;
                SaveAuditScope(auditScope, auditWcfEvent);
                throw;
            }
            AuditBehavior.CurrentAuditScope = null;
            auditWcfEvent.OutputParameters = GetEventElements(outputs);
            auditWcfEvent.Result = new AuditWcfEventElement(result);
            SaveAuditScope(auditScope, auditWcfEvent);
            return result;
        }
        #endregion

        #region Private Methods
        private AuditWcfEvent CreateWcfAuditEvent(object instance, object[] inputs)
        {
            var securityContext = ServiceSecurityContext.Current;
            var operationContext = OperationContext.Current;
            var imProps = OperationContext.Current.IncomingMessageProperties;
            var endpoint = imProps.ContainsKey(RemoteEndpointMessageProperty.Name) ? imProps[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty : null;
            return new AuditWcfEvent()
            {
                Action = _operation.Action,
                ReplyAction = _operation.ReplyAction,
                ContractName = _operationDescription.DeclaringContract.Name,
                MethodSignature = _operationDescription.SyncMethod.ToString(),
                OperationName = _operation.Name,
                ClientAddress = endpoint?.Address,
                HostAddress = string.Join(", ", operationContext.InstanceContext.Host?.BaseAddresses?.Select(a => a.AbsoluteUri)),
                InstanceQualifiedName = instance.GetType().AssemblyQualifiedName,
                InputParameters = GetEventElements(inputs),
                IdentityName = securityContext?.WindowsIdentity?.Name ?? securityContext?.PrimaryIdentity?.Name,
                Success = true
            };
        }

        /// <summary>
        /// Get the dataprovider from property AuditDataProvider
        /// </summary>
        private AuditDataProvider GetAuditDataProvider(object instance)
        {
            var prop = instance.GetType().GetProperty("AuditDataProvider", typeof(AuditDataProvider));
            if (prop != null)
            {
                return prop.GetGetMethod().Invoke(instance, null) as AuditDataProvider;
            }
            return null;
        }

        private AuditWcfEventFault GetWcfFaultData(Exception ex)
        {
            var result = new AuditWcfEventFault();
            result.Exception = ex.GetExceptionInfo();
            if (ex is FaultException)
            {
                result.FaultType = "Fault";
                var fault = ex as FaultException;
                if (fault.GetType().GetProperty("Detail") != null)
                {
                    var detail = fault.GetType().GetProperty("Detail").GetGetMethod().Invoke(fault, null);
                    result.FaultDetail = new AuditWcfEventElement(detail);
                }
                result.FaultCode = fault.Code?.Name;
                result.FaultAction = fault.Action;
                result.FaultReason = fault.Reason?.ToString();
            }
            else
            {
                result.FaultType = "Exception";
            }
            return result;
        }

        private void SaveAuditScope(AuditScope auditScope, AuditWcfEvent auditWcfEvent)
        {
            auditScope.SetCustomField(AuditBehavior.CustomFieldName, auditWcfEvent);
            auditScope.Save();
        }

        private List<AuditWcfEventElement> GetEventElements(object[] objects)
        {
            if (objects == null)
            {
                return null;
            }
            var result = new List<AuditWcfEventElement>(objects.Length);
            for (int i = 0; i < objects.Length; i++)
            {
                result.Add(new AuditWcfEventElement(objects[i]));
            }
            return result;
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}