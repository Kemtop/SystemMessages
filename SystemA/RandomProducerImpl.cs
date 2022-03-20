using Microsoft.Extensions.Logging;
using RabbitMqCl;
using RabbitMqCl.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemA.Models;

namespace SystemA
{
    /// <summary>
    /// Реализация IRandomProducer.
    /// Класс для генерации случайных сообщений и отправке системе B.
    /// </summary>
    class RandomProducerImpl : IRandomProducer,IDisposable
    {
        /// <summary>
        /// Сообщения системы А.
        /// </summary>
        private List<MessageModel> Messages;

        /// <summary>
        /// Счетчик приоритета сообщений.
        /// </summary>
        private int priorCounter;

        /// <summary>
        /// Количество новых,не отправленных сообщений.
        /// </summary>
        private int MsgGenCount;

        /// <summary>
        /// Токен завершения работы. Отстанавливает задачу отправки новых сообщений.
        /// Если вызывается Dispose.
        /// </summary>
        private bool endToken;

        private readonly ILogger<RandomProducerImpl> logger;
        private readonly IRabbitMqClient _rabbitMqClient;

        public RandomProducerImpl(ILogger<RandomProducerImpl> _logger, IRabbitMqClient rabbitMqClient)
        {
            logger = _logger;
            Messages = new List<MessageModel>();
            _rabbitMqClient = rabbitMqClient;

            //Запуск постоянного процесса отслеживающего появление новых сообщений.
            Task.Run(() => SendMessages()); //Асинхронная отправка сообщений. Если они есть.
        }


        /// <summary>
        ///Запустить процесс генерации и отправки сообщений.
        ///Асинхронная генерация сообщений.
        /// </summary>
        /// <param name="numberOfMsg">Сколько строк генерировать.</param>
        public Task GenerateAsync(int numberOfMsg)
        {
            return Task.Run(() => GenerateString(numberOfMsg)); //Генерация строк.
        }


        /// <summary>
        /// Генерирует заданное количество сообщений.
        /// Не больше чем указано, в течении секунды.
        /// </summary>
        /// <param name="numberOfMsg"></param>
        private void GenerateString(int numberOfMsg)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Random rnd = new Random();
            double elapsed = 0; //Счетчик времени.
            logger.LogInformation("Начата генерация строк.");
            

            for (int i = 0; i < numberOfMsg; i++)
            {
                int rVal = rnd.Next(1, 3000); //Получаю псевдо случайно число.
                string hash = GetHashStr(rVal); //Преобразовать в хэш код.

                //Меряем время.
                stopWatch.Stop();
                elapsed += stopWatch.Elapsed.Milliseconds;
                if (elapsed >= 1000) break; //Больше секунды-стоп.
                stopWatch.Start();

                //Проверка счетчика приоритета на предмет близкого переполнения.
                if (priorCounter > int.MaxValue - 3) priorCounter = 0; //Сброс.
                else priorCounter++;

                //Добавляем новое сообщение в список.
                MessageModel m = new MessageModel();
                m.Message = hash;
                m.Prior = priorCounter;

                lock (Messages)
                {
                    Messages.Add(m);
                }

                //Увеличиваем количество новых сообщений.
                Interlocked.Increment(ref MsgGenCount);
            }

            logger.LogInformation("Генерация строк окончена. Создано {0}.", numberOfMsg);
        }


        /// <summary>
        /// Преобразовывает число в хэш строку.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string GetHashStr(int val)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(BitConverter.GetBytes(val));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }


        /// <summary>
        /// Отправка сообщений брокеру в очередь.
        /// </summary>
        private void SendMessages()
        {
            //Количество операций простоя, для запуска фоновой чистки списка.
            int idleIteration=0; 

            //Запускаем мониторинг новых сообщений.   
            while (!endToken)
            {       
                //Есть новые сообщения.
                if(MsgGenCount>0)
                {
                    //Не отправленные сообщения.
                    List<MessageModel> NoSendMsg  = null;

                    lock (Messages)
                    {
                        //Получаю список новых сообщений.
                        NoSendMsg = Messages.Where(x => x.HasSend==false).
                            OrderBy(x=>x.Prior).ToList();
                    }

                    //Есть не отправленные сообщения.
                    if (NoSendMsg != null)
                    {
                        int sendCnt = 0; //Счетчик сообщений, только для вывода данных в консоль.
                        foreach(MessageModel m in NoSendMsg)
                        {
                            DtoSendValue dto = new DtoSendValue();
                            dto.Message = m.Message;
                            dto.Prior = m.Prior;
                            //5 кратная попытка отпавить сообщение-иначе исключение.
                            TrySend(dto,5);
                            m.HasSend = true;//Ставим статус-отправлен.
                            //Уменьшаем счетчик новых сообщений.
                            Interlocked.Decrement(ref MsgGenCount);
                            sendCnt++;
                        }
                        logger.LogInformation("Отправлено {0} сообщений.",sendCnt);
                    }

                    idleIteration = 0; //Сброс итераций простоя.
                }
               
                //Процесс долго ни чего не делает.
                if(idleIteration>10)
                {
                    ClearPool(); //Удаляет отправленные сообщения из списка.
                    idleIteration = 0;
                }

                Thread.Sleep(800);
                idleIteration++; //Количество циклов простоя.
            }
        }

        /// <summary>
        /// Удаляет отправленные сообщения из списка.
        /// Что бы не занимало память.
        /// </summary>
        private void ClearPool()
        {
            int delCnt = 0;//Количество удаленных записей.
                           //Чистим список.
            lock (Messages)
            {
                delCnt = Messages.Where(x => x.HasSend == true).Count();
                //Удалить все отправленные.
                if(delCnt>0)
                Messages.RemoveAll(x => x.HasSend == true);
            }

            if (delCnt > 0)
                logger.LogInformation("В буфере очищено {0} сообщений.", delCnt);
        }

        /// <summary>
        /// Пытается отправить сообщение. Если за указанное количество попыток сообщение не было
        /// отправлено-вызываем исключение.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ettempt">Количество попыток</param>
        private void TrySend(DtoSendValue dto,int ettempt)
        {
            string error = "";
            while(ettempt>0)
            {
                try
                {
                    _rabbitMqClient.Publish(dto);
                    return; //Успешная отправка.
                }
                catch(Exception ex)
                {
                    //Собираю все ошибки.
                    error += String.Format("[{0}]\r\n", ex.Message);
                    //Логирую.
                    logger.LogError("Exception in TrySend:{0}",ex.Message);
                    ettempt--; //Попыток все меньше.
                }

                Thread.Sleep(800); 
            }

            //Попытки кончились.
            throw new Exception(error);
        }


        public void Dispose()
        {
            endToken=true; 
            Thread.Sleep(200); 
        }
    }
}
