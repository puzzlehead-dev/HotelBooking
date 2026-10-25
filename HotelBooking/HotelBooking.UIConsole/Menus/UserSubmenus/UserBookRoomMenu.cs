using System;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.UserSubmenus
{
    public class UserBookRoomMenu : MenuBase
    {
        private readonly string _userName;
        private readonly BookingService _bookingService;

        public UserBookRoomMenu(
            ConsoleIO io,
            string userName,
            BookingService bookingService) : base(io)
        {
            _userName = userName;
            _bookingService = bookingService;
        }

        public override void Run()
        {
            _io.Clear();
            _io.WriteLine("=== БРОНИРОВАНИЕ НОМЕРА ===");

            var type = _io.ReadRoomType("Выберите тип номера:");

            var date = _io.ReadDate("Дата начала:");
            if (!_bookingService.IsDateInFuture(date))
            {
                _io.WriteLine("Ошибка: дата начала в прошлом.");
                _io.WaitEnter();
                return;
            }

            var days = _io.ReadPositiveInt("Количество дней: ");

            if (!_bookingService.IsTypeAvailable(type, date, days))
            {
                _io.WriteLine("К сожалению, нет свободных номеров этого типа на выбранный период.");
                _io.WaitEnter();
                return;
            }

            _io.WriteLine("");
            _io.WriteLine("=== СВОДКА ЗАЯВКИ ===");
            _io.WriteLine($"Пользователь: {_userName}");
            _io.WriteLine($"Тип номера: {type}");
            _io.WriteLine($"Дата начала: {date:yyyy-MM-dd}");
            _io.WriteLine($"Количество дней: {days}");
            _io.WriteLine("");

            if (!_io.Confirm("Отправить заявку?"))
            {
                _io.WriteLine("Заявка не создана.");
                _io.WaitEnter();
                return;
            }

            try
            {
                var request = _bookingService.CreateRequest(_userName, type, date, days);
                _io.WriteLine($"Заявка №{request.Id} отправлена. Ожидайте решения администратора.");
                _io.WaitEnter();
            }
            catch (InvalidOperationException ex)
            {
                _io.WriteLine($"Ошибка: {ex.Message}");
                _io.WaitEnter();
            }
            catch (ArgumentException ex)
            {
                _io.WriteLine($"Некорректные данные: {ex.Message}");
                _io.WaitEnter();
            }
        }
    }
}