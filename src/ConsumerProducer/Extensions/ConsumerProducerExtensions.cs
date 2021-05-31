using ConsumerProducer.Consumer;
using ConsumerProducer.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Hub.Application.BackgroundServices.QueuedBackgroundService
{
    public static class ConsumerProducerExtensions
    {
        public static IServiceCollection AddBackgroundServiceProducerConsumer<TObj, TProducer, TConsumer>(
            this IServiceCollection services)
            where TObj : class
            where TProducer : class, IProducer<TObj>, IHostedService
            where TConsumer : class, IConsumer<TObj>
        {
            services.TryAddSingleton<TConsumer>();
            services.TryAddSingleton<TProducer>();

            return services
                .AddSingleton<IProducer<TObj>>(sp => sp.GetRequiredService<TProducer>())
                .AddSingleton<IConsumer<TObj>>(sp => sp.GetRequiredService<TConsumer>())
                .AddHostedService(sp => sp.GetService<TProducer>());
        }
    }
}
