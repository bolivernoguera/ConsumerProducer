using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsumerProducer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsumerProducer.Consumer.Implementations.Buffer
{

    public abstract class BufferConsumer<TObj, TOUT, TConsumerOptions, TBuffer> : IConsumer<TObj>
        where TObj : class
        where TConsumerOptions : ConsumerOptions
        where TBuffer : IBuffer<TObj, TOUT>
    {
        private readonly IBuffer<TObj, TOUT> _buffer;
        protected readonly ILogger _logger;
        protected readonly IOptionsMonitor<TConsumerOptions> _options;
        public BufferConsumer(
            TBuffer buffer,
            ILogger logger,
            IOptionsMonitor<TConsumerOptions> options)
        {
            _buffer = buffer;
            _logger = logger;
            _options = options;
        }

        public async Task Consume(WorkItem<TObj> item)
        {
            if (!_options.CurrentValue.Enabled) return;

            if (item.Flush && !_buffer.Any()) return;

            if (!item.Flush)
            {
                _buffer.TryPush(item.Payload);
                if (!_buffer.IsFull) return;
            }

            try
            {
                var success = await PublishAsync(_buffer.Pop());

                if (!success)
                {
                    _logger.LogError($"Failed to send messages to {nameof(BufferConsumer<TObj, TOUT, TConsumerOptions, TBuffer>)}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to send messages to {nameof(BufferConsumer<TObj, TOUT, TConsumerOptions, TBuffer>)}");
            }
            finally
            {
                _buffer.Clear();
            }
        }

        protected abstract Task<bool> PublishAsync(IEnumerable<TOUT> objs);
    }
}