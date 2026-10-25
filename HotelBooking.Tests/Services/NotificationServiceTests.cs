using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;
using HotelBooking.Services;

namespace HotelBooking.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly string _path;

    public NotificationServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"notifications_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private NotificationService CreateService()
    {
        return new NotificationService(new NotificationRepository(_path));
    }

    private List<Notification> LoadRaw()
    {
        return new NotificationRepository(_path).LoadAll(out _);
    }

    // ---------- Add ----------

    [Fact]
    public void Add_ValidNotification_SavesToRepository()
    {
        var service = CreateService();

        service.Add("alice", new DateOnly(2026, 9, 20), "Ваша заявка принята");

        var saved = LoadRaw();
        Assert.Single(saved);
        Assert.Equal("alice", saved[0].UserName);
    }

    [Fact]
    public void Add_ReturnsNotificationWithSameData()
    {
        var service = CreateService();
        var date = new DateOnly(2026, 9, 20);

        var notification = service.Add("alice", date, "msg");

        Assert.Equal("alice", notification.UserName);
        Assert.Equal(date, notification.Date);
        Assert.Equal("msg", notification.Message);
    }

    [Fact]
    public void Add_EmptyUserName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Add("", new DateOnly(2026, 9, 20), "msg"));
    }

    [Fact]
    public void Add_EmptyMessage_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Add("alice", new DateOnly(2026, 9, 20), ""));
    }

    // ---------- GetByUser ----------

    [Fact]
    public void GetByUser_ExistingUser_ReturnsHisNotifications()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "first");
        service.Add("alice", new DateOnly(2026, 9, 21), "second");

        var result = service.GetByUser("alice");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetByUser_OtherUsersNotifications_NotReturned()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "for alice");
        service.Add("bob", new DateOnly(2026, 9, 20), "for bob");

        var result = service.GetByUser("alice");

        Assert.Single(result);
        Assert.Equal("alice", result[0].UserName);
    }

    [Fact]
    public void GetByUser_NoNotifications_ReturnsEmpty()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "msg");

        var result = service.GetByUser("bob");

        Assert.Empty(result);
    }

    [Fact]
    public void GetByUser_EmptyName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetByUser(""));
    }

    [Fact]
    public void GetByUser_WhitespaceName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetByUser("   "));
    }

    // ---------- DeleteByIndex ----------

    [Fact]
    public void DeleteByIndex_ValidIndex_RemovesCorrectNotification()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "first");
        service.Add("alice", new DateOnly(2026, 9, 21), "second");

        service.DeleteByIndex("alice", 0);

        var remaining = service.GetByUser("alice");
        Assert.Single(remaining);
        Assert.Equal("second", remaining[0].Message);
    }

    [Fact]
    public void DeleteByIndex_OnlyRemovesTargetUserNotification()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "alice first");
        service.Add("bob", new DateOnly(2026, 9, 20), "bob msg");
        service.Add("alice", new DateOnly(2026, 9, 21), "alice second");

        service.DeleteByIndex("alice", 0);

        var aliceRemaining = service.GetByUser("alice");
        var bobRemaining = service.GetByUser("bob");

        Assert.Single(aliceRemaining);
        Assert.Equal("alice second", aliceRemaining[0].Message);
        Assert.Single(bobRemaining);
        Assert.Equal("bob msg", bobRemaining[0].Message);
    }

    [Fact]
    public void DeleteByIndex_LastNotificationOfUser_Works()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "only");
        service.Add("bob", new DateOnly(2026, 9, 20), "bob msg");

        service.DeleteByIndex("alice", 0);

        Assert.Empty(service.GetByUser("alice"));
        Assert.Single(service.GetByUser("bob"));
    }

    [Fact]
    public void DeleteByIndex_IndexOutOfRange_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "msg");

        Assert.Throws<InvalidOperationException>(() =>
            service.DeleteByIndex("alice", 5));
    }

    [Fact]
    public void DeleteByIndex_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "msg");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.DeleteByIndex("alice", -1));
    }

    [Fact]
    public void DeleteByIndex_AfterDeletingMiddle_RemainingKeepOrder()
    {
        var service = CreateService();
        service.Add("alice", new DateOnly(2026, 9, 20), "first");
        service.Add("alice", new DateOnly(2026, 9, 21), "second");
        service.Add("alice", new DateOnly(2026, 9, 22), "third");

        service.DeleteByIndex("alice", 1);

        var aliceRemaining = service.GetByUser("alice");
        Assert.Equal(2, aliceRemaining.Count);
        Assert.Equal("first", aliceRemaining[0].Message);
        Assert.Equal("third", aliceRemaining[1].Message);
    }
}