using API.Grpc;
using Application.Interfaces;
using Grpc.Core;

namespace API.Services.Grpc
{
    public class BookingGrpcService : BookingGrpc.BookingGrpcBase
    {
        private readonly ILogger<BookingGrpcService> _logger;
        private readonly IJwtTokenService _jwtTokenService;

        public BookingGrpcService(ILogger<BookingGrpcService> logger, IJwtTokenService jwtTokenService)
        {
            _logger = logger;
            _jwtTokenService = jwtTokenService;
        }

        public override Task<GetQueueTokenResponse> GetQueueToken(GetQueueTokenRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId es requerido"));
            }

            try
            {
                _logger.LogInformation("Generando QueuePass token para usuario {UserId}", request.UserId);

                var token = _jwtTokenService.GenerateQueuePassToken(request.UserId);

                return Task.FromResult(new GetQueueTokenResponse
                {
                    Token = token,
                    Success = true,
                    Message = "Token generado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando QueuePass token para usuario {UserId}", request.UserId);
                throw new RpcException(new Status(StatusCode.Internal, "Error interno generando token"));
            }
        }
    }
}