using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemB
{
    /// <summary>
    /// Реализация ISystemResults.
    /// </summary>
    public class SystemResultsImpl : ISystemResults
    {
        public string Result { get; set; }
        /// <summary>
        /// Ичистка строки если более cnt сообщений.
        /// </summary>
        public void Clear(int cnt)
        {
            if (Result != null && Result.Length > cnt) Result = ""; //Очистка строки, если очень большая.
        }
    }
}
