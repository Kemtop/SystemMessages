using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace RabbitMqCl
{
    /// <summary>
    /// Делегат для передачи подписчику сообщения.
    /// </summary>
    /// <param name="obj"></param>
    public delegate void dgTransiveMessage(byte[] message);

    /// <summary>
    /// Клиент для работы с RabbitMq.
    /// </summary>
    public class RabbitMqClient : IDisposable, IRabbitMqClient
    {
        /// <summary>
        /// Событие по приему сообщения из очереди.
        /// </summary>
        public event dgTransiveMessage EventReciveMessage;

        private readonly ILogger<RabbitMqClient> _logger;

        /// <summary>
        /// Настройки Rabbit из конфига.
        /// </summary>
        private string RabbitHost;
        private string QueueName; //Имя очереди.
        private string RoutingKey; //Ключ маршрутизации.
        private string ExchangeName; //Название точки обмена.
        private string AppId; //Идентификатор сервиса.
        private bool Mode;//Режим работы-1-producer,0-subscriber.

        /// <summary>
        /// Данные соединения.
        /// </summary>
        private IModel Channel;
        private IConnection connection; //Соединение.
        private ConnectionFactory connectionFactory;


        public RabbitMqClient(ILogger<RabbitMqClient> logger, IConfiguration config)
        {
            //Получаю сведения о url брокера.
            string url = config["RabitMqClient:URL"];
            RabbitHost = config["RabitMqClient:HostName"];
        
            ExchangeName= $"{RabbitHost}.{config["RabitMqClient:ExchangeName"]}"; //Точка обмена.
            QueueName = $"{RabbitHost}.{config["RabitMqClient:QueueName"]}";    //Имя очереди.
            RoutingKey = $"{RabbitHost}.{config["RabitMqClient:RoutingKey"]}";
            AppId = $"{RabbitHost}.{config["RabitMqClient:AppId"]}";

            //Режим работы.
            string mode = config["RabitMqClient:Mode"];
            if (mode == "Producer") Mode = true;


            var uri = new Uri(url);
            connectionFactory = new ConnectionFactory
            {
                Uri = uri
            };
            _logger = logger;
          
            ConnectToRabbitMq();
        }

        /// <summary>
        /// Подключение к серверу.
        /// </summary>
        private void ConnectToRabbitMq()
        {
             //Если соединение не открыто-открываем.
            if (connection == null || connection.IsOpen == false)
            {
                connection = connectionFactory.CreateConnection();
            }

            if (Channel == null || Channel.IsOpen == false)
            {
                Channel = connection.CreateModel();

                //Если режим работы-издатель.
                if(Mode)
                {
                    //Настройка точки обмена.
                    Channel.ExchangeDeclare(exchange: ExchangeName,
                        type: "direct", durable: true, autoDelete: false);
                    //Параметры очереди.
                    Channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
                    //Приязка очереди к точке обмена.
                    Channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
                }
             
            }

            //Добавить обработку исключения OperationInterruptedException.
        }

        /// <summary>
        /// Публикация сообщения.
        /// </summary>
        /// <param name="obj"></param>
        public void Publish(Object obj)
        {
            try
            {
                string jsonStr = JsonConvert.SerializeObject(obj);
                var body = Encoding.UTF8.GetBytes(jsonStr);
                var properties = Channel.CreateBasicProperties();
                properties.AppId = AppId;
                properties.ContentType = "application/json";
                properties.DeliveryMode = 2; 
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                Channel.BasicPublish(exchange: ExchangeName, 
                    routingKey: RoutingKey, body: body, basicProperties: properties);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error while publishing");
            }
        }


        /// <summary>
        /// Создаюет логику подписки на сообщения от брокера.
        /// Данные очереди-в конфиге.
        /// </summary>
        public void Subscribe()
        {
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (ch, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                EventReciveMessage(body);
                Channel.BasicAck(ea.DeliveryTag, false);
            };

            Channel.BasicConsume(QueueName,false,consumer);
        }


        /// <summary>
        /// Очистка ресурсов.
        /// </summary>
        public void Dispose()
        {
            try
            {
                Channel?.Close();
                Channel?.Dispose();
                Channel = null;

                connection?.Close();
                connection?.Dispose();
                connection = null;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot dispose RabbitMQ channel or connection");
            }
        }
    }
}
