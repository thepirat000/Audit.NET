using System;
using Polly;

namespace Audit.Polly.Configuration
{
    public interface IPollyResilienceConfigurator
    {
        /// <summary>
        /// Configure the resilience policy
        /// </summary>
        /// <param name="resilienceBuilder">The resilience builder action</param>
        void WithResilience(Action<ResiliencePipelineBuilder<object>> resilienceBuilder);
    }
}