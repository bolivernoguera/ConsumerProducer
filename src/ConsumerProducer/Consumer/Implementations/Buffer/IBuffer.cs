using System.Collections.Generic;
using System.IO;

namespace ConsumerProducer.Consumer.Implementations.Buffer
{

    public interface IBuffer<TIN, TOUT>
    {
        public bool TryPush(TIN obj);

        public bool IsFull { get; }

        public IEnumerable<TOUT> Pop();

        public void Clear();
        bool Any();
    }

    public interface IStreamBuffer<T> : IBuffer<T, Stream>
    {

    }
}