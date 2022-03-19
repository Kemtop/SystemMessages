using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SystemA.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        /// <summary>
        /// Генератор сообщений.
        /// </summary>
        IRandomProducer producer;
        public SAController(IRandomProducer _producer)
        {
            producer = _producer;
        }



        // GET: api/<SAController>
        [HttpGet]
        public async Task<ActionResult> Get()
        {
            //Генерирую число отправляемых сообщений.
            Random rnd = new Random();
            int msgCount = rnd.Next(1, 10);

            //Генерирую сообщения.
            await producer.GenerateAsync(msgCount);

            return Ok(String.Format("Sending {0} messages.",msgCount));
        }
        //public IEnumerable<string> Get()
        //{
        //    //Генерирую новое сообщение.
        //    Random rnd = new Random();
        //    int val = rnd.Next(1, 10);
        //    producer.Generate(val);
        //    return new string[] { "value1", "value2" };
        //}

      




        // GET api/<SAController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<SAController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SAController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SAController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
