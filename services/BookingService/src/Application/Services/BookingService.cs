using Application.Interfaces;
using Domain.Enums;
using Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class BookingService : IBookingService
    {

        private readonly IUnitOfWork _unitOfWork;
        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }
        public async Task<Result> ReserveSeatAsync(Guid seatId, Guid userId, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var seat = await _unitOfWork.Seats.GetByIdAsync(seatId, cancellationToken);

                if (seat == null)
                    return Result.Fail("El asiento no existe.",ErrorType.NotFound); 

               
                var domainResult = seat.Reserve(userId); 

                if (domainResult.IsFailure)
                    return Result.Fail(domainResult.Error,ErrorType.Validation); 

               
                var success = await _unitOfWork.Seats.UpdateAsync(seat, cancellationToken);

                if (!success)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _unitOfWork.Seats.Detach(seat);
                    return Result.Fail("El asiento fue reservado por otro usuario mientras intentabas comprar.",ErrorType.Conflict); 
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
               
                return Result.Fail("Ocurrió un error interno al procesar la reserva.");
            }
        }
    }
}
