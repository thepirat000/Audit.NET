using System;
using System.Threading.Tasks;
using Audit.Core;
using Polly;
using Polly.Hedging;

namespace Audit.Polly
{
    public static class HedgingActionGeneratorArgumentsExtensions
    {
        /// <summary>
        /// Use the specified data provider as the fallback for the hedging action.
        /// </summary>
        /// <param name="args">The Polly's hedging action arguments</param>
        /// <param name="hedgingDataProvider">The Audit Data Provider instance to use for hedging</param>
        public static Func<ValueTask<Outcome<object>>> FallbackToDataProvider(this HedgingActionGeneratorArguments<object> args, AuditDataProvider hedgingDataProvider)
        {
            return () => FallbackToDataProviderInternal(args, hedgingDataProvider);
        }
        
        internal static async ValueTask<Outcome<object>> FallbackToDataProviderInternal(this HedgingActionGeneratorArguments<object> args, AuditDataProvider hedgingDataProvider)
        {
            var auditEvent = args.PrimaryContext.GetAuditEvent();

            if (auditEvent == null)
            {
                return await Outcome.FromResultAsValueTask(new object());
            }

            switch (args.PrimaryContext.OperationKey)
            {
                case nameof(hedgingDataProvider.InsertEvent):
                {
                    var result = hedgingDataProvider.InsertEvent(auditEvent);

                    return await Outcome.FromResultAsValueTask(result);
                }
                case nameof(hedgingDataProvider.InsertEventAsync):
                {
                    var result = await hedgingDataProvider.InsertEventAsync(auditEvent, args.PrimaryContext.CancellationToken);
                        
                    return await Outcome.FromResultAsValueTask(result);
                }
                case nameof(hedgingDataProvider.ReplaceEvent):
                {
                    var eventId = args.PrimaryContext.Properties.GetValue<object?>(new ResiliencePropertyKey<object?>("EventId"), null);

                    hedgingDataProvider.ReplaceEvent(eventId, auditEvent);

                    return await Outcome.FromResultAsValueTask(new object());
                }
                case nameof(hedgingDataProvider.ReplaceEventAsync):
                {
                    var eventId = args.PrimaryContext.Properties.GetValue<object?>(new ResiliencePropertyKey<object?>("EventId"), null);

                    await hedgingDataProvider.ReplaceEventAsync(eventId, auditEvent, args.PrimaryContext.CancellationToken);

                    return await Outcome.FromResultAsValueTask(new object());
                }
                default:
                    return await Outcome.FromResultAsValueTask(new object());
            }
        }
    }
}