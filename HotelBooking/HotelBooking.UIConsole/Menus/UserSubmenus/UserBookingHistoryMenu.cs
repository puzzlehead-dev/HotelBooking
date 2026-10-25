using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.UserSubmenus
{
    public class UserBookingHistoryMenu : MenuBase
    {
        private readonly string _userName;
        private readonly RequestService _requestService;

        public UserBookingHistoryMenu(
            ConsoleIO io,
            string userName,
            RequestService requestService) : base(io)
        {
            _userName = userName;
            _requestService = requestService;
        }

        public override void Run()
        {
            var history = _requestService.GetByStatusAndUser(RequestStatus.Completed, _userName);

            if (history.Count == 0)
            {
                _io.WriteLine("История бронирований пуста.");
                _io.WaitEnter();
                return;
            }

            _io.Clear();
            ShowTable(history);
            _io.WaitEnter();
        }

        private void ShowTable(List<Request> requests)
        {
            _io.WriteLine("=== ИСТОРИЯ БРОНИРОВАНИЙ ===");
            _io.WriteLine($"Всего: {requests.Count}");
            _io.WriteLine("");
            _io.WriteLine("Id  | Тип        | Дата        | Дней | Номер");
            _io.WriteLine("----|------------|-------------|------|------");

            foreach (var request in requests)
            {
                var id = request.Id.ToString().PadRight(4);
                var type = request.RoomType.ToString().PadRight(11);
                var date = request.DateStarting.ToString("yyyy-MM-dd").PadRight(12);
                var days = request.QuantityDays.ToString().PadRight(5);
                var number = request.RoomNumber.HasValue ? request.RoomNumber.Value.ToString() : "-";
                _io.WriteLine($"{id}| {type}| {date}| {days}| {number}");
            }
            _io.WriteLine("");
        }
    }
}