using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemB
{
    /// <summary>
    /// Просто контейнер для возвращения результатов.
    /// </summary>
    public interface ISystemResults
    {
        string Result { get; set; }

        /// <summary>
        /// Ичистка строки, если  строка длиной более cnt символов.
        /// </summary>
        void Clear(int cnt);
    }
}
