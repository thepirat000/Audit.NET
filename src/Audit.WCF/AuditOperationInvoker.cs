using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Audit.Core;
using Audit.Core.Extensions;

namespace Audit.WCF
{
    /// <summary>
    /// Operation invoker to intercept service calls to generate Audit Logs
    /// </summary>
    public class AuditOperationInvoker : IOperationInvoker
    {
        #region Private Fields
        private readonly string _eventType;
        private readonly IOperationInvoker _baseInvoker;
        private readonly DispatchOperation _operation;
        private readonly OperationDescription _operationDescription;
        private readonly EventCreationPolicy? _creationPolicy;
        #endregion

        #region Constructors
        public AuditOperationInvoker(IOperationInvoker baseInvoker, DispatchOperation operation, OperationDescription operationDescription,
            string eventType, EventCreationPolicy? creationPolicy)
        {
            _baseInvoker = baseInvoker;
            _operationDescription = operationDescription;
            _operation = operation;
            _eventType = eventType ?? "{operation}";
            _creationPolicy = creationPolicy;
        }
        #endregion

        #region Public Methods
        public bool IsSynchronous => _baseInvoker.IsSynchronous;

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
            var eventType = _eventType.Replace("{contract}", auditWcfEvent.ContractName)
                .Replace("{operation}", auditWcfEvent.OperationName);
            var auditEventWcf = new AuditEventWcfAction()
            {
                WcfEvent = auditWcfEvent
            };
            // Create the audit scope
            using (var auditScope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = eventType,
                CreationPolicy = _creationPolicy,
                AuditEvent = auditEventWcf,
                DataProvider = GetAuditDataProvider(instance),
                CallingMethod = _operationDescription.SyncMethod ?? _operationDescription.TaskMethod
            }))
            {
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
                    (auditScope.Event as AuditEventWcfAction).WcfEvent = auditWcfEvent;
                    throw;
                }
                AuditBehavior.CurrentAuditScope = null;
                auditWcfEvent.OutputParameters = GetEventElements(outputs);
                auditWcfEvent.Result = new AuditWcfEventElement(result);
                (auditScope.Event as AuditEventWcfAction).WcfEvent = auditWcfEvent;
            }
            return result;
        }

        /// <summary>
        /// Asynchronous implementation of the Invoke (Begin)
        /// </summary>
        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            IAsyncResult result = null;
            // Create the Wcf audit event
            var auditWcfEvent = CreateWcfAuditEvent(instance, inputs);
            // Create the audit scope
            var eventType = _eventType.Replace("{contract}", auditWcfEvent.ContractName)
                .Replace("{operation}", auditWcfEvent.OperationName);
            var auditEventWcf = new AuditEventWcfAction()
            {
                WcfEvent = auditWcfEvent
            };
            // Create the audit scope
            var auditScope = AuditScope.Create(new AuditScopeOptions()
            {
                EventType = eventType,
                CreationPolicy = _creationPolicy,
                AuditEvent = auditEventWcf,
                DataProvider = GetAuditDataProvider(instance),
                CallingMethod = _operationDescription.SyncMethod ?? _operationDescription.TaskMethod
            });
            // Store a reference to this audit scope
            var callbackState = new AuditScopeState()
            {
                AuditScope = auditScope,
                OriginalUserCallback = callback,
                OriginalUserState = state
            };
            AuditBehavior.CurrentAuditScope = auditScope;
            try
            {
                result = _baseInvoker.InvokeBegin(instance, inputs, this.InvokerCallback, callbackState);
            }
            catch (Exception ex)
            {
                AuditBehavior.CurrentAuditScope = null;
                auditWcfEvent.Fault = GetWcfFaultData(ex);
                auditWcfEvent.Success = false;
                (auditScope.Event as AuditEventWcfAction).WcfEvent = auditWcfEvent;
                auditScope.Dispose();
                throw;
            }
            AuditBehavior.CurrentAuditScope = null;
            return new AuditScopeAsyncResult(result, callbackState);
        }

        /// <summary>
        /// Asynchronous implementation of the Invoke (End)
        /// </summary>
        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            var auditAsyncResult = result as AuditScopeAsyncResult;
            var auditScopeState = auditAsyncResult.AuditScopeState;
            object callResult;
            var auditScope = auditScopeState.AuditScope;
            var auditWcfEvent = (auditScope.Event as AuditEventWcfAction).WcfEvent;
            try
            {
                callResult = _baseInvoker.InvokeEnd(instance, out outputs, auditAsyncResult.OriginalAsyncResult);
            }
            catch (Exception ex)
            {
                AuditBehavior.CurrentAuditScope = null;
                auditWcfEvent.Fault = GetWcfFaultData(ex);
                auditWcfEvent.Success = false;
                (auditScope.Event as AuditEventWcfAction).WcfEvent = auditWcfEvent;
                auditScope.Dispose();
                throw;
            }
            AuditBehavior.CurrentAuditScope = null;
            auditWcfEvent.OutputParameters = GetEventElements(outputs);
            auditWcfEvent.Result = new AuditWcfEventElement(callResult);
            (auditScope.Event as AuditEventWcfAction).WcfEvent = auditWcfEvent;
            auditScope.Dispose();
            return callResult;
        }
        #endregion

        #region Private Methods
        private WcfEvent CreateWcfAuditEvent(object instance, object[] inputs)
        {
            var securityContext = ServiceSecurityContext.Current;
            var operationContext = OperationContext.Current;
            var imProps = OperationContext.Current.IncomingMessageProperties;
            var endpoint = imProps.ContainsKey(RemoteEndpointMessageProperty.Name) ? imProps[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty : null;
            return new WcfEvent()
            {
                Action = _operation.Action,
                ReplyAction = _operation.ReplyAction,
                ContractName = _operationDescription.DeclaringContract.Name,
                IsAsync = _operationDescription.SyncMethod == null,
                MethodSignature = (_operationDescription.SyncMethod ?? _operationDescription.TaskMethod).ToString(),
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

        private void InvokerCallback(IAsyncResult asyncResult)
        {
            var state = asyncResult.AsyncState as AuditScopeState;
            state.OriginalUserCallback.Invoke(new AuditScopeAsyncResult(asyncResult, state));
        }
        #endregion
    }
}