using System;
using Xunit;
using HotelBooking.Domain;

namespace HotelBooking.Tests;

public class NotificationTests
{
    [Fact]
    public void CreateValidNotification_Ok()
    {
        var n = new Notification("alice", new DateOnly(2026, 9, 20), "Ваша заявка принята");

        Assert.Equal("alice", n.UserName);
        Assert.Equal(new DateOnly(2026, 9, 20), n.Date);
        Assert.Equal("Ваша заявка принята", n.Message);
    }

    [Fact]
    public void CreateNotificationWithEmptyUserName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notification("", new DateOnly(2026, 9, 20), "msg"));
    }

    [Fact]
    public void CreateNotificationWithEmptyMessage_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notification("alice", new DateOnly(2026, 9, 20), ""));
    }

    [Fact]
    public void CreateNotificationWithDefaultDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new Notification("alice", default, "msg"));
    }
}