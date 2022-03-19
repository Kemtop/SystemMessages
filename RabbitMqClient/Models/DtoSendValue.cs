using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqCl.Models
{
    /// <summary>
    /// Модель для отправки сообщения из системы А.
    /// </summary>
    public class DtoSendValue
    {
        /// <summary>
        /// Сообщение.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Приоритет. Используется только для проверки не потеряли ли чего.
        /// </summary>
        public int Prior { get; set; }

    }
}
