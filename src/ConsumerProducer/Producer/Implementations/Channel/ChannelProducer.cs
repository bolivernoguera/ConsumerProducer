using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ConsumerProducer.Consumer;
using ConsumerProducer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsumerProducer.Producer.Implementations.Channel
{
    public class ChannelProducer<TItem, TConsumer, TProducerOptions> :
        IProducer<TItem>,
        IHostedService,
        IDisposable
        where TItem : class
        where TConsumer : IConsumer<TItem>
        where TProducerOptions : ProducerOptions
    {
        private readonly IOptionsMonitor<TProducerOptions> _producerOptions;
        private readonly TConsumer _consumer;
        private readonly ILogger<ChannelProducer<TItem, TConsumer, TProducerOptions>> _logger;
        private readonly List<Thread> _backgroundThreads = new();
        private readonly Channel<WorkItem<TItem>> _queue;

        public ChannelProducer(
            IOptionsMonitor<TProducerOptions> producerOptions,
            TConsumer consumer,
            ILogger<ChannelProducer<TItem, TConsumer, TProducerOptions>> logger)
        {
            _producerOptions = producerOptions;
            _consumer = consumer;
            _logger = logger;
            _queue = System.Threading.Channels.Channel.CreateBounded<WorkItem<TItem>>(_producerOptions.CurrentValue.MaxQueueSize);

            for (var i = 0; i < _producerOptions.CurrentValue.Parallelism; i++)
            {
                _backgroundThreads.Add(new Thread(async () => await Consume())
                {
                    Name = $"Background thread for {typeof(ChannelProducer<TItem, TConsumer, TProducerOptions>).Name}",
                    Priority = ThreadPriority.BelowNormal,
                    IsBackground = true
                });
            }
        }

        public bool TryEnqueue(WorkItem<TItem> payload)
        {
            try
            {
                if (!_producerOptions.CurrentValue.Enabled) return false;

                if (_queue.Writer.TryWrite(payload)) return true;
                _logger.LogWarning(
                    $"Could not enqueue item {payload} either because of overflow or termination, discarding.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not enqueue item {payload} because of exception.");
                return false;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var backgroundThread in _backgroundThreads)
            {
                backgroundThread.Start();
            }

            return Task.CompletedTask;
        }

        private async Task Consume()
        {

            int timeSpanCounter = 0;

            var flushStopwatch = new Stopwatch();

            if (_producerOptions.CurrentValue.FlushTimeout.HasValue)
            {
                flushStopwatch.Start();
            }

            while (true)
            {
                while (_queue.Reader.TryRead(out var item))
                {
                    await ConsumeInternal(item);

                    timeSpanCounter = 0;
                }

                if (_producerOptions.CurrentValue.FlushTimeout.HasValue &&
                    flushStopwatch.ElapsedMilliseconds >= _producerOptions.CurrentValue.FlushTimeout.Value.TotalMilliseconds)
                {
                    await ConsumeInternal(new WorkItem<TItem>(payload: default, flush: true));

                    flushStopwatch.Restart();
                }

                if (_producerOptions.CurrentValue.IncrementalWait != null)
                {
                    await Task.Delay(_producerOptions.CurrentValue.IncrementalWait.ElementAt(timeSpanCounter));

                    if (timeSpanCounter < _producerOptions.CurrentValue.IncrementalWait.Count() - 1)
                    {
                        timeSpanCounter++;
                    }
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }

        private async Task ConsumeInternal(WorkItem<TItem> item)
        {
            try
            {
                await _consumer.Consume(item);
            }
            catch
            {
                // TODO: Should we log unhandled exceptions here? //TODO Action here
            }
        }



        public Task StopAsync(CancellationToken cancellationToken)
        {
            _queue.Writer.Complete();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

}
