using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IDbSeeder _dbSeeder;

        public AdminController(IDbSeeder dbSeeder)
        {
            _dbSeeder = dbSeeder;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedDatabase()
        {
            var result = await _dbSeeder.SeedAsync();

            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }

            return Ok(new
            {
                message = result.Message,
                eventId = result.EventId,
                seatsCreated = result.SeatsCreated
            });
        }
    }
}
