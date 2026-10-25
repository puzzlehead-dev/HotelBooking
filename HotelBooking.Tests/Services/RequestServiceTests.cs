using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using HotelBooking.Domain;
using HotelBooking.Storage;
using HotelBooking.Services;
using HotelBooking.Tests.Fakes;

namespace HotelBooking.Tests;

public class RequestServiceTests : IDisposable
{
    private readonly string _path;
    private readonly string _notifPath;

    public RequestServiceTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"requests_{Guid.NewGuid():N}.txt");
        _notifPath = Path.Combine(Path.GetTempPath(), $"notifications_{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
        if (File.Exists(_notifPath)) File.Delete(_notifPath);
    }

    private RequestService CreateService(Clock? clock = null)
    {
        var requestRepo = new RequestRepository(_path);
        var notificationRepo = new NotificationRepository(_notifPath);
        var notificationService = new NotificationService(notificationRepo);
        var actualClock = clock ?? new FakeClock(new DateOnly(2026, 9, 20));
        return new RequestService(requestRepo, notificationService, actualClock);
    }

    private List<Request> LoadRaw()
    {
        return new RequestRepository(_path).LoadAll(out _);
    }

    private void Seed(params Request[] requests)
    {
        new RequestRepository(_path).SaveAll(new List<Request>(requests));
    }

    private List<Notification> LoadNotifications(string userName)
    {
        var repo = new NotificationRepository(_notifPath);
        var service = new NotificationService(repo);
        return service.GetByUser(userName);
    }

    // ============================================================
    // Блок 1: Create
    // ============================================================

    [Fact]
    public void Create_EmptyRepo_IdIsOne()
    {
        var service = CreateService();

        var request = service.Create("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Equal(1, request.Id);
    }

    [Fact]
    public void Create_ExistingRequests_IdIsMaxPlusOne()
    {
        Seed(
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(7, "bob", RoomType.Double, new DateOnly(2026, 10, 2), 2, RequestStatus.Unprocessed));
        var service = CreateService();

        var request = service.Create("carol", RoomType.Triple, new DateOnly(2026, 10, 3), 5);

        Assert.Equal(8, request.Id);
    }

    [Fact]
    public void Create_StatusIsUnprocessed()
    {
        var service = CreateService();

        var request = service.Create("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3);

        Assert.Equal(RequestStatus.Unprocessed, request.Status);
    }

    [Fact]
    public void Create_SavesToRepository()
    {
        var service = CreateService();

        service.Create("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3);

        var saved = LoadRaw();
        Assert.Single(saved);
        Assert.Equal("alice", saved[0].UserName);
        Assert.Equal(RoomType.Single, saved[0].RoomType);
    }

    // ============================================================
    // Блок 2: GetAll / GetByUser / GetByStatus / GetByStatusAndUser
    // ============================================================

    [Fact]
    public void GetAll_EmptyRepo_ReturnsEmpty()
    {
        var service = CreateService();

        var all = service.GetAll();

        Assert.Empty(all);
    }

    [Fact]
    public void GetByUser_FiltersCorrectly()
    {
        Seed(
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob", RoomType.Double, new DateOnly(2026, 10, 2), 2, RequestStatus.Unprocessed),
            new Request(3, "alice", RoomType.Triple, new DateOnly(2026, 10, 3), 5, RequestStatus.Accepted));
        var service = CreateService();

        var result = service.GetByUser("alice");

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(3, result[1].Id);
    }

    [Fact]
    public void GetByStatus_FiltersCorrectly()
    {
        Seed(
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob", RoomType.Double, new DateOnly(2026, 10, 2), 2, RequestStatus.Accepted),
            new Request(3, "carol", RoomType.Triple, new DateOnly(2026, 10, 3), 5, RequestStatus.Accepted));
        var service = CreateService();

        var result = service.GetByStatus(RequestStatus.Accepted);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(3, result[1].Id);
    }

    [Fact]
    public void GetByStatusAndUser_FiltersBoth()
    {
        Seed(
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "alice", RoomType.Double, new DateOnly(2026, 10, 2), 2, RequestStatus.Accepted),
            new Request(3, "bob", RoomType.Triple, new DateOnly(2026, 10, 3), 5, RequestStatus.Accepted));
        var service = CreateService();

        var result = service.GetByStatusAndUser(RequestStatus.Accepted, "alice");

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void GetByUser_EmptyName_ThrowsArgumentException()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetByUser(""));
    }

    // ============================================================
    // Блок 3: Accept / Reject
    // ============================================================

    [Fact]
    public void Accept_UnprocessedRequest_ChangesToAccepted()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        service.Accept(1, 101);

        Assert.Equal(RequestStatus.Accepted, LoadRaw()[0].Status);
    }

    [Fact]
    public void Accept_AssignsRoomNumber()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        service.Accept(1, 101);

        Assert.Equal(101, LoadRaw()[0].RoomNumber);
    }

    [Fact]
    public void Accept_CreatesNotification()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        service.Accept(1, 101);

        var notifications = LoadNotifications("alice");
        Assert.Single(notifications);
        Assert.Contains("101", notifications[0].Message);
        Assert.Contains("принята", notifications[0].Message);
    }

    [Fact]
    public void Accept_NonUnprocessedRequest_Throws()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted));
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.Accept(1, 101));
    }

    [Fact]
    public void Reject_UnprocessedRequest_ChangesToRejected()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        service.Reject(1);

        Assert.Equal(RequestStatus.Rejected, LoadRaw()[0].Status);
    }

    // ============================================================
    // Блок 4: CancelByAdmin / CancelByUser
    // ============================================================

    [Fact]
    public void CancelByAdmin_AcceptedRequest_ChangesToRejected()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted));
        var service = CreateService();

        service.CancelByAdmin(1);

        Assert.Equal(RequestStatus.Rejected, LoadRaw()[0].Status);
    }

    [Fact]
    public void CancelByAdmin_UnprocessedRequest_Throws()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.CancelByAdmin(1));
    }

    [Fact]
    public void CancelByUser_UnprocessedRequest_Works()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed));
        var service = CreateService();

        service.CancelByUser(1);

        Assert.Equal(RequestStatus.Rejected, LoadRaw()[0].Status);
    }

    [Fact]
    public void CancelByUser_CompletedRequest_Throws()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Completed));
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.CancelByUser(1));
    }

    // ============================================================
    // Блок 5: CompleteOverdue / RejectOverdue
    // ============================================================

    [Fact]
    public void CompleteOverdue_NoOverdue_ReturnsZero()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.CompleteOverdue();

        Assert.Equal(0, count);
    }

    [Fact]
    public void CompleteOverdue_EndingYesterday_Completed()
    {
        // today = 2026-09-20, заявка 16.09 на 3 дня → конец 18.09, это < today
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 16), 3, RequestStatus.Accepted));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.CompleteOverdue();

        Assert.Equal(1, count);
        Assert.Equal(RequestStatus.Completed, LoadRaw()[0].Status);
    }

    [Fact]
    public void CompleteOverdue_EndingToday_NotCompleted()
    {
        // today = 2026-09-20, заявка 18.09 на 3 дня → конец 20.09, это == today, не < today
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 18), 3, RequestStatus.Accepted));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.CompleteOverdue();

        Assert.Equal(0, count);
        Assert.Equal(RequestStatus.Accepted, LoadRaw()[0].Status);
    }

    [Fact]
    public void CompleteOverdue_CreatesNotification()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 16), 3, RequestStatus.Accepted));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        service.CompleteOverdue();

        var notifications = LoadNotifications("alice");
        Assert.Single(notifications);
    }

    [Fact]
    public void RejectOverdue_EndingYesterday_Rejected()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 16), 3, RequestStatus.Unprocessed));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.RejectOverdue();

        Assert.Equal(1, count);
        Assert.Equal(RequestStatus.Rejected, LoadRaw()[0].Status);
    }

    [Fact]
    public void RejectOverdue_EndingToday_NotRejected()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 18), 3, RequestStatus.Unprocessed));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.RejectOverdue();

        Assert.Equal(0, count);
        Assert.Equal(RequestStatus.Unprocessed, LoadRaw()[0].Status);
    }

    // ============================================================
    // Блок 6: Sort / Filter / Search
    // ============================================================

    [Fact]
    public void Sort_ByUserNameAscending()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "carol", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "alice", RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "bob",   RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var sorted = service.Sort(input, RequestSortField.UserName, ascending: true);

        Assert.Equal("alice", sorted[0].UserName);
        Assert.Equal("bob", sorted[1].UserName);
        Assert.Equal("carol", sorted[2].UserName);
    }

    [Fact]
    public void Sort_ByDateStartingDescending()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "alice", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
            new Request(3, "alice", RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
        };

        var sorted = service.Sort(input, RequestSortField.DateStarting, ascending: false);

        Assert.Equal(new DateOnly(2026, 10, 3), sorted[0].DateStarting);
        Assert.Equal(new DateOnly(2026, 10, 2), sorted[1].DateStarting);
        Assert.Equal(new DateOnly(2026, 10, 1), sorted[2].DateStarting);
    }

    [Fact]
    public void Accept_NonExistingId_Throws()
    {
        var service = CreateService();
        Assert.Throws<InvalidOperationException>(() => service.Accept(999, 101));
    }

    [Fact]
    public void Reject_AcceptedRequest_ThrowsAndStatusUnchanged()
    {
        var service = CreateService();
        var created = service.Create("alice", RoomType.Single, new DateOnly(2026, 10, 1), 3);
        service.Accept(created.Id, 101);

        Assert.Throws<InvalidOperationException>(() => service.Reject(created.Id));

        var all = LoadRaw();
        Assert.Equal(RequestStatus.Accepted, all[0].Status);
    }

    [Fact]
    public void CancelByUser_AcceptedRequest_Works()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted));
        var service = CreateService();

        service.CancelByUser(1);

        Assert.Equal(RequestStatus.Rejected, LoadRaw()[0].Status);
    }

    [Fact]
    public void CompleteOverdue_MultipleOverdue_CompletesAll()
    {
        Seed(
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 10), 3, RequestStatus.Accepted),
            new Request(2, "bob", RoomType.Double, new DateOnly(2026, 9, 12), 3, RequestStatus.Accepted),
            new Request(3, "carol", RoomType.Triple, new DateOnly(2026, 9, 19), 3, RequestStatus.Accepted));
        // today = 20, конец 21 → ещё активна
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.CompleteOverdue();

        var all = LoadRaw();
        Assert.Equal(2, count);
        Assert.Equal(RequestStatus.Completed, all.Find(r => r.Id == 1)!.Status);
        Assert.Equal(RequestStatus.Completed, all.Find(r => r.Id == 2)!.Status);
        Assert.Equal(RequestStatus.Accepted, all.Find(r => r.Id == 3)!.Status);
    }

    [Fact]
    public void CompleteOverdue_UnprocessedRequest_NotTouched()
    {
        Seed(new Request(1, "alice", RoomType.Single, new DateOnly(2026, 9, 10), 3, RequestStatus.Unprocessed));
        var service = CreateService(new FakeClock(new DateOnly(2026, 9, 20)));

        int count = service.CompleteOverdue();

        Assert.Equal(0, count);
        Assert.Equal(RequestStatus.Unprocessed, LoadRaw()[0].Status);
    }

    // ============================================================
    // Блок 6: Filter* — invert: false
    // ============================================================

    [Fact]
    public void FilterByUserName_Match()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "alice", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByUserName(input, "alice", invert: false);

        Assert.Equal(2, filtered.Count);
        foreach (var request in filtered)
        {
            Assert.Equal("alice", request.UserName);
        }
        foreach (var request in filtered)
        {
            Assert.NotEqual("bob", request.UserName);
        }
    }

    [Fact]
    public void FilterByRoomType_Match()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Double, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Double, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByRoomType(input, RoomType.Double, invert: false);

        Assert.Equal(2, filtered.Count);
        foreach (var request in filtered)
        {
            Assert.Equal(RoomType.Double, request.RoomType);
        }
        foreach (var request in filtered)
        {
            Assert.NotEqual(RoomType.Single, request.RoomType);
        }
    }

    [Fact]
    public void FilterByQuantityDays_Range()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 2, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 5, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 7, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByQuantityDays(input, 3, 6, invert: false);

        Assert.Single(filtered);
        foreach (var request in filtered)
        {
            Assert.InRange(request.QuantityDays, 3, 6);
        }
        foreach (var request in filtered)
        {
            Assert.Equal(2, request.Id);
        }
    }

    [Fact]
    public void FilterByRoomNumber_UnassignedNotMatched()
    {
        // Важно: заявка с RoomNumber == null не попадает в диапазон >= min && <= max.
        // Даже если min == 0, HasValue == false отсекает её.
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
        };
        input[1].AssignRoom(101);

        var filtered = service.FilterByRoomNumber(input, 0, 200, invert: false);

        Assert.Single(filtered);
        Assert.Equal(2, filtered[0].Id);
        Assert.NotNull(filtered[0].RoomNumber);
        Assert.InRange(filtered[0].RoomNumber!.Value, 0, 200);
    }

    [Fact]
    public void FilterByStatus_AcceptedOnly()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Accepted),
        };

        var filtered = service.FilterByStatus(input, RequestStatus.Accepted, invert: false);

        Assert.Equal(2, filtered.Count);
        foreach (var request in filtered)
        {
            Assert.Equal(RequestStatus.Accepted, request.Status);
        }
        foreach (var request in filtered)
        {
            Assert.NotEqual(RequestStatus.Unprocessed, request.Status);
        }
    }

    [Fact]
    public void FilterByDateRange_Works()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 5), 3, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 10), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByDateRange(input, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 7), invert: false);

        Assert.Single(filtered);
        foreach (var request in filtered)
        {
            Assert.InRange(request.DateStarting, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 7));
        }
        Assert.Equal(2, filtered[0].Id);
    }

    // ============================================================
    // Блок 6: Filter* — invert: true
    // ============================================================

    [Fact]
    public void FilterByUserName_Invert()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "alice", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByUserName(input, "alice", invert: true);

        Assert.Single(filtered);
        foreach (var request in filtered)
        {
            Assert.NotEqual("alice", request.UserName);
        }
        Assert.Equal("bob", filtered[0].UserName);
    }

    [Fact]
    public void FilterByRoomType_Invert()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Double, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Double, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByRoomType(input, RoomType.Double, invert: true);

        Assert.Single(filtered);
        foreach (var request in filtered)
        {
            Assert.NotEqual(RoomType.Double, request.RoomType);
        }
        Assert.Equal(RoomType.Single, filtered[0].RoomType);
    }

    [Fact]
    public void FilterByQuantityDays_Invert()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 2, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 5, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 7, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByQuantityDays(input, 3, 6, invert: true);

        Assert.Equal(2, filtered.Count);
        foreach (var request in filtered)
        {
            Assert.False(request.QuantityDays >= 3 && request.QuantityDays <= 6);
        }
        Assert.Equal(1, filtered[0].Id);
        Assert.Equal(3, filtered[1].Id);
    }

    [Fact]
    public void FilterByRoomNumber_Invert_IncludesUnassigned()
    {
        // С invert: true заявки без RoomNumber попадают в результат,
        // потому что matches == false, а false != true.
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
        };
        input[1].AssignRoom(101);

        var filtered = service.FilterByRoomNumber(input, 0, 200, invert: true);

        Assert.Single(filtered);
        Assert.Equal(1, filtered[0].Id);
        Assert.False(filtered[0].RoomNumber.HasValue);
    }

    [Fact]
    public void FilterByStatus_Invert()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Accepted),
        };

        var filtered = service.FilterByStatus(input, RequestStatus.Accepted, invert: true);

        Assert.Single(filtered);
        foreach (var request in filtered)
        {
            Assert.NotEqual(RequestStatus.Accepted, request.Status);
        }
        Assert.Equal(RequestStatus.Unprocessed, filtered[0].Status);
    }

    [Fact]
    public void FilterByDateRange_Invert()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 5), 3, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 10), 3, RequestStatus.Unprocessed),
        };

        var filtered = service.FilterByDateRange(input, new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 7), invert: true);

        Assert.Equal(2, filtered.Count);
        foreach (var request in filtered)
        {
            Assert.False(request.DateStarting >= new DateOnly(2026, 10, 2) && request.DateStarting <= new DateOnly(2026, 10, 7));
        }
        Assert.Equal(1, filtered[0].Id);
        Assert.Equal(3, filtered[1].Id);
    }

    // ============================================================
    // Блок 6: Search*
    // ============================================================

    [Fact]
    public void SearchByUserName_Substring()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Unprocessed),
            new Request(3, "alina", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Unprocessed),
        };

        var result = service.SearchByUserName(input, "ali");

        Assert.Equal(2, result.Count);
        foreach (var request in result)
        {
            Assert.Contains("ali", request.UserName);
        }
        Assert.Equal(1, result[0].Id);
        Assert.Equal(3, result[1].Id);
    }

    [Fact]
    public void SearchByDate_Substring()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 11, 5), 3, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 15), 3, RequestStatus.Unprocessed),
        };

        // "2026-10" встречается в дате 2026-10-01 и 2026-10-15, но не в 2026-11-05
        var result = service.SearchByDate(input, "2026-10");

        Assert.Equal(2, result.Count);
        foreach (var request in result)
        {
            Assert.Contains("2026-10", request.DateStarting.ToString("yyyy-MM-dd"));
        }
        Assert.Equal(1, result[0].Id);
        Assert.Equal(3, result[1].Id);
    }

    [Fact]
    public void SearchByQuantityDays_Substring()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 13, RequestStatus.Unprocessed),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 5, RequestStatus.Unprocessed),
        };

        // "3" встречается в "3" и "13", но не в "5"
        var result = service.SearchByQuantityDays(input, "3");

        Assert.Equal(2, result.Count);
        foreach (var request in result)
        {
            Assert.Contains("3", request.QuantityDays.ToString());
        }
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void SearchByRoomNumber_UnassignedWithDash()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Unprocessed),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
        };
        input[1].AssignRoom(101);

        var result = service.SearchByRoomNumber(input, "-");

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.False(result[0].RoomNumber.HasValue);
    }

    [Fact]
    public void SearchByRoomNumber_Substring()
    {
        var service = CreateService();
        var input = new List<Request>
        {
            new Request(1, "alice", RoomType.Single, new DateOnly(2026, 10, 1), 3, RequestStatus.Accepted),
            new Request(2, "bob",   RoomType.Single, new DateOnly(2026, 10, 2), 3, RequestStatus.Accepted),
            new Request(3, "carol", RoomType.Single, new DateOnly(2026, 10, 3), 3, RequestStatus.Accepted),
        };
        input[0].AssignRoom(101);
        input[1].AssignRoom(211);
        input[2].AssignRoom(102);

        var result = service.SearchByRoomNumber(input, "10");

        Assert.Equal(2, result.Count);
        foreach (var request in result)
        {
            Assert.True(request.RoomNumber.HasValue);
            Assert.Contains("10", request.RoomNumber.Value.ToString());
        }
        Assert.Equal(1, result[0].Id);
        Assert.Equal(3, result[1].Id);
    }
}