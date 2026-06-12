using Application.Consumers;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookingService.IntegrationTests;


public class PaymentFlowIntegrationTests
{
    //Helpers internos
    private static Seat CreateReservedSeat(Guid seatId, Guid userId)
    {
        
        var seat = (Seat)Activator.CreateInstance(typeof(Seat), nonPublic: true)!;

       
        typeof(Seat).GetProperty(nameof(Seat.Id))!.SetValue(seat, seatId);
        typeof(Seat).GetProperty(nameof(Seat.Section))!.SetValue(seat, "General");
        typeof(Seat).GetProperty(nameof(Seat.Number))!.SetValue(seat, "A-1");
        typeof(Seat).GetProperty(nameof(Seat.Price))!.SetValue(seat, 150.00m);
        typeof(Seat).GetProperty(nameof(Seat.Status))!.SetValue(seat, SeatStatus.Reserved);
        typeof(Seat).GetProperty(nameof(Seat.UserId))!.SetValue(seat, userId);

        return seat;
    }

   

    
    [Fact]
    public async Task InitiatePayment_DebePublicarPaymentInitiatedEvent_ConDatosCorrectos()
    {
        // ARRANGE
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expectedAmount = 150.00m;

        var reservedSeat = CreateReservedSeat(seatId, userId);

      
        var mockSeatRepo = new Mock<ISeatRepository>();
        mockSeatRepo
            .Setup(r => r.GetByIdAsync(seatId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservedSeat);

        
        var mockUoW = new Mock<IUnitOfWork>();
        mockUoW.Setup(u => u.Seats).Returns(mockSeatRepo.Object);

       
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                
                cfg.AddConsumer<PaymentProccessorConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var bookingService = new Application.Services.BookingService(
            mockUoW.Object,
            harness.Bus  
        );

        // ACT
        var result = await bookingService.InitiatePaymentAsync(
            seatId,
            userId,
            CancellationToken.None
        );

        // ASSERT
  
        Assert.True(result.IsSuccess,
            $"InitiatePaymentAsync falló inesperadamente: {result.Error}");

        
        Assert.True(
            await harness.Published.Any<PaymentInitiatedEvent>(),
            "El BookingService no publicó ningún PaymentInitiatedEvent en el bus."
        );

       
        var publishedMessages = harness.Published.Select<PaymentInitiatedEvent>().ToList();
        Assert.Single(publishedMessages);

        var publishedEvent = publishedMessages.First().Context.Message;
        Assert.Equal(seatId, publishedEvent.SeatId);
        Assert.Equal(userId, publishedEvent.UserId);
        Assert.Equal(expectedAmount, publishedEvent.Amount);

        await harness.Stop();
    }

   
    [Fact]
    public async Task PaymentConsumer_AlRecibirEvento_DebeConfirmarCompraYPublicarSuceso()
    {
        // ARRANGE
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

      
        var reservedSeat = CreateReservedSeat(seatId, userId);

      
        var mockSeatRepo = new Mock<ISeatRepository>();
        mockSeatRepo
            .Setup(r => r.GetByIdAsync(seatId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservedSeat);

       
        var mockUoW = new Mock<IUnitOfWork>();
        mockUoW.Setup(u => u.Seats).Returns(mockSeatRepo.Object);
        mockUoW.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(1); 

      
        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PaymentProccessorConsumer>();
            })
            .AddScoped<IUnitOfWork>(_ => mockUoW.Object)
            .AddLogging()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // ACT 
        
        await harness.Bus.Publish(new PaymentInitiatedEvent
        {
            SeatId = seatId,
            UserId = userId,
            Amount = 150.00m,
            CreatedAt = DateTime.UtcNow
        });

      
        var consumed = await harness.Consumed.Any<PaymentInitiatedEvent>();

        // ASSERT

       
        Assert.True(consumed,
            "El PaymentProccessorConsumer no consumió el PaymentInitiatedEvent.");

      
        mockUoW.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "SaveChangesAsync debería haberse llamado exactamente una vez para persistir el Sold."
        );

        
        Assert.True(
            await harness.Published.Any<PaymentSucceededEvent>(),
            "El consumer no publicó el PaymentSucceededEvent tras confirmar la compra."
        );

        await harness.Stop();
    }

   

    
    [Fact]
    public async Task InitiatePayment_CuandoAsientoNoEstaReservado_NoDebePublicarEvento()
    {
        // ARRANGE
        var seatId = Guid.NewGuid();
        var userId = Guid.NewGuid();

      
        var soldSeat = CreateReservedSeat(seatId, Guid.NewGuid()); 
        typeof(Seat).GetProperty(nameof(Seat.Status))!.SetValue(soldSeat, SeatStatus.Sold);

        var mockSeatRepo = new Mock<ISeatRepository>();
        mockSeatRepo
            .Setup(r => r.GetByIdAsync(seatId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(soldSeat);

        var mockUoW = new Mock<IUnitOfWork>();
        mockUoW.Setup(u => u.Seats).Returns(mockSeatRepo.Object);

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<PaymentProccessorConsumer>();
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var bookingService = new Application.Services.BookingService(
            mockUoW.Object,
            harness.Bus
        );

        // ACT
        var result = await bookingService.InitiatePaymentAsync(
            seatId,
            userId,
            CancellationToken.None
        );

        await Task.Delay(100);

        // ASSERT

        Assert.True(result.IsFailure,
            "El servicio debería haber fallado para un asiento en estado Sold.");
        Assert.Equal(Domain.Enums.ErrorType.Conflict, result.ErrorType);

       
        var anyEventPublished = await harness.Published.Any<PaymentInitiatedEvent>();
        Assert.False(anyEventPublished,
            "El servicio publicó un PaymentInitiatedEvent para un asiento que no estaba Reserved. ¡Bug crítico!");

        await harness.Stop();
    }
}
