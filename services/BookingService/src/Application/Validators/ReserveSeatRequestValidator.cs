using Application.DTOs;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Validators
{
    public class ReserveSeatRequestValidator:AbstractValidator<ReserveSeatRequest>
    {
        public ReserveSeatRequestValidator()
        {
            RuleFor(x => x.SeatId).NotEmpty().WithMessage("El ID del asiento es obligatorio.");
            RuleFor(x => x.UserId).NotEmpty().WithMessage("El ID del usuario es obligatorio.");
        }
    }
}
