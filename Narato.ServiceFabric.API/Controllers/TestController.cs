using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Narato.ServiceFabric.Contracts.Contracts;
using Narato.ServiceFabric.Contracts.Models;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.API.Controllers
{
    [Route("[controller]")]
    public class TestController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]TestModel model)
        {
            try
            {
                var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
                var created = await testService.CreateAsync(model);
                return Ok(created);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody]TestModel model)
        {
            var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
            var created = await testService.UpdateAsync(model);
            return Ok(created);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string key)
        {
            var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
            await testService.DeleteAsync(key);
            return Ok();
        }
    }
}