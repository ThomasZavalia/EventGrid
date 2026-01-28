using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedDatabase()
        {
            
            if (_context.Events.Any())
            {
                return BadRequest("La base de datos ya tiene datos.");
            }

            // 2. Crear Evento
            var myEvent = new Event
            {
                Id = Guid.NewGuid(),
                Name = "Concierto de Rock 2026",
                Date = DateTime.UtcNow.AddMonths(1)
            };

            _context.Events.Add(myEvent);

            
            var seats = new List<Seat>();
            for (int i = 1; i <= 50; i++)
            {
               
                seats.Add(new Seat(
                    section: "General",
                    number: $"A-{i}",
                    price: 150.00m,
                    eventId: myEvent.Id
                ));
            }

            _context.Seats.AddRange(seats);

          
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Base de datos inicializada",
                eventId = myEvent.Id,
                seatsCreated = seats.Count
            });
        }
    }
}
