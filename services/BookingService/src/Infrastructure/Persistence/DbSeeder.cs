using Application.Interfaces;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class DbSeeder : IDbSeeder
    {
        private readonly ApplicationDbContext _context;

        public DbSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsSuccess, string Message, Guid? EventId, int SeatsCreated)> SeedAsync()
        {
            if (_context.Events.Any())
            {
                return (false, "La base de datos ya tiene datos.", null, 0);
            }

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

            return (true, "Base de datos inicializada", myEvent.Id, seats.Count);
        }
    }
}
