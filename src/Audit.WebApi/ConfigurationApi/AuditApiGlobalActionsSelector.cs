﻿#if ASP_CORE
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

        public IAuditApiGlobalConfigurator LogActionIf(Func<ControllerActionDescriptor, bool> controllerActionPredicate)
        {
            _config._logDisabledBuilder = ctx =>
            {
                if (ctx.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                {
                    return !controllerActionPredicate.Invoke(actionDescriptor);
                }
                return true;
            };
            return _config;
        }

#if ASP_CORE
        public IAuditApiGlobalConfigurator LogRequestIf(Func<Microsoft.AspNetCore.Http.HttpRequest, bool> requestPredicate)
        {
            _config._logDisabledBuilder = ctx =>
            {
                if (ctx.HttpContext?.Request != null)
                {
                    return !requestPredicate.Invoke(ctx.HttpContext.Request);
                }
                return true;
            };
            return _config;
        }
#else
        public IAuditApiGlobalConfigurator LogRequestIf(Func<System.Net.Http.HttpRequestMessage, bool> requestPredicate)
        {
            _config._logDisabledBuilder = ctx =>
            {
                if (ctx.Request != null)
                {
                    return !requestPredicate.Invoke(ctx.Request);
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
