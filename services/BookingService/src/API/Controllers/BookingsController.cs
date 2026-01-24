using Application.DTOs;
using Application.Services;
using Application.Validators;
using Domain.Enums;
using Domain.Primitives;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public BookingsController(IBookingService bookingService, IValidator<ReserveSeatRequest> validator)
        {
            _bookingService = bookingService;
            _validator = validator;
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
    }
    }

