using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Auth
{
    public record RegisterRequest(string Email, string Password, string FirstName, string LastName);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string Id, string Email, string Token);
}
