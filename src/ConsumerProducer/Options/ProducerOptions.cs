using System;
using System.Collections.Generic;

namespace ConsumerProducer.Options
{
    public abstract class ProducerOptions
    {
        public bool Enabled { get; set; }
        public int MaxQueueSize { get; set; }
        public int Parallelism { get; set; }
        public TimeSpan? FlushTimeout { get; set; }
        public IEnumerable<TimeSpan> IncrementalWait { get; set; }
    }
}
