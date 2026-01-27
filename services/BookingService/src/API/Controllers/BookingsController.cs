using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.Validators;
using Domain.Enums;
using Domain.Events;
using Domain.Primitives;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IValidator<ReserveSeatRequest> _validator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(
            IBookingService bookingService,
            IValidator<ReserveSeatRequest> validator,
            IPublishEndpoint publishEndpoint,
            IUnitOfWork unitOfWork,
            ILogger<BookingsController> logger
            
            )
        {
            _bookingService = bookingService;
            _validator = validator;
            _publishEndpoint = publishEndpoint;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        [HttpPost("reserve")]
        [Authorize(Policy="RequireQueuePass")]
        public async Task<IActionResult> ReserveSeat([FromBody] ReserveSeatRequest request,CancellationToken cancellationToken)
        {

            Console.WriteLine("--- CLAIMS RECIBIDOS ---");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Key: {claim.Type} | Value: {claim.Value}");
            }
            Console.WriteLine("------------------------");
            
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

           
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { error = "Token inválido: No se encontró el ID del usuario (ni como NameIdentifier ni como sub)" });
            }

            
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return BadRequest(new { error = $"El ID '{userIdString}' no es un GUID válido." });
            }

            var result = await _bookingService.ReserveSeatAsync(request.SeatId,userId,cancellationToken);
            if (result.IsSuccess)
            {
                
                return Ok(new { message = "Asiento reservado con éxito" });
            }



            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { error = result.Error }),
                ErrorType.Conflict => Conflict(new { error = result.Error }),
                ErrorType.Validation => BadRequest(new { error = result.Error }),
                _ => StatusCode(500, new { error = "Error interno del servidor" })
            };
        }


        [HttpPost("confirm-payment")]
        [Authorize(Policy = "RequireQueuePass")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request, CancellationToken cancellationToken)
        {
          
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { error = "Token inválido" });
            }

            
            var seat = await _unitOfWork.Seats.GetByIdAsync(request.SeatId, cancellationToken);

            if (seat == null)
                return NotFound(new { error = "Asiento no encontrado" });

            if (seat.Status != SeatStatus.Reserved)
                return BadRequest(new { error = "El asiento no está reservado" });

            if (seat.UserId != userId)
                return Forbid(); 

            
            _logger.LogInformation($" Publicando evento de pago para Seat {seat.Id}");

            await _publishEndpoint.Publish(new PaymentInitiatedEvent
            {
                SeatId = seat.Id,
                UserId = userId,
                Amount = seat.Price,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return Accepted(new
            {
                message = "Pago en proceso. Te notificaremos cuando se confirme.",
                seatId = seat.Id,
                amount = seat.Price
            });
        }
    }
}
    

