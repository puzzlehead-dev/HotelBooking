using HotelBooking.Domain;
using System;
using System.Collections.Generic;

namespace HotelBooking.Services
{
    public class BookingService
    {
        private readonly RoomService _roomService;
        private readonly RequestService _requestService;
        private readonly Clock _clock;

        public BookingService(RoomService roomService, RequestService requestService, Clock clock)
        {
            _roomService = roomService;
            _requestService = requestService;
            _clock = clock;
        }

        public List<int> GetAvailableRoomNumbers(RoomType type, DateOnly date, int days)
        {
            var openNumbers = _roomService.GetOpenRoomNumbers(type);
            if (openNumbers.Count == 0)
            {
                return new List<int>();
            }

            var accepted = _requestService.GetByStatus(RequestStatus.Accepted);
            var busyNumbers = new HashSet<int>();
            var requestEnd = date.AddDays(days - 1);

            foreach (var request in accepted)
            {
                if (request.RoomType != type)
                {
                    continue;
                }
                if (!request.RoomNumber.HasValue)
                {
                    continue;
                }

                var existingStart = request.DateStarting;
                var existingEnd = request.DateStarting.AddDays(request.QuantityDays - 1);

                bool overlaps = existingStart <= requestEnd && date <= existingEnd;
                if (overlaps)
                {
                    busyNumbers.Add(request.RoomNumber.Value);
                }
            }

            var available = new List<int>();
            foreach (var number in openNumbers)
            {
                if (!busyNumbers.Contains(number))
                {
                    available.Add(number);
                }
            }
            return available;
        }

        public bool IsTypeAvailable(RoomType type, DateOnly date, int days)
        {
            return GetAvailableRoomNumbers(type, date, days).Count > 0;
        }

        public Request CreateRequest(string userName, RoomType type, DateOnly date, int days)
        {
            if (date < _clock.Today)
            {
                throw new InvalidOperationException("Дата начала в прошлом");
            }
            if (days <= 0)
            {
                throw new ArgumentException("Количество дней должно быть положительным", nameof(days));
            }
            if (!IsTypeAvailable(type, date, days))
            {
                throw new InvalidOperationException("Нет свободных номеров этого типа на выбранные даты");
            }

            return _requestService.Create(userName, type, date, days);
        }

        public void AcceptRequest(int requestId, int roomNumber)
        {
            Request? target = null;
            foreach (var request in _requestService.GetAll())
            {
                if (request.Id == requestId)
                {
                    target = request;
                    break;
                }
            }
            if (target == null)
            {
                throw new InvalidOperationException($"Заявка с Id {requestId} не найдена");
            }
            if (target.Status != RequestStatus.Unprocessed)
            {
                throw new InvalidOperationException("Можно принять только необработанную заявку");
            }
            var available = GetAvailableRoomNumbers(target.RoomType, target.DateStarting, target.QuantityDays);
            bool found = false;
            foreach (var number in available)
            {
                if (number == roomNumber)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new InvalidOperationException("Этот номер недоступен на выбранные даты");
            }
            _requestService.Accept(requestId, roomNumber);
        }

        public List<int> GetAvailableForRequest(Request request)
        {
            return GetAvailableRoomNumbers(request.RoomType, request.DateStarting, request.QuantityDays);
        }

        public bool IsDateInFuture(DateOnly date)
        {
            return date >= _clock.Today;
        }
    }
}