using HotelBooking.Domain;
using System;
using System.Collections.Generic;

namespace HotelBooking.Storage
{
    public class NotificationRepository
    {
        private const char Separator = '|';
        private readonly string _filePath;

        public NotificationRepository(string filePath)
        {
            _filePath = filePath;
        }

        public List<Notification> LoadAll(out int skippedCount)
        {
            skippedCount = 0;
            var notifications = new List<Notification>();
            if (!File.Exists(_filePath))
            {
                return notifications;
            }

            var lines = File.ReadAllLines(_filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!TryParse(line, out var notification))
                {
                    skippedCount++;
                    continue;
                }

                notifications.Add(notification);
            }

            return notifications;
        }

        public void SaveAll(List<Notification> notifications)
        {
            var lines = new List<string>(notifications.Count);
            foreach (var notification in notifications)
            {
                lines.Add(Format(notification));
            }
            File.WriteAllLines(_filePath, lines);
        }

        private bool TryParse(string line, out Notification notification)
        {
            notification = null!;

            var parts = line.Split(Separator, StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                return false;
            }
            var userName = parts[0];
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }
            if (!DateOnly.TryParse(parts[1], out var date))
            {
                return false;
            }
            var message = parts[2];
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }
            notification = new Notification(userName, date, message);
            return true;
        }

        private string Format(Notification notification)
        {
            return $"{notification.UserName}{Separator}{notification.Date:yyyy-MM-dd}{Separator}{notification.Message}";
        }

    }
}