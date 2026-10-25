using HotelBooking.Domain;
using System;
using System.Collections.Generic;
using System.IO;

namespace HotelBooking.Storage
{
    public class RoomRepository
    {
        private readonly string _filePath;

        public RoomRepository(string filePath)
        {
            _filePath = filePath;
        }

        public List<Room> LoadAll(out int skippedCount)
        {
            skippedCount = 0;
            var rooms = new List<Room>();
            if (!File.Exists(_filePath))
            {
                return rooms;
            }
            var seenNumbers = new HashSet<int>();
            var lines = File.ReadAllLines(_filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (!TryParse(line, out var room))
                {
                    skippedCount++;
                    continue;
                }
                if (!seenNumbers.Add(room.Number))
                {
                    skippedCount++;
                    continue;
                }
                rooms.Add(room);
            }

            return rooms;
        }

        public void SaveAll(List<Room> rooms)
        {
            var lines = new List<string>(rooms.Count);
            foreach (var room in rooms)
            {
                lines.Add(Format(room));
            }
            File.WriteAllLines(_filePath, lines);
        }

        private bool TryParse(string line, out Room room)
        {
            room = null!;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                return false;
            }
            if (!int.TryParse(parts[0], out var number))
            {
                return false;
            }
            if (!Enum.TryParse<RoomType>(parts[1], out var type))
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(RoomType), type))
            {
                return false;
            }
            if (!Enum.TryParse<RoomStatus>(parts[2], out var status))
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(RoomStatus), status))
            {
                return false;
            }
            try
            {
                room = new Room(number, type, status);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return true;
        }
         
        private string Format(Room room)
        {
            return $"{room.Number} {room.Type} {room.Status}";
        }
    }
}