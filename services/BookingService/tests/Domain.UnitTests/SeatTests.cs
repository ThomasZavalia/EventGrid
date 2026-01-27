using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Domain.UnitTests;

public class SeatTests
{
    [Fact]
    public void Reserve_Should_SetStatusToReserved_When_SeatIsAvailable()
    {
        
        var seat = new Seat("A", "1", 100, Guid.NewGuid()); 
        var userId = Guid.NewGuid();

        
        var result = seat.Reserve(userId);

        
        result.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Reserved);
        seat.UserId.Should().Be(userId);
    }

    [Fact]
    public void Reserve_Should_Fail_When_SeatIsAlreadyReserved()
    {
      
        var seat = new Seat("A", "1", 100, Guid.NewGuid());
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        
        seat.Reserve(userId1);

        var result = seat.Reserve(userId2);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"El asiento {seat.Number} no esta disponible.");
        seat.UserId.Should().Be(userId1); 
    }

    [Fact]
    public void ConfirmPurchase_Should_Fail_When_SeatIsNotReserved()
    {

        var seat = new Seat("A", "1", 100, Guid.NewGuid());


        var result = seat.ConfirmPurchase();

    
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("debe estar reservado");
        seat.Status.Should().Be(SeatStatus.Available);
    }

    [Fact]
    public void ConfirmPurchase_Should_Succeed_When_StatusIsReserved()
    {
  
        var seat = new Seat("A", "1", 100, Guid.NewGuid());
        var userId = Guid.NewGuid();
        seat.Reserve(userId); 

        
        var result = seat.ConfirmPurchase();

      
        result.IsSuccess.Should().BeTrue();
        seat.Status.Should().Be(SeatStatus.Sold);
    }
}