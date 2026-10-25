using System;
using Xunit;
using HotelBooking.Domain;

namespace HotelBooking.Tests;

public class RoomTests
{
    [Fact]
    public void CreateValidRoom_Ok()
    {
        var room = new Room(101, RoomType.Double, RoomStatus.Open);

        Assert.Equal(101, room.Number);
        Assert.Equal(RoomType.Double, room.Type);
        Assert.Equal(RoomStatus.Open, room.Status);
    }

    [Fact]
    public void CreateRoomWithNumberZero_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Room(0, RoomType.Double, RoomStatus.Open));
    }

    [Fact]
    public void CreateRoomWithNegativeNumber_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Room(-1, RoomType.Double, RoomStatus.Open));
    }

    [Fact]
    public void ChangeStatus_UpdatesStatus()
    {
        var room = new Room(101, RoomType.Double, RoomStatus.Open);

        room.ChangeStatus(RoomStatus.Closed);

        Assert.Equal(RoomStatus.Closed, room.Status);
    }

    [Fact]
    public void ChangeStatus_SameStatus_NoChange()
    {
        var room = new Room(101, RoomType.Double, RoomStatus.Open);

        room.ChangeStatus(RoomStatus.Open);

        Assert.Equal(RoomStatus.Open, room.Status);
    }

    [Fact]
    public void CreateRoomWithNumberOne_Ok()
    {
        var room = new Room(1, RoomType.Single, RoomStatus.Open);

        Assert.Equal(1, room.Number);
    }
}