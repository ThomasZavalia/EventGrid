using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Repositories
{
    public class SeatRepository : ISeatRepository
    {
        private readonly ApplicationDbContext _context;
        public SeatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Seat?> GetByIdAsync(Guid seatId, bool trackChanges, CancellationToken cancellationToken)
        {
            var query = _context.Seats.AsQueryable();
            
            if (!trackChanges)
                query = query.AsNoTracking();
                
            return await query.FirstOrDefaultAsync(s => s.Id == seatId, cancellationToken);
        }

        public async Task<bool> UpdateAsync(Seat seat, CancellationToken cancellationToken)
        {
            try
            {
                _context.Seats.Update(seat);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
               
                return false; 
            }
        }

        public void Detach(Seat seat)
        {
           _context.Entry(seat).State = EntityState.Detached;
        }
    }
}
