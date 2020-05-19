#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NETCOREAPP3_1
using Microsoft.AspNetCore.Mvc.Controllers;
#else
using ControllerActionDescriptor = System.Web.Http.Controllers.HttpActionDescriptor;
#endif
using System;

namespace Audit.WebApi.ConfigurationApi
{
    public interface IAuditApiGlobalActionsSelector
    {
        /// <summary>
        /// Specifies a predicate to select the actions to be audited.
        /// </summary>
        /// <param name="controllerActionPredicate">A function of the current ControllerActionDescriptor that returns true for the actions to be audited, or false otherwise</param>
        /// <returns></returns>
        IAuditApiGlobalConfigurator LogActionIf(Func<ControllerActionDescriptor, bool> controllerActionPredicate);
        /// <summary>
        /// Specifies a predicate to select the actions to be audited.
        /// </summary>
        /// <param name="requestPredicate">A function of the current HTTP request that returns true for the request to be audited, or false otherwise</param>
        /// <returns></returns>
#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451 || NETCOREAPP3_1
        IAuditApiGlobalConfigurator LogRequestIf(Func<Microsoft.AspNetCore.Http.HttpRequest, bool> requestPredicate);
#else
        IAuditApiGlobalConfigurator LogRequestIf(Func<System.Net.Http.HttpRequestMessage, bool> requestPredicate);
#endif
        /// <summary>
        /// Specifies all the controller actions to be audited.
        /// </summary>
        IAuditApiGlobalConfigurator LogAllActions();
    }
}
