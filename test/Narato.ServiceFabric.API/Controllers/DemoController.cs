using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Narato.ServiceFabric.Contracts.Contracts;
using Narato.ServiceFabric.Contracts.Models;
using Narato.ServiceFabric.Services;

namespace Narato.ServiceFabric.API.Controllers
{
    [Route("[controller]")]
    public class DemoController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody]Ship model)
        {
            try
            {
                var testService = new ServiceResolver().Resolve<IShipService>(new ShipServiceDefinition());
                var created = await testService.CreateAsync(model);
                return Ok(created);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody]Ship model)
        {
            try
            {
                var testService = new ServiceResolver().Resolve<IShipService>(new ShipServiceDefinition());
                var created = await testService.UpdateAsync(model);
                return Ok(created);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
           
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key)
        {
            try
            {
                var testService = new ServiceResolver().Resolve<IShipService>(new ShipServiceDefinition());
                await testService.DeleteAsync(key);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}