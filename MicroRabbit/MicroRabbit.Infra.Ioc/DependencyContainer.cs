using MicroRabbit.Domain.Core.Bus;
using Microsoft.Extensions.DependencyInjection;
using RabbitMq.Infra.Bus;
using System;
using System.Collections.Generic;
using System.Text;

namespace MicroRabbit.Infra.Ioc
{
    public class DependencyContainer
    {
        public static void RegisterServices(IServiceCollection services)
        {
            // Domain Bus
            services.AddTransient<IEventBus, RabbitMqBus>();
        }
    }
}
