using System;
using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.UserSubmenus
{
    public class UserCancelRequestMenu : MenuBase
    {
        private readonly string _userName;
        private readonly RequestService _requestService;

        public UserCancelRequestMenu(
            ConsoleIO io,
            string userName,
            RequestService requestService) : base(io)
        {
            _userName = userName;
            _requestService = requestService;
        }

        public override void Run()
        {
            while (true)
            {
                _io.Clear();
                _io.WriteLine("=== ОТМЕНА БРОНИРОВАНИЯ ===");
                _io.WriteLine("1. Отозвать отправленную заявку");
                _io.WriteLine("2. Отменить принятую заявку");
                _io.WriteLine("0. Назад");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: ShowUnprocessed(); break;
                    case 2: ShowAccepted(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void ShowUnprocessed()
        {
            var unprocessed = _requestService.GetByStatusAndUser(RequestStatus.Unprocessed, _userName);
            HandleCancellation(unprocessed, "отправленные");
        }

        private void ShowAccepted()
        {
            var accepted = _requestService.GetByStatusAndUser(RequestStatus.Accepted, _userName);
            HandleCancellation(accepted, "принятые");
        }

        private void HandleCancellation(List<Request> requests, string label)
        {
            if (requests.Count == 0)
            {
                _io.WriteLine($"У вас нет заявок с этим статусом");
                _io.WaitEnter();
                return;
            }

            _io.Clear();
            ShowTable(requests, label);

            _io.WriteLine("0. Назад");
            var id = _io.ReadInt("Id заявки для отмены (0 — назад): ");
            if (id == 0)
            {
                return;
            }

            Request? target = null;
            foreach (var request in requests)
            {
                if (request.Id == id)
                {
                    target = request;
                    break;
                }
            }

            if (target == null)
            {
                _io.WriteLine($"Ошибка: заявка с Id {id} не найдена в этом списке заявок");
                _io.WaitEnter();
                return;
            }

            try
            {
                _requestService.CancelByUser(id);
                _io.WriteLine("Заявка отменена. Вы получите уведомление.");
                _io.WaitEnter();
            }
            catch (InvalidOperationException ex)
            {
                _io.WriteLine($"Ошибка: {ex.Message}");
                _io.WaitEnter();
            }
        }

        private void ShowTable(List<Request> requests, string label)
        {
            _io.WriteLine($"=== {label.ToUpper()} ЗАЯВКИ ===");
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