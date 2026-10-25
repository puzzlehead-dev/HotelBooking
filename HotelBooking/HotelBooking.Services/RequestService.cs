using HotelBooking.Domain;
using HotelBooking.Storage;
using System;
using System.Collections.Generic;

namespace HotelBooking.Services
{
    public class RequestService
    {
        private readonly RequestRepository _requestRepository;
        private readonly NotificationService _notificationService;
        private readonly Clock _clock;

        public RequestService(RequestRepository requestRepository, NotificationService notificationService, Clock clock)
        {
            _requestRepository = requestRepository;
            _notificationService = notificationService;
            _clock = clock;
        }

        public List<Request> GetAll()
        {
            return _requestRepository.LoadAll(out _);
        }

        public List<Request> GetByUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }

            var all = _requestRepository.LoadAll(out _);
            var result = new List<Request>();
            foreach (var request in all)
            {
                if (request.UserName == userName)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> GetByStatus(RequestStatus status)
        {
            var all = _requestRepository.LoadAll(out _);
            var result = new List<Request>();
            foreach (var request in all)
            {
                if (request.Status == status)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> GetByStatusAndUser(RequestStatus status, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }

            var all = _requestRepository.LoadAll(out _);
            var result = new List<Request>();
            foreach (var request in all)
            {
                if (request.Status == status && request.UserName == userName)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public Request Create(string userName, RoomType type, DateOnly date, int days)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }
            if (days <= 0)
            {
                throw new ArgumentException("Количество дней должно быть положительным", nameof(days));
            }
            var all = _requestRepository.LoadAll(out _);
            int maxId = 0;
            foreach (var existing in all)
            {
                if (existing.Id > maxId)
                {
                    maxId = existing.Id;
                }
            }

            var request = new Request(maxId + 1, userName, type, date, days, RequestStatus.Unprocessed);
            all.Add(request);
            _requestRepository.SaveAll(all);
            return request;
        }

        public void Accept(int requestId, int roomNumber)
        {
            var all = _requestRepository.LoadAll(out _);
            var target = FindById(all, requestId);
            if (target.Status != RequestStatus.Unprocessed)
            {
                throw new InvalidOperationException("Принять можно только необработанную заявку");
            }
            target.ChangeStatus(RequestStatus.Accepted);
            target.AssignRoom(roomNumber);

            _notificationService.Add(
                target.UserName,
                _clock.Today,
                $"Ваша заявка принята. Номер: {roomNumber}");

            _requestRepository.SaveAll(all);
        }

        public void Reject(int requestId)
        {
            var all = _requestRepository.LoadAll(out _);
            var target = FindById(all, requestId);
            if (target.Status != RequestStatus.Unprocessed)
            {
                throw new InvalidOperationException("Отклонить можно только необработанную заявку");
            }
            target.ChangeStatus(RequestStatus.Rejected);
            _notificationService.Add(target.UserName, _clock.Today, "Ваша заявка отклонена");
            _requestRepository.SaveAll(all);
        }

        public void CancelByAdmin(int requestId)
        {
            var all = _requestRepository.LoadAll(out _);
            var target = FindById(all, requestId);

            if (target.Status != RequestStatus.Accepted)
            {
                throw new InvalidOperationException("Отменить можно только принятую заявку");
            }
            target.ChangeStatus(RequestStatus.Rejected);
            _notificationService.Add(target.UserName, _clock.Today, "Ваша заявка отменена администратором");
            _requestRepository.SaveAll(all);
        }

        public void CancelByUser(int requestId)
        {
            var all = _requestRepository.LoadAll(out _);
            var target = FindById(all, requestId);

            if (target.Status != RequestStatus.Unprocessed && target.Status != RequestStatus.Accepted)
            {
                throw new InvalidOperationException("Отозвать можно только необработанную или принятую заявку");
            }

            target.ChangeStatus(RequestStatus.Rejected);

            _notificationService.Add(target.UserName, _clock.Today, "Заявка отозвана");

            _requestRepository.SaveAll(all);
        }

        public int CompleteOverdue()
        {
            var all = _requestRepository.LoadAll(out _);
            int count = 0;

            foreach (var request in all)
            {
                if (request.Status != RequestStatus.Accepted)
                {
                    continue;
                }
                var endDate = request.DateStarting.AddDays(request.QuantityDays - 1);
                if (endDate < _clock.Today)
                {
                    request.ChangeStatus(RequestStatus.Completed);
                    _notificationService.Add(request.UserName, _clock.Today, "Бронь завершена");
                    count++;
                }
            }
            if (count > 0)
            {
                _requestRepository.SaveAll(all);
            }
            return count;
        }

        public int RejectOverdue()
        {
            var all = _requestRepository.LoadAll(out _);
            int count = 0;

            foreach (var request in all)
            {
                if (request.Status != RequestStatus.Unprocessed)
                {
                    continue;
                }
                var endDate = request.DateStarting.AddDays(request.QuantityDays - 1);
                if (endDate < _clock.Today)
                {
                    request.ChangeStatus(RequestStatus.Rejected);
                    _notificationService.Add(request.UserName, _clock.Today, "Заявка отозвана из-за истекшего срока");
                    count++;
                }
            }
            if (count > 0)
            {
                _requestRepository.SaveAll(all);
            }
            return count;
        }

        public List<Request> Sort(List<Request> requests, RequestSortField field, bool ascending)
        {
            var sorted = new List<Request>(requests);
            sorted.Sort((a, b) =>
            {
                int cmp;
                if (field == RequestSortField.UserName)
                {
                    cmp = string.Compare(a.UserName, b.UserName, StringComparison.Ordinal);
                }
                else if (field == RequestSortField.DateStarting)
                {
                    cmp = a.DateStarting.CompareTo(b.DateStarting);
                }
                else if (field == RequestSortField.QuantityDays)
                {
                    cmp = a.QuantityDays.CompareTo(b.QuantityDays);
                }
                else
                {
                    int an = a.RoomNumber ?? int.MaxValue;
                    int bn = b.RoomNumber ?? int.MaxValue;
                    cmp = an.CompareTo(bn);
                }
                return ascending ? cmp : -cmp;
            });
            return sorted;
        }

        public List<Request> FilterByUserName(List<Request> requests, string userName, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.UserName == userName;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> FilterByRoomType(List<Request> requests, RoomType type, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.RoomType == type;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> FilterByDateRange(List<Request> requests, DateOnly from, DateOnly to, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.DateStarting >= from && request.DateStarting <= to;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> FilterByQuantityDays(List<Request> requests, int min, int max, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.QuantityDays >= min && request.QuantityDays <= max;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> FilterByStatus(List<Request> requests, RequestStatus status, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.Status == status;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> FilterByRoomNumber(List<Request> requests, int min, int max, bool invert)
        {
            var result = new List<Request>();
            foreach (var request in requests)
            {
                bool matches = request.RoomNumber.HasValue
                    && request.RoomNumber.Value >= min
                    && request.RoomNumber.Value <= max;
                if (matches != invert)
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> SearchByUserName(List<Request> requests, string query)
        {
            var result = new List<Request>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            foreach (var request in requests)
            {
                if (request.UserName.Contains(query))
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> SearchByDate(List<Request> requests, string query)
        {
            var result = new List<Request>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            foreach (var request in requests)
            {
                var text = request.DateStarting.ToString("yyyy-MM-dd");
                if (text.Contains(query))
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> SearchByQuantityDays(List<Request> requests, string query)
        {
            var result = new List<Request>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            foreach (var request in requests)
            {
                var text = request.QuantityDays.ToString();
                if (text.Contains(query))
                {
                    result.Add(request);
                }
            }
            return result;
        }

        public List<Request> SearchByRoomNumber(List<Request> requests, string query)
        {
            var result = new List<Request>();
            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            bool searchUnassigned = query.Contains("-");

            foreach (var request in requests)
            {
                if (!request.RoomNumber.HasValue)
                {
                    if (searchUnassigned)
                    {
                        result.Add(request);
                    }
                    continue;
                }

                var text = request.RoomNumber.Value.ToString();
                if (text.Contains(query))
                {
                    result.Add(request);
                }
            }
            return result;
        }

        private static Request FindById(List<Request> requests, int requestId)
        {
            foreach (var request in requests)
            {
                if (request.Id == requestId)
                {
                    return request;
                }
            }
            throw new InvalidOperationException($"Заявка с Id {requestId} не найдена");
        }
    }
}