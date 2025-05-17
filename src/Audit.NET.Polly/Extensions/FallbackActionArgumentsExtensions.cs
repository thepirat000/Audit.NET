using System.Threading.Tasks;
using Audit.Core;
using Polly;
using Polly.Fallback;

// ReSharper disable CheckNamespace
// ReSharper disable MethodHasAsyncOverload
#pragma warning disable S6966

namespace Audit.Polly
{
    public static class FallbackActionArgumentsExtensions
    {
        /// <summary>
        /// Fallback to the specified data provider.
        /// </summary>
        /// <param name="args">The Polly's fallback action arguments</param>
        /// <param name="fallbackDataProvider">The Audit Data Provider instance to use for fallback</param>
        public static async ValueTask<Outcome<object>> FallbackToDataProvider(this FallbackActionArguments<object> args, IAuditDataProvider fallbackDataProvider)
        {
            var auditEvent = args.Context.GetAuditEvent();

            if (auditEvent == null)
            {
                return await Outcome.FromResultAsValueTask(new object());
            }

            switch (args.Context.OperationKey)
            {
                case nameof(fallbackDataProvider.InsertEvent):
                    {
                        var result = fallbackDataProvider.InsertEvent(auditEvent);

                        return await Outcome.FromResultAsValueTask(result);
                    }
                case nameof(fallbackDataProvider.InsertEventAsync):
                    {
                        var result = await fallbackDataProvider.InsertEventAsync(auditEvent, args.Context.CancellationToken);

                        return await Outcome.FromResultAsValueTask(result);
                    }
                case nameof(fallbackDataProvider.ReplaceEvent):
                    {
                        var eventId = args.Context.Properties.GetValue(new ResiliencePropertyKey<object?>("EventId"), null);

                        fallbackDataProvider.ReplaceEvent(eventId, auditEvent);

                        return await Outcome.FromResultAsValueTask(new object());
                    }
                case nameof(fallbackDataProvider.ReplaceEventAsync):
                    {
                        var eventId = args.Context.Properties.GetValue(new ResiliencePropertyKey<object?>("EventId"), null);

                        await fallbackDataProvider.ReplaceEventAsync(eventId, auditEvent, args.Context.CancellationToken);

                        return await Outcome.FromResultAsValueTask(new object());
                    }
                default:
                    return await Outcome.FromResultAsValueTask(new object());
            }
        }
    }
}
