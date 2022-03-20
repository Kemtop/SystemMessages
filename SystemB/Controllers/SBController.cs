using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SystemB.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SBController : ControllerBase
    {
        /// <summary>
        /// Контейнер, эмулирующий результат работы долгого сервиса. Строка содержит входящие сообщения.
        /// </summary>
        private readonly ISystemResults results;

        public SBController(ISystemResults results_)
        {
            results = results_;
        }


      /// <summary>
      /// Посмотреть результат работы медленной системы.
      /// </summary>
      /// <returns></returns>
        [HttpGet]
        public ActionResult Get()
        {
            return Ok(results.Result);
        }

        // GET api/<SBController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<SBController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SBController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SBController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
