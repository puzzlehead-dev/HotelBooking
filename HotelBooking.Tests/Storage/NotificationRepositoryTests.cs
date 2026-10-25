using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;

namespace HotelBooking.Tests;

public class NotificationRepositoryTests : IDisposable
{
    private readonly string _path;

    public NotificationRepositoryTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"notifications_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private void WriteFile(params string[] lines) => File.WriteAllLines(_path, lines);
    private string[] ReadFile() => File.ReadAllLines(_path);

    [Fact]
    public void LoadAll_FileNotExists_ReturnsEmptyListAndZeroSkipped()
    {
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_EmptyFile_ReturnsEmptyListAndZeroSkipped()
    {
        WriteFile();
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_OneValidLine_ReturnsOneNotification()
    {
        WriteFile("alice|2026-09-20|Ваша заявка принята");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal(new DateOnly(2026, 9, 20), list[0].Date);
        Assert.Equal("Ваша заявка принята", list[0].Message);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithTwoFields_SkipsLine()
    {
        WriteFile("alice|2026-09-20");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithFourFields_SkipsLine()
    {
        WriteFile("alice|2026-09-20|msg|extra");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEmptyUserName_SkipsLine()
    {
        WriteFile("|2026-09-20|msg");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithWhitespaceUserName_SkipsLine()
    {
        WriteFile("   |2026-09-20|msg");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEmptyMessage_SkipsLine()
    {
        WriteFile("alice|2026-09-20|");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithInvalidDate_SkipsLine()
    {
        WriteFile("alice|not-a-date|msg");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithExtraSpaces_ParsesCorrectly()
    {
        WriteFile("  alice  |  2026-09-20  |  msg  ");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal(new DateOnly(2026, 9, 20), list[0].Date);
        Assert.Equal("msg", list[0].Message);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_MultipleLines_KeepsOrder()
    {
        WriteFile(
            "alice|2026-09-20|first",
            "bob|2026-09-21|second",
            "carol|2026-09-22|third");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Equal(3, list.Count);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal("bob", list[1].UserName);
        Assert.Equal("carol", list[2].UserName);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_MixValidAndBroken_CountsSkippedCorrectly()
    {
        WriteFile(
            "alice|2026-09-20|ok",
            "broken|line",
            "bob|2026-09-21|ok",
            "|2026-09-21|empty-name");
        var repo = new NotificationRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Equal(2, list.Count);
        Assert.Equal(2, skipped);
    }

    [Fact]
    public void SaveAll_WritesAllNotificationsToFile()
    {
        var repo = new NotificationRepository(_path);
        var list = new List<Notification>
        {
            new Notification("alice", new DateOnly(2026, 9, 20), "first"),
            new Notification("bob", new DateOnly(2026, 9, 21), "second"),
        };

        repo.SaveAll(list);

        var lines = ReadFile();
        Assert.Equal(2, lines.Length);
        Assert.Equal("alice|2026-09-20|first", lines[0]);
        Assert.Equal("bob|2026-09-21|second", lines[1]);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_RoundTripPreservesData()
    {
        var repo = new NotificationRepository(_path);
        var original = new List<Notification>
        {
            new Notification("alice", new DateOnly(2026, 9, 20), "first"),
            new Notification("bob", new DateOnly(2026, 9, 21), "second"),
        };

        repo.SaveAll(original);
        var loaded = repo.LoadAll(out var skipped);

        Assert.Equal(0, skipped);
        Assert.Equal(2, loaded.Count);
        Assert.Equal(original[0].UserName, loaded[0].UserName);
        Assert.Equal(original[0].Date, loaded[0].Date);
        Assert.Equal(original[0].Message, loaded[0].Message);
        Assert.Equal(original[1].UserName, loaded[1].UserName);
        Assert.Equal(original[1].Date, loaded[1].Date);
        Assert.Equal(original[1].Message, loaded[1].Message);
    }

    [Fact]
    public void SaveAll_EmptyList_WritesEmptyFile()
    {
        var repo = new NotificationRepository(_path);

        repo.SaveAll(new List<Notification>());

        Assert.True(File.Exists(_path));
        Assert.Empty(ReadFile());
    }

    [Fact]
    public void SaveAll_OverwritesPreviousContent()
    {
        WriteFile("old|2026-09-20|stale");
        var repo = new NotificationRepository(_path);

        repo.SaveAll(new List<Notification>
        {
            new Notification("alice", new DateOnly(2026, 9, 21), "new"),
        });

        var lines = ReadFile();
        Assert.Single(lines);
        Assert.Equal("alice|2026-09-21|new", lines[0]);
    }
}