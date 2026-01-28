using Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IIdentityService
    {
        Task<(bool IsSuccess, string ErrorMessage)> RegisterAsync(RegisterRequest request);
        Task<(bool IsSuccess, AuthResponse Response, string ErrorMessage)> LoginAsync(LoginRequest request);
    }
}
