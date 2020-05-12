#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
using Microsoft.AspNetCore.Mvc.Controllers;
#else
using ControllerActionDescriptor = System.Web.Http.Controllers.HttpActionDescriptor;
#endif
using System;

namespace Audit.WebApi.ConfigurationApi
{
    public class AuditApiGlobalActionsSelector : IAuditApiGlobalActionsSelector
    {
        internal AuditApiGlobalConfigurator _config = new AuditApiGlobalConfigurator();

        public IAuditApiGlobalConfigurator LogActionIf(Func<ControllerActionDescriptor, bool> controllerActionSelector)
        {
            _config._logDisabledBuilder = ctx =>
            {
                var actionDescriptior = ctx.ActionDescriptor as ControllerActionDescriptor;
                if (actionDescriptior != null)
                {
                    return !controllerActionSelector.Invoke(actionDescriptior);
                }
                return true;
            };
            return _config;
        }

#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETSTANDARD1_6 || NET451
        public IAuditApiGlobalConfigurator LogRequestIf(Func<Microsoft.AspNetCore.Http.HttpRequest, bool> requestSelector)
        {
            _config._logDisabledBuilder = ctx =>
            {
                if (ctx.HttpContext?.Request != null)
                {
                    return !requestSelector.Invoke(ctx.HttpContext.Request);
                }
                return true;
            };
            return _config;
        }
#else
        public IAuditApiGlobalConfigurator LogRequestIf(Func<System.Net.Http.HttpRequestMessage, bool> requestSelector)
        {
            _config._logDisabledBuilder = ctx =>
            {
                if (ctx.Request != null)
                {
                    return !requestSelector.Invoke(ctx.Request);
                }
                return true;
            };
            return _config;
        }
#endif
        
        public IAuditApiGlobalConfigurator LogAllActions()
        {
            _config._logDisabledBuilder = null;
            return _config;
        }

    }

}
