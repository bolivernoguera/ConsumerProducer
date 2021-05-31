namespace ConsumerProducer.Producer
{
    public interface IProducer<T>
    {
        bool TryEnqueue(WorkItem<T> payload);

        public bool TryEnqueue(T payload)
        {
            return TryEnqueue(new WorkItem<T>(payload, false));
        }
    }

}
