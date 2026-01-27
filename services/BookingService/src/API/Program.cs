using API.Services.Grpc;
using Application;
using Application.Consumers;
using Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    
    var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
    var confSecret = builder.Configuration["JwtSettings:Secret"];
    var secretKey = !string.IsNullOrEmpty(envSecret) ? envSecret : confSecret;

    if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
    {
        throw new InvalidOperationException("JWT Secret no configurado o muy corto.");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireQueuePass", policy =>
        policy.RequireClaim("is_queue_pass", "true"));
});



builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentProccessorConsumer>();

    

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });

       
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("BookingService"))
            .AddAspNetCoreInstrumentation()
          .AddEntityFrameworkCoreInstrumentation(options =>
          {
           options.EnrichWithIDbCommand = (activity, command) =>
           {
          activity.SetTag("db.statement", command.CommandText);
           };
          })
          .AddMassTransitInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://jaeger:4317"); 
            });
    });

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication(); 

app.UseAuthorization();

app.MapGrpcService<BookingGrpcService>();
app.MapControllers();



app.Run();

