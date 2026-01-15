using Application.DTOs;
using Application.Services;
using Application.Validators;
using Domain.Enums;
using Domain.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }
        [HttpPost("reserve")]
        public async Task<IActionResult> ReserveSeat([FromBody] ReserveSeatRequest request, CancellationToken cancellationToken)
        {

            var validator = new ReserveSeatRequestValidator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            var result = await _bookingService.ReserveSeatAsync(request.SeatId, request.UserId, cancellationToken);
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
    }
    }

