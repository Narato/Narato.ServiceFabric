using System;
using System.Threading.Tasks;
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
            var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
            var created = await testService.CreateAsync(model);
            return Ok(created);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody]TestModel model)
        {
            var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
            var created = await testService.UpdateAsync(model);
            return Ok(created);
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            var testService = new ServiceResolver().Resolve<ITestService>(new TestServiceDefinition());
            await testService.DeleteAsync(key);
            return Ok();
        }
    }
}