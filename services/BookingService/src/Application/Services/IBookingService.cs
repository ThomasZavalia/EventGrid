using Application.DTOs;
using Domain.Primitives;

namespace Application.Services
{
    public interface IBookingService
    {
        Task<Result> ReserveSeatAsync(Guid seatId, Guid userId, CancellationToken cancellationToken);
        Task<Result<PaymentInitiatedDto>> InitiatePaymentAsync(Guid seatId, Guid userId, CancellationToken cancellationToken);
    }
}
