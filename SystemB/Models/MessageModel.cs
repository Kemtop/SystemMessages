using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemB.Models
{
    public class MessageModel
    {
        /// <summary>
        /// Сообщение.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Приоритет. Используется только для проверки не потеряли ли чего.
        /// </summary>
        public int Prior { get; set; }

        /// <summary>
        /// Объект обработан.
        /// </summary>
        public bool ItProcessed { get; set; }
    }
}
