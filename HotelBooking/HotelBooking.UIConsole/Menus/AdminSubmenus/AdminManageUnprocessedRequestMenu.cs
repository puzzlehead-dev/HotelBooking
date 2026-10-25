using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.AdminSubmenus
{
    public class AdminManageUnprocessedRequestMenu : MenuBase
    {
        private readonly RequestService _requestService;
        private readonly BookingService _bookingService;

        public AdminManageUnprocessedRequestMenu(
            ConsoleIO io,
            RequestService requestService,
            BookingService bookingService) : base(io)
        {
            _requestService = requestService;
            _bookingService = bookingService;
        }

        public override void Run()
        {
            var unprocessed = _requestService.GetByStatus(RequestStatus.Unprocessed);

            if (unprocessed.Count == 0)
            {
                _io.WriteLine("Необработанных заявок нет.");
                _io.WaitEnter();
                return;
            }

            while (true)
            {
                _io.Clear();
                ShowTable(unprocessed);

                _io.WriteLine("0. Выйти в меню");
                _io.WriteLine("1. Принять заявку");
                _io.WriteLine("2. Отклонить заявку");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1:
                        if (AcceptRequest(unprocessed))
                        {
                            unprocessed = _requestService.GetByStatus(RequestStatus.Unprocessed);
                        }
                        break;
                    case 2:
                        if (RejectRequest())
                        {
                            unprocessed = _requestService.GetByStatus(RequestStatus.Unprocessed);
                        }
                        break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }

                if (unprocessed.Count == 0)
                {
                    _io.WriteLine("Необработанных заявок больше нет.");
                    _io.WaitEnter();
                    return;
                }
            }
        }

        private void ShowTable(List<Request> requests)
        {
            _io.WriteLine("=== НЕОБРАБОТАННЫЕ ЗАЯВКИ ===");
            _io.WriteLine($"Всего: {requests.Count}");
            _io.WriteLine("");
            _io.WriteLine("Id  | Пользователь    | Тип        | Дата        | Дней");
            _io.WriteLine("----|-----------------|------------|-------------|-----");

            foreach (var request in requests)
            {
                var id = request.Id.ToString().PadRight(4);
                var user = request.UserName.PadRight(16);
                var type = request.RoomType.ToString().PadRight(11);
                var date = request.DateStarting.ToString("yyyy-MM-dd").PadRight(12);
                var days = request.QuantityDays.ToString();
                _io.WriteLine($"{id}| {user}| {type}| {date}| {days}");
            }
            _io.WriteLine("");
        }

        private bool AcceptRequest(List<Request> unprocessed)
        {
            _io.Clear();
            _io.WriteLine("=== ПРИНЯТИЕ ЗАЯВКИ ===");
            var id = _io.ReadPositiveInt("Id заявки (из первой колонки): ");

            Request? target = null;
            foreach (var request in unprocessed)
            {
                if (request.Id == id)
                {
                    target = request;
                    break;
                }
            }

            if (target == null)
            {
                _io.WriteLine("Ошибка: необработанная заявка с таким Id не найдена.");
                _io.WaitEnter();
                return false;
            }

            var available = _bookingService.GetAvailableForRequest(target);
            if (available.Count == 0)
            {
                _io.WriteLine("Нет свободных номеров для этой заявки.");
                _io.WaitEnter();
                return false;
            }

            _io.WriteLine("Доступные номера: " + string.Join(", ", available));
            var roomNumber = _io.ReadPositiveInt("Номер комнаты: ");

            if (!available.Contains(roomNumber))
            {
                _io.WriteLine("Ошибка: этот номер недоступен.");
                _io.WaitEnter();
                return false;
            }

            try
            {
                _bookingService.AcceptRequest(id, roomNumber);
                _io.WriteLine("Заявка принята.");
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

        private bool RejectRequest()
        {
            _io.Clear();
            _io.WriteLine("=== ОТКЛОНЕНИЕ ЗАЯВКИ ===");
            var id = _io.ReadPositiveInt("Id заявки (из первой колонки): ");

            try
            {
                _requestService.Reject(id);
                _io.WriteLine("Заявка отклонена.");
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