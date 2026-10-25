using HotelBooking.Domain;
using HotelBooking.Storage;
using System;
using System.Collections.Generic;

namespace HotelBooking.Services
{
    public class RoomService
    {
        private readonly RoomRepository _roomRepository;

        public RoomService(RoomRepository roomRepository)
        {
            _roomRepository = roomRepository;
        }

        public List<Room> GetAll()
        {
            return _roomRepository.LoadAll(out _);
        }

        public Room Add(int number, RoomType type)
        {
            var rooms = _roomRepository.LoadAll(out _);

            foreach (var room in rooms)
            {
                if (room.Number == number)
                {
                    throw new InvalidOperationException($"Номер {number} уже существует");
                }
            }

            var newRoom = new Room(number, type, RoomStatus.Open);
            rooms.Add(newRoom);
            _roomRepository.SaveAll(rooms);
            return newRoom;
        }

        public void Delete(int number)
        {
            var rooms = _roomRepository.LoadAll(out _);
            int removed = rooms.RemoveAll(r => r.Number == number);

            if (removed == 0)
            {
                throw new InvalidOperationException($"Номер {number} не найден");
            }

            _roomRepository.SaveAll(rooms);
        }

        public void ChangeStatus(int number, RoomStatus status)
        {
            var rooms = _roomRepository.LoadAll(out _);
            Room? target = null;

            foreach (var room in rooms)
            {
                if (room.Number == number)
                {
                    target = room;
                    break;
                }
            }

            if (target == null)
            {
                throw new InvalidOperationException($"Номер {number} не найден");
            }

            target.ChangeStatus(status);
            _roomRepository.SaveAll(rooms);
        }

        public List<int> GetOpenRoomNumbers(RoomType type)
        {
            var rooms = _roomRepository.LoadAll(out _);
            var result = new List<int>();

            foreach (var room in rooms)
            {
                if (room.Type == type && room.Status == RoomStatus.Open)
                {
                    result.Add(room.Number);
                }
            }
            return result;
        }

        public bool HasOpenRooms(RoomType type)
        {
            return GetOpenRoomNumbers(type).Count > 0;
        }

        public List<Room> Sort(List<Room> rooms, RoomSortField field, bool ascending)
        {
            var sorted = new List<Room>(rooms);

            sorted.Sort((a, b) =>
            {
                int cmp = field == RoomSortField.Number
                    ? a.Number.CompareTo(b.Number)
                    : a.Type.CompareTo(b.Type);
                return ascending ? cmp : -cmp;
            });

            return sorted;
        }

        public List<Room> FilterByNumber(List<Room> rooms, int min, int max, bool invert)
        {
            var result = new List<Room>();
            foreach (var room in rooms)
            {
                bool inRange = room.Number >= min && room.Number <= max;
                if (inRange != invert)
                {
                    result.Add(room);
                }
            }
            return result;
        }

        public List<Room> FilterByType(List<Room> rooms, RoomType type, bool invert)
        {
            var result = new List<Room>();
            foreach (var room in rooms)
            {
                bool matches = room.Type == type;
                if (matches != invert)
                {
                    result.Add(room);
                }
            }
            return result;
        }

        public List<Room> FilterByStatus(List<Room> rooms, RoomStatus status, bool invert)
        {
            var result = new List<Room>();
            foreach (var room in rooms)
            {
                bool matches = room.Status == status;
                if (matches != invert)
                {
                    result.Add(room);
                }
            }
            return result;
        }

        public List<Room> SearchByNumber(List<Room> rooms, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<Room>();
            var result = new List<Room>();
            foreach (var room in rooms)
            {
                if (room.Number.ToString().Contains(query))
                {
                    result.Add(room);
                }
            }
            return result;
        }
    }
}