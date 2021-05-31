namespace ConsumerProducer
{
    public class WorkItem<T>
    {
        public T Payload { get; }
        public bool Flush { get; }

        public WorkItem(T payload, bool flush)
        {
            Payload = payload;
            Flush = flush;
        }
    }
}
