using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMqCl
{
    /// <summary>
    /// Интерфейс клиента для работы с RabbitMqClient.
    /// </summary>
    public interface IRabbitMqClient
    {
        /// <summary>
        /// Публикация сообщения.
        /// </summary>
        /// <param name="obj"></param>
        void Publish(Object obj);

        /// <summary>
        /// Создаюет логику подписки на сообщения от брокера.
        /// Данные очереди-в конфиге.
        /// </summary>
        void Subscribe();

        /// <summary>
        /// Событие по приходу нового сообщения(для клиента).
        /// </summary>
        event dgTransiveMessage EventReciveMessage;
    }
}
