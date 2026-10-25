using System;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;

namespace HotelBooking.Tests;

public class RoomRepositoryTests : IDisposable
{
    private readonly string _path;

    public RoomRepositoryTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"rooms_{Guid.NewGuid():N}.txt");
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
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_EmptyFile_ReturnsEmptyListAndZeroSkipped()
    {
        WriteFile();
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_OneValidLine_ReturnsOneRoom()
    {
        WriteFile("101 Single Open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Single(rooms);
        Assert.Equal(101, rooms[0].Number);
        Assert.Equal(RoomType.Single, rooms[0].Type);
        Assert.Equal(RoomStatus.Open, rooms[0].Status);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithTwoFields_SkipsLine()
    {
        WriteFile("101 Single");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithFourFields_SkipsLine()
    {
        WriteFile("101 Single Open Extra");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNonNumericNumber_SkipsLine()
    {
        WriteFile("abc Single Open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUnknownRoomType_SkipsLine()
    {
        WriteFile("101 Penthouse Open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUnknownRoomStatus_SkipsLine()
    {
        WriteFile("101 Single Renovating");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_DuplicateRoomNumber_KeepsFirstSkipsSecond()
    {
        WriteFile("101 Single Open", "101 Double Closed");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Single(rooms);
        Assert.Equal(RoomType.Single, rooms[0].Type);
        Assert.Equal(RoomStatus.Open, rooms[0].Status);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNumberZero_SkipsLine()
    {
        WriteFile("0 Single Open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNegativeNumber_SkipsLine()
    {
        WriteFile("-5 Single Open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithExtraSpaces_ParsesCorrectly()
    {
        WriteFile("  101   Single   Open  ");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Single(rooms);
        Assert.Equal(101, rooms[0].Number);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithDifferentCase_SkipsLine()
    {
        WriteFile("101 single open");
        var repo = new RoomRepository(_path);

        var rooms = repo.LoadAll(out var skipped);

        Assert.Empty(rooms);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void SaveAll_WritesAllRoomsToFile()
    {
        var repo = new RoomRepository(_path);
        var rooms = new List<Room>
        {
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Double, RoomStatus.Closed),
        };

        repo.SaveAll(rooms);

        var lines = ReadFile();
        Assert.Equal(2, lines.Length);
        Assert.Equal("101 Single Open", lines[0]);
        Assert.Equal("102 Double Closed", lines[1]);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_RoundTripPreservesData()
    {
        var repo = new RoomRepository(_path);
        var original = new List<Room>
        {
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Double, RoomStatus.Closed),
        };

        repo.SaveAll(original);
        var loaded = repo.LoadAll(out var skipped);

        Assert.Equal(0, skipped);
        Assert.Equal(2, loaded.Count);
        Assert.Equal(original[0].Number, loaded[0].Number);
        Assert.Equal(original[0].Type, loaded[0].Type);
        Assert.Equal(original[0].Status, loaded[0].Status);
        Assert.Equal(original[1].Number, loaded[1].Number);
        Assert.Equal(original[1].Type, loaded[1].Type);
        Assert.Equal(original[1].Status, loaded[1].Status);
    }
}