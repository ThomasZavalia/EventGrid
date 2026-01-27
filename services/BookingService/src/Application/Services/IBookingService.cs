using Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public interface IBookingService
    {
        Task<Result> ReserveSeatAsync( Guid seatId, Guid userId, CancellationToken cancellationToken);
    }
}
