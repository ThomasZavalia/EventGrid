using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using Domain.Events;
using Domain.Primitives;
using MassTransit;

namespace Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;

        public BookingService(IUnitOfWork unitOfWork, IPublishEndpoint publishEndpoint)
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<Result> ReserveSeatAsync(Guid seatId, Guid userId, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId, cancellationToken);

                if (seat == null)
                    return Result.Fail("El asiento no existe.", ErrorType.NotFound);

                var domainResult = seat.Reserve(userId);

                if (domainResult.IsFailure)
                    return Result.Fail(domainResult.Error, ErrorType.Conflict);

                var success = await _unitOfWork.Seats.UpdateAsync(seat, cancellationToken);

                if (!success)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.Seats.Detach(seat);
                    return Result.Fail("El asiento fue reservado por otro usuario mientras intentabas comprar.", ErrorType.Conflict);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result.Fail("Ocurrió un error interno al procesar la reserva.", ErrorType.InternalError);
            }
        }

        public async Task<Result<PaymentInitiatedDto>> InitiatePaymentAsync(Guid seatId, Guid userId, CancellationToken cancellationToken)
        {
            var seat = await _unitOfWork.Seats.GetByIdAsync(seatId, cancellationToken);

            if (seat == null)
                return Result.Fail<PaymentInitiatedDto>("Asiento no encontrado.", ErrorType.NotFound);

            if (seat.Status != Domain.Enums.SeatStatus.Reserved)
                return Result.Fail<PaymentInitiatedDto>("El asiento no está en estado reservado.", ErrorType.Conflict);

            if (seat.UserId != userId)
                return Result.Fail<PaymentInitiatedDto>("Este asiento no te pertenece.", ErrorType.Forbidden);

            await _publishEndpoint.Publish(new PaymentInitiatedEvent
            {
                SeatId = seat.Id,
                UserId = userId,
                Amount = seat.Price,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Result.Success(new PaymentInitiatedDto(seat.Id, seat.Price));
        }
    }
}
