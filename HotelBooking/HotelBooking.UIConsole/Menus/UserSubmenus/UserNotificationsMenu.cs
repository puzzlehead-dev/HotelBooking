using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.UserSubmenus
{
    public class UserNotificationsMenu : MenuBase
    {
        private readonly string _userName;
        private readonly NotificationService _notificationService;

        public UserNotificationsMenu(
            ConsoleIO io,
            string userName,
            NotificationService notificationService) : base(io)
        {
            _userName = userName;
            _notificationService = notificationService;
        }

        public override void Run()
        {
            var notifications = _notificationService.GetByUser(_userName);

            if (notifications.Count == 0)
            {
                _io.WriteLine("Уведомлений нет.");
                _io.WaitEnter();
                return;
            }

            while (true)
            {
                _io.Clear();
                ShowTable(notifications);

                _io.WriteLine("0. Выйти в меню");
                _io.WriteLine("1. Удалить уведомление");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1:
                        if (DeleteNotification(notifications))
                        {
                            notifications = _notificationService.GetByUser(_userName);
                        }
                        break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }

                if (notifications.Count == 0)
                {
                    _io.WriteLine("Уведомлений больше нет.");
                    _io.WaitEnter();
                    return;
                }
            }
        }

        private void ShowTable(List<Notification> notifications)
        {
            _io.WriteLine("=== ВАШИ УВЕДОМЛЕНИЯ ===");
            _io.WriteLine($"Всего: {notifications.Count}");
            _io.WriteLine("");
            _io.WriteLine("№   | Дата        | Сообщение");
            _io.WriteLine("----|-------------|------------------------------------------");

            for (int i = 0; i < notifications.Count; i++)
            {
                var number = (i + 1).ToString().PadRight(4);
                var date = notifications[i].Date.ToString("yyyy-MM-dd").PadRight(12);
                var message = notifications[i].Message;

                if (message.Length > 40)
                {
                    message = message.Substring(0, 37) + "...";
                }

                _io.WriteLine($"{number}| {date}| {message}");
            }
            _io.WriteLine("");
        }

        private bool DeleteNotification(List<Notification> notifications)
        {
            _io.Clear();
            _io.WriteLine("=== УДАЛЕНИЕ УВЕДОМЛЕНИЯ ===");
            var number = _io.ReadPositiveInt($"Номер уведомления (1-{notifications.Count}): ");

            if (number < 1 || number > notifications.Count)
            {
                _io.WriteLine("Ошибка: уведомления с таким номером нет.");
                _io.WaitEnter();
                return false;
            }

            try
            {
                _notificationService.DeleteByIndex(_userName, number - 1);
                _io.WriteLine("Уведомление удалено.");
                _io.WaitEnter();
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _io.WriteLine($"Ошибка: {ex.Message}");
                _io.WaitEnter();
                return false;
            }
        }
    }
}