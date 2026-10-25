using HotelBooking.Domain;
using System;
using System.Collections.Generic;
using System.IO;

namespace HotelBooking.Storage
{
    public class RequestRepository
    {
        private const char Separator = '|';
        private readonly string _filePath;

        public RequestRepository(string filePath)
        {
            _filePath = filePath;
        }

        public List<Request> LoadAll(out int skippedCount)
        {
            skippedCount = 0;
            var requests = new List<Request>();
            if (!File.Exists(_filePath))
            {
                return requests;
            }

            var seenIds = new HashSet<int>();
            var lines = File.ReadAllLines(_filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (!TryParse(line, out var request))
                {
                    skippedCount++;
                    continue;
                }
                if (!seenIds.Add(request.Id))
                {
                    skippedCount++;
                    continue;
                }
                requests.Add(request);
            }
            return requests;
        }

        public void SaveAll(List<Request> requests)
        {
            var lines = new List<string>(requests.Count);
            foreach (var request in requests)
            {
                lines.Add(Format(request));
            }
            File.WriteAllLines(_filePath, lines);
        }

        private bool TryParse(string line, out Request request)
        {
            request = null!;
            var parts = line.Split(Separator, StringSplitOptions.TrimEntries);
            if (parts.Length != 7)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out var id))
            {
                return false;
            }
            if (id <= 0)
            {
                return false;
            }

            var userName = parts[1];
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }

            if (!Enum.TryParse<RoomType>(parts[2], out var roomType))
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(RoomType), roomType))
            {
                return false;
            }

            if (!DateOnly.TryParse(parts[3], out var dateStarting))
            {
                return false;
            }

            if (!int.TryParse(parts[4], out var quantityDays))
            {
                return false;
            }
            if (quantityDays <= 0)
            {
                return false;
            }

            if (!Enum.TryParse<RequestStatus>(parts[5], out var status))
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(RequestStatus), status))
            {
                return false;
            }

            int? roomNumber = null;
            if (parts[6] != "-")
            {
                if (!int.TryParse(parts[6], out var parsedRoomNumber))
                {
                    return false;
                }
                if (parsedRoomNumber <= 0)
                {
                    return false;
                }
                roomNumber = parsedRoomNumber;
            }

            try
            {
                request = new Request(id, userName, roomType, dateStarting, quantityDays, status);
                if (roomNumber.HasValue)
                {
                    request.AssignRoom(roomNumber.Value);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private string Format(Request request)
        {
            var roomNumber = request.RoomNumber.HasValue
                ? request.RoomNumber.Value.ToString()
                : "-";
            return $"{request.Id}{Separator}{request.UserName}{Separator}{request.RoomType}{Separator}{request.DateStarting:yyyy-MM-dd}{Separator}{request.QuantityDays}{Separator}{request.Status}{Separator}{roomNumber}";
        }
    }
}