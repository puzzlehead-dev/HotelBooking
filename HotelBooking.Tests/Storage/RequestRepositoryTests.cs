using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;

namespace HotelBooking.Tests;

public class RequestRepositoryTests : IDisposable
{
    private readonly string _path;

    public RequestRepositoryTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"requests_{Guid.NewGuid():N}.txt");
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
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_EmptyFile_ReturnsEmptyListAndZeroSkipped()
    {
        WriteFile();
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_OneValidLine_ReturnsOneRequest()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal(RoomType.Single, list[0].RoomType);
        Assert.Equal(new DateOnly(2026, 9, 20), list[0].DateStarting);
        Assert.Equal(3, list[0].QuantityDays);
        Assert.Equal(RequestStatus.Unprocessed, list[0].Status);
        Assert.Null(list[0].RoomNumber);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithDashRoomNumber_RoomNumberIsNull()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Null(list[0].RoomNumber);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithRoomNumber_ParsesRoomNumber()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Accepted|101");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal(101, list[0].RoomNumber);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNonNumericRoomNumber_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Accepted|abc");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithZeroRoomNumber_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Accepted|0");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNegativeRoomNumber_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Accepted|-5");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithSixFields_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Unprocessed");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEightFields_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Unprocessed|-|extra");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNonNumericId_SkipsLine()
    {
        WriteFile("abc|alice|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithIdZero_SkipsLine()
    {
        WriteFile("0|alice|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNegativeId_SkipsLine()
    {
        WriteFile("-1|alice|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithEmptyUserName_SkipsLine()
    {
        WriteFile("1||Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithWhitespaceUserName_SkipsLine()
    {
        WriteFile("1|   |Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUserNameContainingSpace_ParsesCorrectly()
    {
        WriteFile("1|alice smith|Single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal("alice smith", list[0].UserName);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUnknownRoomType_SkipsLine()
    {
        WriteFile("1|alice|Penthouse|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithLowercaseRoomType_SkipsLine()
    {
        WriteFile("1|alice|single|2026-09-20|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithInvalidDate_SkipsLine()
    {
        WriteFile("1|alice|Single|not-a-date|3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithNonNumericQuantityDays_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|abc|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithQuantityDaysZero_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|0|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithQuantityDaysNegative_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|-3|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithUnknownStatus_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|Pending|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_LineWithLowercaseStatus_SkipsLine()
    {
        WriteFile("1|alice|Single|2026-09-20|3|unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Empty(list);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_DuplicateId_KeepsFirstSkipsSecond()
    {
        WriteFile(
            "1|alice|Single|2026-09-20|3|Unprocessed|-",
            "1|bob|Double|2026-09-21|5|Accepted|102");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal(RequestStatus.Unprocessed, list[0].Status);
        Assert.Equal(1, skipped);
    }

    [Fact]
    public void LoadAll_ExtraSpacesAroundFields_ParsesCorrectly()
    {
        WriteFile("  1  |  alice  |  Single  |  2026-09-20  |  3  |  Unprocessed  |  -  ");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("alice", list[0].UserName);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_MultipleLines_KeepsOrder()
    {
        WriteFile(
            "1|alice|Single|2026-09-20|3|Unprocessed|-",
            "2|bob|Double|2026-09-21|5|Accepted|102",
            "3|carol|Triple|2026-09-22|2|Rejected|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0].Id);
        Assert.Equal(2, list[1].Id);
        Assert.Equal(3, list[2].Id);
        Assert.Equal(0, skipped);
    }

    [Fact]
    public void LoadAll_MixValidAndBroken_CountsSkippedCorrectly()
    {
        WriteFile(
            "1|alice|Single|2026-09-20|3|Unprocessed|-",
            "broken line",
            "2|bob|Double|2026-09-21|5|Accepted|102",
            "3|carol|Triple|not-a-date|2|Unprocessed|-");
        var repo = new RequestRepository(_path);

        var list = repo.LoadAll(out var skipped);

        Assert.Equal(2, list.Count);
        Assert.Equal(2, skipped);
    }

    [Fact]
    public void SaveAll_WritesAllRequestsToFile()
    {
        var repo = new RequestRepository(_path);
        var list = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed),
            new Request(2, "bob", RoomType.Double, new DateOnly(2026, 9, 21), 5, RequestStatus.Accepted),
        };

        repo.SaveAll(list);

        var lines = ReadFile();
        Assert.Equal(2, lines.Length);
        Assert.Equal("1|alice|Single|2026-09-20|3|Unprocessed|-", lines[0]);
        Assert.Equal("2|bob|Double|2026-09-21|5|Accepted|-", lines[1]);
    }

    [Fact]
    public void SaveAll_RequestWithAssignedRoom_WritesRoomNumber()
    {
        var repo = new RequestRepository(_path);
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Accepted);
        request.AssignRoom(101);

        repo.SaveAll(new List<Request> { request });

        var lines = ReadFile();
        Assert.Single(lines);
        Assert.Equal("1|alice|Single|2026-09-20|3|Accepted|101", lines[0]);
    }

    [Fact]
    public void SaveAll_RequestWithoutRoom_WritesDash()
    {
        var repo = new RequestRepository(_path);
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed);

        repo.SaveAll(new List<Request> { request });

        var lines = ReadFile();
        Assert.Single(lines);
        Assert.EndsWith("|-", lines[0]);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_RoundTripPreservesData()
    {
        var repo = new RequestRepository(_path);
        var original = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed),
            new Request(2, "bob", RoomType.Double, new DateOnly(2026, 9, 21), 5, RequestStatus.Accepted),
        };

        repo.SaveAll(original);
        var loaded = repo.LoadAll(out var skipped);

        Assert.Equal(0, skipped);
        Assert.Equal(2, loaded.Count);
        Assert.Equal(original[0].Id, loaded[0].Id);
        Assert.Equal(original[0].UserName, loaded[0].UserName);
        Assert.Equal(original[0].RoomType, loaded[0].RoomType);
        Assert.Equal(original[0].DateStarting, loaded[0].DateStarting);
        Assert.Equal(original[0].QuantityDays, loaded[0].QuantityDays);
        Assert.Equal(original[0].Status, loaded[0].Status);
        Assert.Null(loaded[0].RoomNumber);
        Assert.Equal(original[1].Id, loaded[1].Id);
        Assert.Equal(original[1].UserName, loaded[1].UserName);
        Assert.Equal(original[1].RoomType, loaded[1].RoomType);
        Assert.Equal(original[1].DateStarting, loaded[1].DateStarting);
        Assert.Equal(original[1].QuantityDays, loaded[1].QuantityDays);
        Assert.Equal(original[1].Status, loaded[1].Status);
        Assert.Null(loaded[1].RoomNumber);
    }

    [Fact]
    public void SaveAll_ThenLoadAll_PreservesAssignedRoomNumber()
    {
        var repo = new RequestRepository(_path);
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Accepted);
        request.AssignRoom(101);

        repo.SaveAll(new List<Request> { request });
        var loaded = repo.LoadAll(out var skipped);

        Assert.Equal(0, skipped);
        Assert.Single(loaded);
        Assert.Equal(101, loaded[0].RoomNumber);
    }

    [Fact]
    public void SaveAll_EmptyList_WritesEmptyFile()
    {
        var repo = new RequestRepository(_path);

        repo.SaveAll(new List<Request>());

        Assert.True(File.Exists(_path));
        Assert.Empty(ReadFile());
    }

    [Fact]
    public void SaveAll_OverwritesPreviousContent()
    {
        WriteFile("9|old|Single|2026-01-01|1|Unprocessed|-");
        var repo = new RequestRepository(_path);

        repo.SaveAll(new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 20), 3, RequestStatus.Unprocessed),
        });

        var lines = ReadFile();
        Assert.Single(lines);
        Assert.Equal("1|alice|Single|2026-09-20|3|Unprocessed|-", lines[0]);
    }
}