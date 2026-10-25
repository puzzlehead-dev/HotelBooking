using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.UserSubmenus
{
    public class UserAvailableRoomsMenu : MenuBase
    {
        private readonly BookingService _bookingService;

        public UserAvailableRoomsMenu(ConsoleIO io, BookingService bookingService) : base(io)
        {
            _bookingService = bookingService;
        }

        public override void Run()
        {
            _io.Clear();
            _io.WriteLine("=== ДОСТУПНЫЕ НОМЕРА ===");
            _io.WriteLine("Укажите период для проверки.");

            var date = _io.ReadDate("Дата начала:");
            if (!_bookingService.IsDateInFuture(date))
            {
                _io.WriteLine("Ошибка: дата начала в прошлом.");
                _io.WaitEnter();
                return;
            }
            var days = _io.ReadPositiveInt("Количество дней: ");

            _io.WriteLine("");
            _io.WriteLine($"Период: {date:yyyy-MM-dd}, {days} дн.");
            _io.WriteLine("");

            ShowAvailability(RoomType.Single, "Одноместные", date, days);
            ShowAvailability(RoomType.Double, "Двуместные", date, days);
            ShowAvailability(RoomType.Triple, "Трёхместные", date, days);
            ShowAvailability(RoomType.Quadruple, "Четырёхместные", date, days);

            _io.WriteLine("");
            _io.WaitEnter();
        }

        private void ShowAvailability(RoomType type, string displayName, DateOnly date, int days)
        {
            var available = _bookingService.GetAvailableRoomNumbers(type, date, days);

            string status;
            if (available.Count == 0)
            {
                status = "Нет свободных номеров";
            }
            else
            {
                status = $"Есть свободные номера ({available.Count})";
            }

            _io.WriteLine($"{displayName,-16}: {status}");
        }
    }
}