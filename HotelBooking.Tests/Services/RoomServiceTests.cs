using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;
using HotelBooking.Services;

namespace HotelBooking.Tests;

public class RoomServiceTests : IDisposable
{
    private readonly string _path;

    public RoomServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"rooms_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    private RoomService CreateService()
    {
        return new RoomService(new RoomRepository(_path));
    }

    private List<Room> LoadRaw()
    {
        return new RoomRepository(_path).LoadAll(out _);
    }

    // ---------- Add ----------

    [Fact]
    public void Add_ValidRoom_SavesToRepository()
    {
        var service = CreateService();

        service.Add(101, RoomType.Single);

        var saved = LoadRaw();
        Assert.Single(saved);
        Assert.Equal(101, saved[0].Number);
    }

    [Fact]
    public void Add_ReturnsRoomWithCorrectData()
    {
        var service = CreateService();

        var room = service.Add(101, RoomType.Double);

        Assert.Equal(101, room.Number);
        Assert.Equal(RoomType.Double, room.Type);
    }

    [Fact]
    public void Add_NewRoom_DefaultStatusIsOpen()
    {
        var service = CreateService();

        var room = service.Add(101, RoomType.Single);

        Assert.Equal(RoomStatus.Open, room.Status);
    }

    [Fact]
    public void Add_DuplicateNumber_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        Assert.Throws<InvalidOperationException>(() =>
            service.Add(101, RoomType.Double));
    }

    [Fact]
    public void Add_NumberZero_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.Add(0, RoomType.Single));
    }

    [Fact]
    public void Add_NegativeNumber_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.Add(-1, RoomType.Single));
    }

    // ---------- Delete ----------

    [Fact]
    public void Delete_ExistingRoom_RemovesFromRepository()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        service.Delete(101);

        Assert.Empty(LoadRaw());
    }

    [Fact]
    public void Delete_NonexistentRoom_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        Assert.Throws<InvalidOperationException>(() => service.Delete(999));
    }

    [Fact]
    public void Delete_OneOfMany_KeepsOthers()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);
        service.Add(102, RoomType.Double);
        service.Add(103, RoomType.Triple);

        service.Delete(102);

        var saved = LoadRaw();
        Assert.Equal(2, saved.Count);
        Assert.Equal(101, saved[0].Number);
        Assert.Equal(103, saved[1].Number);
    }

    // ---------- ChangeStatus ----------

    [Fact]
    public void ChangeStatus_ExistingRoom_UpdatesStatus()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        service.ChangeStatus(101, RoomStatus.Closed);

        var saved = LoadRaw();
        Assert.Equal(RoomStatus.Closed, saved[0].Status);
    }

    [Fact]
    public void ChangeStatus_NonexistentRoom_ThrowsInvalidOperationException()
    {
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.ChangeStatus(999, RoomStatus.Closed));
    }

    [Fact]
    public void ChangeStatus_SameStatus_NoException()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        service.ChangeStatus(101, RoomStatus.Open);

        Assert.Equal(RoomStatus.Open, LoadRaw()[0].Status);
    }

    // ---------- GetAll ----------

    [Fact]
    public void GetAll_EmptyRepository_ReturnsEmptyList()
    {
        var service = CreateService();

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void GetAll_MultipleRooms_ReturnsAll()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);
        service.Add(102, RoomType.Double);

        var all = service.GetAll();

        Assert.Equal(2, all.Count);
    }

    // ---------- GetOpenRoomNumbers ----------

    [Fact]
    public void GetOpenRoomNumbers_ReturnsOnlyOpenOfType()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);
        service.Add(102, RoomType.Single);
        service.Add(103, RoomType.Double);
        service.ChangeStatus(102, RoomStatus.Closed);

        var result = service.GetOpenRoomNumbers(RoomType.Single);

        Assert.Single(result);
        Assert.Equal(101, result[0]);
    }

    [Fact]
    public void GetOpenRoomNumbers_NoneMatching_ReturnsEmpty()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        var result = service.GetOpenRoomNumbers(RoomType.Quadruple);

        Assert.Empty(result);
    }

    [Fact]
    public void GetOpenRoomNumbers_AllClosed_ReturnsEmpty()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);
        service.ChangeStatus(101, RoomStatus.Closed);

        var result = service.GetOpenRoomNumbers(RoomType.Single);

        Assert.Empty(result);
    }

    // ---------- HasOpenRooms ----------

    [Fact]
    public void HasOpenRooms_HasOpen_ReturnsTrue()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);

        Assert.True(service.HasOpenRooms(RoomType.Single));
    }

    [Fact]
    public void HasOpenRooms_OnlyClosed_ReturnsFalse()
    {
        var service = CreateService();
        service.Add(101, RoomType.Single);
        service.ChangeStatus(101, RoomStatus.Closed);

        Assert.False(service.HasOpenRooms(RoomType.Single));
    }

    [Fact]
    public void HasOpenRooms_NoRooms_ReturnsFalse()
    {
        var service = CreateService();

        Assert.False(service.HasOpenRooms(RoomType.Single));
    }

    // ---------- Sort ----------

    private List<Room> MakeUnsortedRooms()
    {
        return new List<Room>
        {
            new Room(105, RoomType.Double, RoomStatus.Open),
            new Room(101, RoomType.Quadruple, RoomStatus.Open),
            new Room(103, RoomType.Single, RoomStatus.Open),
        };
    }

    [Fact]
    public void Sort_ByNumberAscending()
    {
        var service = CreateService();

        var sorted = service.Sort(MakeUnsortedRooms(), RoomSortField.Number, true);

        Assert.Equal(101, sorted[0].Number);
        Assert.Equal(103, sorted[1].Number);
        Assert.Equal(105, sorted[2].Number);
    }

    [Fact]
    public void Sort_ByNumberDescending()
    {
        var service = CreateService();

        var sorted = service.Sort(MakeUnsortedRooms(), RoomSortField.Number, false);

        Assert.Equal(105, sorted[0].Number);
        Assert.Equal(103, sorted[1].Number);
        Assert.Equal(101, sorted[2].Number);
    }

    [Fact]
    public void Sort_ByTypeAscending()
    {
        var service = CreateService();

        var sorted = service.Sort(MakeUnsortedRooms(), RoomSortField.Type, true);

        Assert.Equal(RoomType.Single, sorted[0].Type);
        Assert.Equal(RoomType.Double, sorted[1].Type);
        Assert.Equal(RoomType.Quadruple, sorted[2].Type);
    }

    [Fact]
    public void Sort_DoesNotMutateOriginalList()
    {
        var service = CreateService();
        var original = MakeUnsortedRooms();
        var firstBefore = original[0].Number;

        service.Sort(original, RoomSortField.Number, true);

        Assert.Equal(firstBefore, original[0].Number);
    }

    [Fact]
    public void Sort_EmptyList_ReturnsEmpty()
    {
        var service = CreateService();

        var sorted = service.Sort(new List<Room>(), RoomSortField.Number, true);

        Assert.Empty(sorted);
    }

    // ---------- FilterByNumber ----------

    [Fact]
    public void FilterByNumber_InRange()
    {
        var service = CreateService();

        var result = service.FilterByNumber(MakeUnsortedRooms(), 102, 104, invert: false);

        Assert.Single(result);
        Assert.Equal(103, result[0].Number);
    }

    [Fact]
    public void FilterByNumber_Invert()
    {
        var service = CreateService();

        var result = service.FilterByNumber(MakeUnsortedRooms(), 102, 104, invert: true);

        Assert.Equal(2, result.Count);
        Assert.Equal(105, result[0].Number);
        Assert.Equal(101, result[1].Number);
    }

    [Fact]
    public void FilterByNumber_NoMatches_ReturnsEmpty()
    {
        var service = CreateService();

        var result = service.FilterByNumber(MakeUnsortedRooms(), 200, 300, invert: false);

        Assert.Empty(result);
    }

    // ---------- FilterByType ----------

    [Fact]
    public void FilterByType_MatchOnly()
    {
        var service = CreateService();

        var result = service.FilterByType(MakeUnsortedRooms(), RoomType.Double, invert: false);

        Assert.Single(result);
        Assert.Equal(105, result[0].Number);
    }

    [Fact]
    public void FilterByType_Invert()
    {
        var service = CreateService();

        var result = service.FilterByType(MakeUnsortedRooms(), RoomType.Double, invert: true);

        Assert.Equal(2, result.Count);
    }

    // ---------- FilterByStatus ----------

    [Fact]
    public void FilterByStatus_Open()
    {
        var service = CreateService();
        var rooms = MakeUnsortedRooms();
        rooms[0].ChangeStatus(RoomStatus.Closed);

        var result = service.FilterByStatus(rooms, RoomStatus.Open, invert: false);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterByStatus_Closed()
    {
        var service = CreateService();
        var rooms = MakeUnsortedRooms();
        rooms[0].ChangeStatus(RoomStatus.Closed);

        var result = service.FilterByStatus(rooms, RoomStatus.Closed, invert: false);

        Assert.Single(result);
        Assert.Equal(105, result[0].Number);
    }

    [Fact]
    public void FilterByStatus_Invert()
    {
        var service = CreateService();
        var rooms = MakeUnsortedRooms();
        rooms[0].ChangeStatus(RoomStatus.Closed);

        var result = service.FilterByStatus(rooms, RoomStatus.Open, invert: true);

        Assert.Single(result);
    }

    // ---------- SearchByNumber ----------

    [Fact]
    public void SearchByNumber_ExactMatch()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "105");

        Assert.Single(result);
        Assert.Equal(105, result[0].Number);
    }

    [Fact]
    public void SearchByNumber_Substring()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "10");

        Assert.Equal(3, result.Count);
        Assert.Equal(105, result[0].Number);
        Assert.Equal(101, result[1].Number);
        Assert.Equal(103, result[2].Number);
    }

    [Fact]
    public void SearchByNumber_SubstringUnique_ReturnsOne()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "05");

        Assert.Single(result);
        Assert.Equal(105, result[0].Number);
    }

    [Fact]
    public void SearchByNumber_EmptyQuery_ReturnsEmpty()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "");

        Assert.Empty(result);
    }

    [Fact]
    public void SearchByNumber_WhitespaceQuery_ReturnsEmpty()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "   ");

        Assert.Empty(result);
    }

    [Fact]
    public void SearchByNumber_NoMatch_ReturnsEmpty()
    {
        var service = CreateService();

        var result = service.SearchByNumber(MakeUnsortedRooms(), "999");

        Assert.Empty(result);
    }
}