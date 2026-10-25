using System;
using HotelBooking.Domain;
using HotelBooking.Services;
using HotelBooking.UIConsole.Menus.AdminSubmenus;

namespace HotelBooking.UIConsole.Menus
{
    public class AdminMenu : MenuBase
    {
        private readonly string _userName;
        private readonly RoomService _roomService;
        private readonly RequestService _requestService;
        private readonly BookingService _bookingService;

        public AdminMenu(
                   ConsoleIO io,
                   string userName,
                   RoomService rooms,
                   RequestService requests,
                   BookingService booking) : base(io)
        {
            _userName = userName;
            _roomService = rooms;
            _requestService = requests;
            _bookingService = booking;
        }

        public override void Run()
        {
            while (true)
            {
                _io.Clear();
                _io.WriteLine("=== МЕНЮ АДМИНИСТРАТОРА ===");
                _io.WriteLine($"Администратор: {_userName}");
                _io.WriteLine("1. Необработанные заявки");
                _io.WriteLine("2. Номера гостиницы");
                _io.WriteLine("3. Управление номерами");
                _io.WriteLine("4. Все заявки");
                _io.WriteLine("5. Отменить принятую заявку");
                _io.WriteLine("0. Выйти из аккаунта");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: ManageUnprocessedRequests(); break;
                    case 2: ViewRooms(); break;
                    case 3: ManageRooms(); break;
                    case 4: ViewAllRequests(); break;
                    case 5: CancelAcceptedRequest(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void ManageUnprocessedRequests()
        {
            new AdminManageUnprocessedRequestMenu(_io, _requestService, _bookingService).Run();
        }

        private void ViewRooms()
        {
            new AdminViewRoomMenu(_io, _roomService).Run();
        }

        private void ManageRooms()
        {
            new AdminRoomManageMenu(_io, _roomService).Run();
        }

        private void ViewAllRequests()
        {
            new AdminAllRequestsMenu(_io, _requestService).Run();
        }

        private void CancelAcceptedRequest()
        {
            new AdminCancelAcceptedMenu(_io, _requestService).Run();
        }
    }
}