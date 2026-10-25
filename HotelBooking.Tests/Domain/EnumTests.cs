using Xunit;
using HotelBooking.Domain;

namespace HotelBooking.Tests;

public class EnumsTests
{
    [Fact]
    public void DefaultRequestStatus_IsUnprocessed()
    {
        Assert.Equal(RequestStatus.Unprocessed, default(RequestStatus));
    }

    [Fact]
    public void DefaultRoomStatus_IsOpen()
    {
        Assert.Equal(RoomStatus.Open, default(RoomStatus));
    }

    [Fact]
    public void RoomType_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(RoomType), RoomType.Single));
        Assert.True(Enum.IsDefined(typeof(RoomType), RoomType.Double));
        Assert.True(Enum.IsDefined(typeof(RoomType), RoomType.Triple));
        Assert.True(Enum.IsDefined(typeof(RoomType), RoomType.Quadruple));
        Assert.Equal(4, Enum.GetValues(typeof(RoomType)).Length);
    }
}