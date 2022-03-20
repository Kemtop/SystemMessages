using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMqCl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SystemA
{
    /// <summary>
    /// Фоновая задача для создание очереди в RabbitMq.
    /// Что бы не приходилось ждать Get запроса от пользователя,
    /// в результате чего Подписчик не сможет работать. 
    /// </summary>
    public class InitQueueService : IHostedService
    {
        private readonly IRabbitMqClient rabbitMqClient;
        private readonly ILogger<InitQueueService> logger;
        public InitQueueService(IRabbitMqClient _rabbitMqClient, ILogger<InitQueueService> _logger)
        {
            rabbitMqClient = _rabbitMqClient;
            logger = _logger;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Заставляю обратиться к объекту и создать его реализацию.
            //А не ждать действия пользователя-и получить ошибки в других микросервисах.
            int r=rabbitMqClient.Dummy(); //Просто обратиться к методу, что DI создало реализацию.
            logger.LogInformation("Init rabbit.{0}",r);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
