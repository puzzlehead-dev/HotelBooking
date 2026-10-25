using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;
using HotelBooking.Services;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

public class BookingServiceTests : IDisposable
{
    private readonly string _roomsPath;
    private readonly string _requestsPath;
    private readonly string _notifPath;
    private readonly FakeClock _clock;

    public BookingServiceTests()
    {
        _roomsPath = Path.Combine(Path.GetTempPath(), $"rooms_{Guid.NewGuid():N}.txt");
        _requestsPath = Path.Combine(Path.GetTempPath(), $"requests_{Guid.NewGuid():N}.txt");
        _notifPath = Path.Combine(Path.GetTempPath(), $"notifications_{Guid.NewGuid():N}.txt");
        _clock = new FakeClock(new DateOnly(2026, 9, 20));
    }

    public void Dispose()
    {
        if (File.Exists(_roomsPath)) File.Delete(_roomsPath);
        if (File.Exists(_requestsPath)) File.Delete(_requestsPath);
        if (File.Exists(_notifPath)) File.Delete(_notifPath);
    }

    private BookingService CreateService(Clock? clock = null)
    {
        var actualClock = clock ?? _clock;
        var roomService = new RoomService(new RoomRepository(_roomsPath));
        var notifService = new NotificationService(new NotificationRepository(_notifPath));
        var requestService = new RequestService(new RequestRepository(_requestsPath), notifService, actualClock);
        return new BookingService(roomService, requestService, actualClock);
    }

    private void SeedRooms(params Room[] rooms)
    {
        new RoomRepository(_roomsPath).SaveAll(new List<Room>(rooms));
    }

    private void SeedRequests(params Request[] requests)
    {
        new RequestRepository(_requestsPath).SaveAll(new List<Request>(requests));
    }

    private List<Request> LoadRequestsRaw()
    {
        return new RequestRepository(_requestsPath).LoadAll(out _);
    }

    private List<Notification> LoadNotifications(string userName)
    {
        var service = new NotificationService(new NotificationRepository(_notifPath));
        return service.GetByUser(userName);
    }

    // ============================================================
    // Блок 1: GetAvailableRoomNumbers
    // ============================================================

    [Fact]
    public void GetAvailableRoomNumbers_NoOpenRooms_ReturnsEmpty()
    {
        var service = CreateService();
        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 1), 3);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableRoomNumbers_NoAcceptedRequests_ReturnsAllOpen()
    {
        SeedRooms(
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var service = CreateService();

        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Equal(2, result.Count);
        Assert.Equal(101, result[0]);
        Assert.Equal(102, result[1]);
    }

    [Fact]
    public void GetAvailableRoomNumbers_OneAcceptedRequest_ExcludesItsRoom()
    {
        SeedRooms(
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();
        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 2), 3);
        Assert.Single(result);
        Assert.Equal(102, result[0]);
    }

    [Fact]
    public void GetAvailableRoomNumbers_NonOverlappingRequest_IncludesRoom()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        // заявка занимает 10-01..10-03, новая 10-05..10-07 — нет пересечения
        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 5), 3);

        Assert.Single(result);
        Assert.Equal(101, result[0]);
    }

    [Fact]
    public void GetAvailableRoomNumbers_OverlappingRequest_ExcludesRoom()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        // заявка занимает 10-01..10-03, новая 10-02..10-04 — пересечение
        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 2), 3);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableRoomNumbers_BoundaryDates_Overlaps()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        // заявка занимает 10-01..10-03, новая начинается 10-03 — пересечение по границе
        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 3), 3);

        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableRoomNumbers_DifferentType_NotExcluded()
    {
        SeedRooms(
            new Room(101, RoomType.Double, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Double, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Single(result);
        Assert.Equal(102, result[0]);
    }

    [Fact]
    public void GetAvailableRoomNumbers_UnprocessedRequest_NotExcluded()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var unprocessed = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        // искусственно назначаем номер, чтобы убедиться: BookingService
        // фильтрует по статусу, а не по наличию номера
        unprocessed.AssignRoom(101);
        SeedRequests(unprocessed);
        var service = CreateService();

        var result = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Single(result);
        Assert.Equal(101, result[0]);
    }

    // ============================================================
    // Блок 2: IsTypeAvailable
    // ============================================================

    [Fact]
    public void IsTypeAvailable_HasFree_ReturnsTrue()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var service = CreateService();

        Assert.True(service.IsTypeAvailable(RoomType.Single, new DateOnly(2026, 10, 1), 3));
    }

    [Fact]
    public void IsTypeAvailable_AllBusy_ReturnsFalse()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        Assert.False(service.IsTypeAvailable(RoomType.Single, new DateOnly(2026, 10, 1), 3));
    }

    // ============================================================
    // Блок 3: CreateRequest
    // ============================================================

    [Fact]
    public void CreateRequest_PastDate_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        var pastDate = new DateOnly(2026, 9, 19); // clock.Today = 2026-09-20
        Assert.Throws<InvalidOperationException>(() =>
            service.CreateRequest("alice", RoomType.Single, pastDate, 3));
    }

    [Fact]
    public void CreateRequest_DaysZero_ThrowsArgumentException()
    {
        var service = CreateService();
        Assert.Throws<ArgumentException>(() =>
            service.CreateRequest("alice", RoomType.Single, new DateOnly(2026, 10, 1), 0));
    }

    [Fact]
    public void CreateRequest_NoAvailable_ThrowsInvalidOperationException()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        SeedRequests(busy);
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateRequest("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3));
    }

    [Fact]
    public void CreateRequest_Valid_SavesViaRequestService()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var service = CreateService();

        var created = service.CreateRequest("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.True(created.Id > 0);
        Assert.Equal("alice", created.UserName);
        Assert.Equal(RoomType.Single, created.RoomType);
        Assert.Equal(RequestStatus.Unprocessed, created.Status);

        var saved = LoadRequestsRaw();
        Assert.Single(saved);
        Assert.Equal(created.Id, saved[0].Id);
    }

    [Fact]
    public void CreateRequest_DateToday_Works()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var service = CreateService();

        // date == Today — валидно (проверка строго <)
        var created = service.CreateRequest("alice", RoomType.Single, _clock.Today, 3);

        Assert.NotNull(created);
        Assert.Equal(_clock.Today, created.DateStarting);
    }

    // ============================================================
    // Блок 4: AcceptRequest
    // ============================================================

    [Fact]
    public void AcceptRequest_NonExistingId_Throws()
    {
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.AcceptRequest(999, 101));
    }

    [Fact]
    public void AcceptRequest_NonUnprocessed_Throws()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var accepted = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        accepted.AssignRoom(101);
        SeedRequests(accepted);
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.AcceptRequest(1, 101));
    }

    [Fact]
    public void AcceptRequest_BusyRoom_Throws()
    {
        SeedRooms(
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var busy = new Request(1, "bob", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        busy.AssignRoom(101);
        var target = new Request(2, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(busy, target);
        var service = CreateService();
        Assert.Throws<InvalidOperationException>(() => service.AcceptRequest(2, 101));
    }

    [Fact]
    public void AcceptRequest_Valid_SetsStatusAndRoom()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var target = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(target);
        var service = CreateService();
        service.AcceptRequest(1, 101);
        var saved = LoadRequestsRaw();
        Assert.Equal(RequestStatus.Accepted, saved[0].Status);
        Assert.Equal(101, saved[0].RoomNumber);
    }

    [Fact]
    public void AcceptRequest_Valid_CreatesNotification()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Open));
        var target = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(target);
        var service = CreateService();

        service.AcceptRequest(1, 101);

        var notifications = LoadNotifications("alice");
        Assert.Single(notifications);
        Assert.Contains("101", notifications[0].Message);
        Assert.Contains("принята", notifications[0].Message);
    }

    // ============================================================
    // Блок 5: GetAvailableForRequest
    // ============================================================

    [Fact]
    public void GetAvailableForRequest_SameAsGetAvailableRoomNumbers()
    {
        SeedRooms(
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var request = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(request);
        var service = CreateService();

        var fromRequest = service.GetAvailableForRequest(request);
        var fromNumbers = service.GetAvailableRoomNumbers(RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Equal(fromNumbers, fromRequest);
    }

    [Fact]
    public void GetAvailableForRequest_WithAssignedRoom_NotInList()
    {
        // Заявка Accepted с номером 101 сама блокирует 101 — она не должна быть в списке
        SeedRooms(
            new Room(101, RoomType.Single, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var accepted = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted);
        accepted.AssignRoom(101);
        SeedRequests(accepted);
        var service = CreateService();

        var available = service.GetAvailableForRequest(accepted);

        Assert.DoesNotContain(101, available);
        Assert.Contains(102, available);
    }

    [Fact]
    public void AcceptRequest_ClosedRoom_Throws()
    {
        SeedRooms(new Room(101, RoomType.Single, RoomStatus.Closed));
        var target = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(target);
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.AcceptRequest(1, 101));
    }

    [Fact]
    public void AcceptRequest_RoomOfWrongType_Throws()
    {
        SeedRooms(
            new Room(101, RoomType.Double, RoomStatus.Open),
            new Room(102, RoomType.Single, RoomStatus.Open));
        var target = new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed);
        SeedRequests(target);
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.AcceptRequest(1, 101));
    }
}