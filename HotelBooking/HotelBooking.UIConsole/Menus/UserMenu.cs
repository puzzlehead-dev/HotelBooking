using HotelBooking.Domain;
using HotelBooking.Services;
using HotelBooking.UIConsole.Menus.UserSubmenus;

namespace HotelBooking.UIConsole.Menus
{
    public class UserMenu : MenuBase
    {
        private readonly string _userName;
        private readonly RoomService _roomService;
        private readonly RequestService _requestService;
        private readonly BookingService _bookingService;
        private readonly NotificationService _notificationService;

        public UserMenu(
            ConsoleIO io,
            string userName,
            RoomService roomService,
            RequestService requestService,
            BookingService bookingService,
            NotificationService notificationService) : base(io)
        {
            _userName = userName;
            _roomService = roomService;
            _requestService = requestService;
            _bookingService = bookingService;
            _notificationService = notificationService;
        }

        public override void Run()
        {
            while (true)
            {
                _io.Clear();
                _io.WriteLine("=== МЕНЮ ПОЛЬЗОВАТЕЛЯ ===");
                _io.WriteLine($"Пользователь: {_userName}");
                _io.WriteLine("1. Забронировать номер");
                _io.WriteLine("2. Отменить бронирование");
                _io.WriteLine("3. Доступные номера");
                _io.WriteLine("4. Уведомления");
                _io.WriteLine("5. История бронирований");
                _io.WriteLine("6. Информация об аккаунте");
                _io.WriteLine("0. Выйти из аккаунта");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: BookRoom(); break;
                    case 2: CancelRequest(); break;
                    case 3: ViewAvailableRooms(); break;
                    case 4: ViewNotifications(); break;
                    case 5: ViewBookingHistory(); break;
                    case 6: ViewAccountInfo(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void BookRoom()
        {
            new UserBookRoomMenu(_io, _userName, _bookingService).Run();
        }

        private void CancelRequest()
        {
            new UserCancelRequestMenu(_io, _userName, _requestService).Run();
        }

        private void ViewAvailableRooms()
        {
            new UserAvailableRoomsMenu(_io, _bookingService).Run();
        }

        private void ViewNotifications()
        {
            new UserNotificationsMenu(_io, _userName, _notificationService).Run();
        }

        private void ViewBookingHistory()
        {
            new UserBookingHistoryMenu(_io, _userName, _requestService).Run();
        }

        private void ViewAccountInfo()
        {
            new UserAccountInfoMenu(_io, _userName, _requestService).Run();
        }
    }
}