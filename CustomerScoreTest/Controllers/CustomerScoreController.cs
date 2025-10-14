using CustomerScoreTest.Models;
using CustomerScoreTest.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerScoreTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CustomerScoreController : ControllerBase
    {
        readonly ICustomerScoreService _customerScoreService;

        public CustomerScoreController(ICustomerScoreService customerScoreService)
        {
            _customerScoreService = customerScoreService;
        }

        [ProducesResponseType(typeof(UpdateCustomerResponse), 200)]
        [HttpPost("~/customer/{customerid}/score/{score}")]
        public async Task<IActionResult> UpdateCustomer(long customerid, int score)
        {
            if (score < -1000 || score > 1000) return BadRequest();
            var restult =await _customerScoreService.UpdateCustomer(customerid, score);
            return Ok(restult);
        }

        [ProducesResponseType(typeof(List<Customer>), 200)]
        [HttpGet("~/leaderboard")]
        public async Task<IActionResult> GetGustomersByRank([FromQuery]int start, [FromQuery]int end)
        {
            var restult = await _customerScoreService.GetGustomersByRank(start, end);
            return Ok(restult);
        }

        [ProducesResponseType(typeof(List<Customer>), 200)]
        [HttpGet("~/leaderboard/{customerid}")]
        public async Task<IActionResult> GetGustomersByCustomerId(long customerid, [FromQuery] int high, [FromQuery] int low)
        {
            var restult = await _customerScoreService.GetGustomersByCustomerId(customerid, high, low);
            return Ok(restult);
        }
    }
}
