using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemA
{
    /// <summary>
    /// Интерфейс генерации случайных сообщений.
    /// </summary>
    public interface IRandomProducer
    {
        /// <summary>
        /// Асинхронная генерация сообщений.
        /// </summary>
        /// <param name="numberOfMsg"></param>
        Task GenerateAsync(int numberOfMsg);

    }
}
