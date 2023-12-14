#if ASP_NET
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Net.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace Audit.WebApi.UnitTest
{
    /// <summary>
    /// Class created to mimic the real internal CandidateHttpActionDescriptor
    /// </summary>
    public class CandidateHttpActionDescriptor_Test : HttpActionDescriptor
    {
        public HttpActionDescriptor Inner { get; set; }

        internal CandidateHttpActionDescriptor_Test(HttpActionDescriptor action)
        {
            Inner = action;
        }

        public override HttpActionBinding ActionBinding
        {
            get => Inner.ActionBinding;
            set => Inner.ActionBinding = value;
        }

        public override string ActionName => Inner.ActionName;

        public override Task<object> ExecuteAsync(HttpControllerContext controllerContext, IDictionary<string, object> arguments, CancellationToken cancellationToken) =>
            Inner.ExecuteAsync(controllerContext, arguments, cancellationToken);

        public override Collection<T> GetCustomAttributes<T>() => Inner.GetCustomAttributes<T>();

        public override Collection<T> GetCustomAttributes<T>(bool inherit) => Inner.GetCustomAttributes<T>(inherit);

        public override Collection<FilterInfo> GetFilterPipeline() => Inner.GetFilterPipeline();

        public override Collection<IFilter> GetFilters() => Inner.GetFilters();

        public override Collection<HttpParameterDescriptor> GetParameters() => Inner.GetParameters();

        public override ConcurrentDictionary<object, object> Properties => Inner.Properties;

        public override IActionResultConverter ResultConverter => Inner.ResultConverter;

        public override Type ReturnType => Inner.ReturnType;

        public override Collection<HttpMethod> SupportedHttpMethods => Inner.SupportedHttpMethods;
    }
}
#endif