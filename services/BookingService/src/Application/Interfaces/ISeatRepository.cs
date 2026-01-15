using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ISeatRepository
    {
        Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken cancellationToken);

        
        Task<bool> UpdateAsync(Seat seat, CancellationToken cancellationToken);

        void Detach(Seat seat);
    }
}
