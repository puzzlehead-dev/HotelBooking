using HotelBooking.Domain;
using HotelBooking.Storage;
using System;
using System.Collections.Generic;

namespace HotelBooking.Services
{
    public class NotificationService
    {
        private readonly NotificationRepository _notificationRepository;

        public NotificationService(NotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public List<Notification> GetByUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }

            var allNots = _notificationRepository.LoadAll(out _);
            var result = new List<Notification>();
            foreach (var notification in allNots)
            {
                if (notification.UserName == userName)
                {
                    result.Add(notification);
                }
            }
            return result;
        }

        public Notification Add(string userName, DateOnly date, string message)
        {
            var notification = new Notification(userName, date, message);
            var all = _notificationRepository.LoadAll(out _);
            all.Add(notification);
            _notificationRepository.SaveAll(all);
            return notification;
        }

        public void DeleteByIndex(string userName, int index)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Индекс не может быть отрицательным");
            }

            var all = _notificationRepository.LoadAll(out _);

            int userIndex = 0;
            int targetGlobalIndex = -1;

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].UserName == userName)
                {
                    if (userIndex == index)
                    {
                        targetGlobalIndex = i;
                        break;
                    }
                    userIndex++;
                }
            }

            if (targetGlobalIndex == -1)
            {
                throw new InvalidOperationException($"У пользователя '{userName}' нет уведомления с индексом {index}");
            }

            all.RemoveAt(targetGlobalIndex);
            _notificationRepository.SaveAll(all);
        }
    }
}