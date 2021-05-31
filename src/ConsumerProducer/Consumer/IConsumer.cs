using System.Threading.Tasks;

namespace ConsumerProducer.Consumer
{
    public interface IConsumer<T>
    {
        Task Consume(WorkItem<T> item);
    }
}
