using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.AdminSubmenus
{
    public class AdminCancelAcceptedMenu : MenuBase
    {
        private readonly RequestService _requestService;

        public AdminCancelAcceptedMenu(ConsoleIO io, RequestService requestService) : base(io)
        {
            _requestService = requestService;
        }

        public override void Run()
        {
            var accepted = _requestService.GetByStatus(RequestStatus.Accepted);

            if (accepted.Count == 0)
            {
                _io.WriteLine("Принятых заявок нет.");
                _io.WaitEnter();
                return;
            }

            while (true)
            {
                _io.Clear();
                ShowTable(accepted);

                _io.WriteLine("0. Выйти в меню");
                _io.WriteLine("1. Отменить заявку");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1:
                        if (CancelRequest())
                        {
                            accepted = _requestService.GetByStatus(RequestStatus.Accepted);
                        }
                        break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }

                if (accepted.Count == 0)
                {
                    _io.WriteLine("Принятых заявок больше нет.");
                    _io.WaitEnter();
                    return;
                }
            }
        }

        private void ShowTable(List<Request> requests)
        {
            _io.WriteLine("=== ПРИНЯТЫЕ ЗАЯВКИ ===");
            _io.WriteLine($"Всего: {requests.Count}");
            _io.WriteLine("");
            _io.WriteLine("Id  | Пользователь    | Тип        | Дата        | Дней | Номер");
            _io.WriteLine("----|-----------------|------------|-------------|------|------");

            foreach (var request in requests)
            {
                var id = request.Id.ToString().PadRight(4);
                var user = request.UserName.PadRight(16);
                var type = request.RoomType.ToString().PadRight(11);
                var date = request.DateStarting.ToString("yyyy-MM-dd").PadRight(12);
                var days = request.QuantityDays.ToString().PadRight(5);
                var number = request.RoomNumber.HasValue ? request.RoomNumber.Value.ToString() : "-";
                _io.WriteLine($"{id}| {user}| {type}| {date}| {days}| {number}");
            }
            _io.WriteLine("");
        }

        private bool CancelRequest()
        {
            _io.Clear();
            _io.WriteLine("=== ОТМЕНА ЗАЯВКИ ===");
            var id = _io.ReadPositiveInt("Id заявки (из первой колонки): ");

            try
            {
                _requestService.CancelByAdmin(id);
                _io.WriteLine("Заявка отменена. Пользователь уведомлён.");
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