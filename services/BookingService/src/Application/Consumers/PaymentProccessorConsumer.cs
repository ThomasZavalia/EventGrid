using Domain.Enums;
using Domain.Events;
using Microsoft.Extensions.Logging;
using MassTransit;
using Application.Interfaces;

namespace Application.Consumers
{
    public class PaymentProcessorConsumer : IConsumer<PaymentInitiatedEvent>
    {
        private readonly ILogger<PaymentProcessorConsumer> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentProcessorConsumer(ILogger<PaymentProcessorConsumer> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<PaymentInitiatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation(
                "Procesando pago para Seat {SeatId} del usuario {UserId}",
                message.SeatId, message.UserId);

            var seat = await _unitOfWork.Seats.GetByIdAsync(
                message.SeatId, trackChanges: true, context.CancellationToken);

            if (seat == null)
            {
               
                _logger.LogError(
                    "Asiento {SeatId} no encontrado. Mensaje descartado.",
                    message.SeatId);
                return;
            }

            if (seat.Status == SeatStatus.Sold)
            {
               
                _logger.LogWarning(
                    "Asiento {SeatId} ya está en estado Sold. Mensaje duplicado ignorado.",
                    message.SeatId);
                return;
            }

            if (seat.UserId != message.UserId)
            {
               
                throw new InvalidOperationException(
                    $"Inconsistencia de datos: asiento {message.SeatId} pertenece al usuario " +
                    $"{seat.UserId}, pero el evento indica {message.UserId}.");
            }

            if (seat.Status != SeatStatus.Reserved)
            {
               
                throw new InvalidOperationException(
                    $"Estado inesperado del asiento {message.SeatId}: '{seat.Status}'. " +
                    $"Se esperaba 'Reserved'.");
            }

            var result = seat.ConfirmPurchase();
            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Error de dominio al confirmar asiento {seat.Id}: {result.Error}");
            }

            await _unitOfWork.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Pago confirmado. Asiento {SeatNumber} (Id: {SeatId}) vendido al usuario {UserId}.",
                seat.Number, seat.Id, message.UserId);

            await context.Publish(new PaymentSucceededEvent
            {
                SeatId = seat.Id,
                UserId = message.UserId,
                ProcessedAt = DateTime.UtcNow
            });
        }
    }
}
