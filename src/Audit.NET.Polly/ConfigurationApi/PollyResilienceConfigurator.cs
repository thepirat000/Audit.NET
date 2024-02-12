using System;
using Audit.Core;
using Polly;

namespace Audit.Polly.Configuration
{
    public class PollyResilienceConfigurator : IPollyResilienceConfigurator
    {
        internal AuditDataProvider? _innerDataProvider;
        internal ResiliencePipeline<object>? _pipeline;

        public void WithResilience(Action<ResiliencePipelineBuilder<object>> resilienceBuilder)
        {
            var builder = new ResiliencePipelineBuilder<object>();
            resilienceBuilder.Invoke(builder);
            _pipeline = builder.Build();
        }
    }
}