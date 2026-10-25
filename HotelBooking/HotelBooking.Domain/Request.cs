using System;

namespace HotelBooking.Domain
{
    public class Request
    {
        private int _id;
        private string _userName;
        private RoomType _roomType;
        private DateOnly _dateStarting;
        private int _quantityDays;
        private RequestStatus _status;
        private int? _roomNumber;

        public Request(int id, string userName, RoomType roomType, DateOnly dateStarting, int quantityDays, RequestStatus status)
        {
            if (id <= 0)
            {
                throw new ArgumentException("Id должен быть положительным", nameof(id));
            }
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }
            if (quantityDays <= 0)
            {
                throw new ArgumentException("Количество дней должно быть положительным", nameof(quantityDays));
            }

            _id = id;
            _userName = userName;
            _roomType = roomType;
            _dateStarting = dateStarting;
            _quantityDays = quantityDays;
            _status = status;
            _roomNumber = null;
        }

        public int Id { get => _id; }
        public string UserName { get => _userName; }
        public RoomType RoomType { get => _roomType; }
        public DateOnly DateStarting { get => _dateStarting; }
        public int QuantityDays { get => _quantityDays; }
        public RequestStatus Status { get => _status; }
        public int? RoomNumber { get => _roomNumber; }

        public void ChangeStatus(RequestStatus status)
        {
            _status = status;
        }

        public void AssignRoom(int roomNumber)
        {
            if (roomNumber <= 0)
            {
                throw new ArgumentException("Номер комнаты должен быть положительным", nameof(roomNumber));
            }
            _roomNumber = roomNumber;
        }
    }
}
