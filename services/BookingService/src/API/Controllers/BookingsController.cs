using Application.DTOs;
using Application.Services;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingsController> _logger;

        public BookingsController(IBookingService bookingService, ILogger<BookingsController> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        [HttpPost("reserve")]
        [Authorize(Policy = "RequireQueuePass")]
        public async Task<IActionResult> ReserveSeat([FromBody] ReserveSeatRequest request, CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdString))
                return Unauthorized(new { error = "Token inválido: no se encontró el ID del usuario." });

            if (!Guid.TryParse(userIdString, out var userId))
                return BadRequest(new { error = $"El ID '{userIdString}' no es un GUID válido." });

            var result = await _bookingService.ReserveSeatAsync(request.SeatId, userId, cancellationToken);

            if (result.IsSuccess)
                return Ok(new { message = "Asiento reservado con éxito." });

            return result.ErrorType switch
            {
                ErrorType.NotFound    => NotFound(new { error = result.Error }),
                ErrorType.Conflict    => Conflict(new { error = result.Error }),
                ErrorType.Validation  => BadRequest(new { error = result.Error }),
                _                     => StatusCode(500, new { error = "Error interno del servidor." })
            };
        }

        [HttpPost("confirm-payment")]
        [Authorize(Policy = "RequireQueuePass")]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { error = "Token inválido." });

            _logger.LogInformation("Iniciando pago para Seat {SeatId} por usuario {UserId}", request.SeatId, userId);

            var result = await _bookingService.InitiatePaymentAsync(request.SeatId, userId, cancellationToken);

            if (result.IsSuccess)
                return Accepted(new
                {
                    message = "Pago en proceso. Te notificaremos cuando se confirme.",
                    seatId = result.Value.SeatId,
                    amount = result.Value.Amount
                });

            return result.ErrorType switch
            {
                ErrorType.NotFound  => NotFound(new { error = result.Error }),
                ErrorType.Conflict  => Conflict(new { error = result.Error }),
                ErrorType.Forbidden => Forbid(),
                _                   => StatusCode(500, new { error = "Error interno del servidor." })
            };
        }
    }
}
  

