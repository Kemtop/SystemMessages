using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMqCl;
using RabbitMqCl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemB.Models;

namespace SystemB
{
    /// <summary>
    /// Фоновая задача, читающая сообщения из брокера сообщений.
    /// И симулирующая длительный процесс.
    /// </summary>
    public class ReadMessagesService : IHostedService
    {
        private readonly ILogger<ReadMessagesService> logger;
        /// <summary>
        /// Контейнер, эмулирующий результат работы долгого сервиса. Строка содержит входящие сообщения.
        /// </summary>
        private readonly ISystemResults results;

        /// <summary>
        /// Интерфейс для работы с клиентом.
        /// </summary>
        private readonly IRabbitMqClient rabbitMqClient;

        /// <summary>
        /// Таймер обработки сообщений. Эмуляция медленной системы.
        /// </summary>
        private Timer timerProcessing;

        /// <summary>
        /// Входящие сообщения.
        /// </summary>
        List<MessageModel> Messages;


        public ReadMessagesService(ILogger<ReadMessagesService> _logger, 
            RabbitMqClient rabbitMqClient_,
            ISystemResults results_)
        {
            logger = _logger;
            rabbitMqClient = rabbitMqClient_;
            results = results_;
            Messages = new List<MessageModel>();
        }

        /// <summary>
        /// Запуск сервиса.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Настройка таймера эмулирующего долгий процесс.
            timerProcessing = new Timer((Object stateInfo) =>
            {
                ProcessingWorker(); //Какой то процесс.
            }, null, 1000, 1000);

            //Инициализация клиента для работы с брокером, подпись на событие нового сообщения.
            rabbitMqClient.Subscribe();
            rabbitMqClient.EventReciveMessage += OnHasNewMassege;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timerProcessing.Dispose();
            return Task.CompletedTask;
        }


        /// <summary>
        /// Запускается по срабатыванию таймера.
        /// </summary>
        private void ProcessingWorker()
        {
            MessageModel m = null;
            //Тут вопрос-а не потеряем ли мы чего при большой нагрузке.
            //Если Да-тогда нужно подумать через счетчик или буфер кольцевой.
            lock (Messages)
            {
                //Выбираю не обработанное сообщение.
                m = Messages.Where(x => x.ItProcessed == false).OrderBy(x => x.Prior).FirstOrDefault();
            }

            if (m == null) return; //Нет результатов.

            //Создаю результат работы медленного процесса.            
            string line = m.Message + " prior=" + m.Prior + "\r\n";

            lock(results) //Используется в контроллере.
            {
                results.Clear(2000); //Очистка строки если более 2000 символов.
                results.Result += line; //Добавляю строку в контейнер.
            }
       
            m.ItProcessed = true; //Сообщению ставлю флаг-обработано.
            logger.LogInformation(line);

            //Чистка списка-если очень большой.
            lock (Messages)
            {
                //Удалить обработанные сообщения-если их больше 500.
                if(Messages.Where(x=>x.ItProcessed==true).Count()>500)
                {
                    Messages.RemoveAll(x => x.ItProcessed == true);
                }
            }

        }

        /// <summary>
        /// Событие по приходу нового сообщения.
        /// </summary>
        /// <param name="data"></param>
        private void OnHasNewMassege(byte[] data)
        {
            string message = Encoding.UTF8.GetString(data);
            DtoSendValue dto = JsonConvert.DeserializeObject<DtoSendValue>(message);

            //Добавление сообщения в список.
            MessageModel m = new MessageModel();
            m.Message = dto.Message;
            m.Prior = dto.Prior;
            lock (Messages)
            {
                Messages.Add(m);
            }
        }


    }
}
