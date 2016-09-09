#if NET451
using Audit.Core;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Xunit;

namespace Audit.Mvc.UnitTest
{
    public class ActionFilterUnitTest
    {
        [Fact]
        public void Test_AuditActionFilter()
        {
            // Mock out the context to run the action filter.
            var request = new Mock<HttpRequestBase>();
            var nvc = new NameValueCollection();
            //var request = new HttpRequest(null, "http://200.10.10.20:1010/api/values", null);
            request.Setup(c => c.ContentType).Returns("application/json");
            request.Setup(c => c.Headers).Returns(() => nvc);

            var httpResponse = new Mock<HttpResponseBase>();

            httpResponse.Setup(c => c.StatusCode).Returns(200);
            var itemsDict = new Dictionary<object, object>();
            var httpContext = new Mock<HttpContextBase>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Items).Returns(() => itemsDict);
            httpContext.SetupGet(c => c.Response).Returns(() => httpResponse.Object);
            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContext.Object
            };
            controllerContext.HttpContext.Request.Headers.Add("test-header", "header-value");
            var actionDescriptor = new Mock<ActionDescriptor>();
            actionDescriptor.Setup(c => c.ActionName).Returns("get");

            var args = new Dictionary<string, object>()
            {
                {"test1", "value1" }
            };
            
            var dataProvider = new Mock<AuditDataProvider>();
            Audit.Core.Configuration.DataProvider = dataProvider.Object;

            var filter = new AuditAttribute()
            {
                IncludeHeaders = true,
                IncludeModel = true,
                EventTypeName = "TestEvent"
            };
            var actionExecutingContext = new ActionExecutingContext(controllerContext, actionDescriptor.Object, new Dictionary<string, object> { { "test1", "value1" } } );
            
            //.Properties.Add("MS_HttpContext", httpContext.Object);
            
            filter.OnActionExecuting(actionExecutingContext);

            var actionExecutedContext = new ActionExecutedContext(controllerContext, actionDescriptor.Object, false, null);
            filter.OnActionExecuted(actionExecutedContext);

            var action = itemsDict["__private_AuditAction__"] as AuditAction;
            var scope = itemsDict["__private_AuditScope__"] as AuditScope;

            //Assert
            dataProvider.Verify(p => p.InsertEvent(It.IsAny<AuditEvent>()), Times.Once);
            Assert.Equal("header-value", action.Headers["test-header"]);
            Assert.Equal("get", action.ActionName);
            Assert.Equal("value1", action.ActionParameters["test1"]);
        }
    }
}
#endif