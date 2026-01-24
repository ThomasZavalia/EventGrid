using API.Grpc;
using Grpc.Core;
using Microsoft.IdentityModel.Tokens; 
using System.IdentityModel.Tokens.Jwt; 
using System.Security.Claims;
using System.Text;

namespace API.Services.Grpc
{
    public class BookingGrpcService : BookingGrpc.BookingGrpcBase
    {
        private readonly ILogger<BookingGrpcService> _logger;
        private readonly IConfiguration _configuration;

        public BookingGrpcService(ILogger<BookingGrpcService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override Task<GetQueueTokenResponse> GetQueueToken(GetQueueTokenRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserId))
                {

                    throw new RpcException(new Status(
                        StatusCode.InvalidArgument,
                        "UserId es requerido"
                    ));
                }

                _logger.LogInformation($"[gRPC] Generando JWT para usuario: {request.UserId}");
                try
                {

                    var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
                    var confSecret = _configuration["JwtSettings:Secret"];
                    var secretKey = !string.IsNullOrEmpty(envSecret) ? envSecret : confSecret;
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                   
                    var claims = new[]
                    {
                new Claim(JwtRegisteredClaimNames.Sub, request.UserId), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
                new Claim("is_queue_pass", "true") 
            };

                    
                    var token = new JwtSecurityToken(
                        issuer: _configuration["JwtSettings:Issuer"],
                        audience: _configuration["JwtSettings:Audience"],
                        claims: claims,
                        expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpirationMinutes"]!)),
                        signingCredentials: creds
                    );

                    var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

                  
                    return Task.FromResult(new GetQueueTokenResponse
                    {
                        Token = jwtString,
                        Success = true,
                        Message = "Token JWT generado exitosamente"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando JWT");
                    throw new RpcException(new Status(StatusCode.Internal, "Error interno generando token"));
                }
            }
            catch (RpcException rpcEx)
            {
                
                throw rpcEx;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en GetQueueToken");
                throw new RpcException(new Status(StatusCode.Internal, "Error inesperado"));
            }
        }
    }
}