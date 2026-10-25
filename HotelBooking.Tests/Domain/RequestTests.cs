using System;
using Xunit;
using HotelBooking.Domain;

namespace HotelBooking.Tests;

public class RequestTests
{
    [Fact]
    public void CreateValidRequest_Ok()
    {
        var request = new Request(
            1,
            "alice",
            RoomType.Single,
            new DateOnly(2026, 9, 20),
            3,
            RequestStatus.Unprocessed);

        Assert.Equal(1, request.Id);
        Assert.Equal("alice", request.UserName);
        Assert.Equal(RoomType.Single, request.RoomType);
        Assert.Equal(new DateOnly(2026, 9, 20), request.DateStarting);
        Assert.Equal(3, request.QuantityDays);
        Assert.Equal(RequestStatus.Unprocessed, request.Status);
        Assert.Null(request.RoomNumber);
    }

    [Fact]
    public void CreateRequestWithIdZero_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Request(0, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed));
    }

    [Fact]
    public void CreateRequestWithEmptyUserName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Request(1, "", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed));
    }

    [Fact]
    public void CreateRequestWithQuantityDaysZero_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 0, RequestStatus.Unprocessed));
    }

    [Fact]
    public void AssignRoomZero_ThrowsArgumentException()
    {
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed);

        Assert.Throws<ArgumentException>(() => request.AssignRoom(0));
    }

    [Fact]
    public void AssignRoomFive_SetsRoomNumber()
    {
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed);

        request.AssignRoom(5);

        Assert.Equal(5, request.RoomNumber);
    }

    [Fact]
    public void ChangeStatus_UpdatesStatus()
    {
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed);

        request.ChangeStatus(RequestStatus.Accepted);

        Assert.Equal(RequestStatus.Accepted, request.Status);
    }

    [Fact]
    public void AssignRoomNegative_ThrowsArgumentException()
    {
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed);

        Assert.Throws<ArgumentException>(() => request.AssignRoom(-5));
    }

    [Fact]
    public void CreateRequestWithWhitespaceUserName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Request(1, "   ", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed));
    }

    [Fact]
    public void CreateRequestWithQuantityDaysNegative_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), -1, RequestStatus.Unprocessed));
    }
}