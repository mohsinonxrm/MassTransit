﻿namespace MassTransit.Saga.Pipeline.Filters
{
    using System.Threading.Tasks;
    using Context;
    using GreenPipes;
    using Logging;
    using Metadata;


    /// <summary>
    /// Dispatches the ConsumeContext to the consumer method for the specified message type
    /// </summary>
    /// <typeparam name="TSaga">The consumer type</typeparam>
    /// <typeparam name="TMessage">The message type</typeparam>
    public class ObservesSagaMessageFilter<TSaga, TMessage> :
        ISagaMessageFilter<TSaga, TMessage>
        where TSaga : class, ISaga, Observes<TMessage, TSaga>
        where TMessage : class
    {
        void IProbeSite.Probe(ProbeContext context)
        {
            var scope = context.CreateFilterScope("observes");
            scope.Add("method", $"Consume({TypeMetadataCache<TMessage>.ShortName} message)");
        }

        public async Task Send(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
        {
            var activity = LogContext.IfEnabled(OperationName.Saga.Observe)?.StartSagaActivity<TSaga, TMessage>(context.Saga.CorrelationId);
            try
            {
                await context.Saga.Consume(context).ConfigureAwait(false);

                await next.Send(context).ConfigureAwait(false);
            }
            finally
            {
                activity?.Stop();
            }
        }
    }
}
