#if NETCOREAPP1_0
using Audit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Audit.Mvc.UnitTest
{
    public class ActionFilterUnitTest
    {
        [Fact]
        public void Test_AuditActionFilter_Core()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequest>();
            request.SetupGet(r => r.Scheme).Returns("http");
            request.SetupGet(r => r.Host).Returns(new HostString("200.10.10.20:1010"));
            request.SetupGet(r => r.Path).Returns("/home/index");
            var httpResponse = new Mock<HttpResponse>();
            httpResponse.SetupGet(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var actionContext = new ActionContext()
            {
                HttpContext = httpContext.Object,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
                {
                    ActionName = "index",
                    ControllerName = "home"
                }
            };
            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            var filters = new List<IFilterMetadata>();
            var controller = new Mock<Controller>();
            var dataProvider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.DataProvider = dataProvider.Object;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };

            var actionExecutingContext = new ActionExecutingContext(actionContext, filters, args, controller.Object);
            filter.OnActionExecuting(actionExecutingContext);

            var actionExecutedContext = new ActionExecutedContext(actionContext, filters, controller.Object);
            actionExecutedContext.Result = new ObjectResult("this is the result");
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal("http://200.10.10.20:1010/home/index", action.RequestUrl);
            Assert.Equal("home", action.ControllerName);
            Assert.Equal("value1", action.ActionParameters["test1"]);
            Assert.Equal(200, action.ResponseStatusCode);
        }
    }
}
#endif