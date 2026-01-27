using Domain.Enums;
using Domain.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using MassTransit;
using Application.Interfaces;


namespace Application.Consumers
{
    public class PaymentProccessorConsumer : IConsumer<PaymentInitiatedEvent>
    {
        private readonly ILogger<PaymentProccessorConsumer> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentProccessorConsumer(ILogger<PaymentProccessorConsumer> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task Consume(ConsumeContext<PaymentInitiatedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation($" Procesando pago para Seat {message.SeatId} del usuario {message.UserId}...");

            
            await Task.Delay(2000);

            try { 
            var seat = await _unitOfWork.Seats.GetByIdAsync(message.SeatId,context.CancellationToken);

                if (seat == null)
            {
                _logger.LogError($"Asiento {message.SeatId} para finalizar pago");
                return;
            }

                if (seat.UserId != message.UserId)
                {
                    _logger.LogWarning($"UserIds no coinciden. Esperado: {seat.UserId}, Recibido: {message.UserId}");
                    return;
                }

                if(seat.Status != SeatStatus.Reserved)
                {
                    _logger.LogWarning($"El asiento {seat.Id} no está en estado 'Reserved'. Estado actual: {seat.Status}");
                    return;
                }

                var result = seat.ConfirmPurchase();
                if (result.IsFailure)
                {
                    _logger.LogError($"Error al confirmar la compra del asiento {seat.Id}: {result.Error}");
                    return;
                }
                
               await _unitOfWork.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation($"PAGO CONFIRMADO. Asiento {seat.Number} vendido.");

                await context.Publish(new PaymentSucceededEvent
                {
                    SeatId = seat.Id,
                    UserId = message.UserId,
                    ProcessedAt = DateTime.UtcNow
                });
            }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando pago");
            throw; 
        }
}
    }
}
