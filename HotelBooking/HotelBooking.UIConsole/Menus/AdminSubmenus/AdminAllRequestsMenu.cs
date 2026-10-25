using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.AdminSubmenus
{
    public class AdminAllRequestsMenu : MenuBase
    {
        private readonly RequestService _requestService;

        private List<Request> _original = new();
        private List<Request> _current = new();
        private readonly Stack<List<Request>> _history = new();

        public AdminAllRequestsMenu(ConsoleIO io, RequestService requestService) : base(io)
        {
            _requestService = requestService;
        }

        public override void Run()
        {
            _original = _requestService.GetAll();
            _current = new List<Request>(_original);
            _history.Clear();
            if (_original.Count == 0)
            {
                _io.WriteLine("Заявок нет.");
                _io.WaitEnter();
                return;
            }

            while (true)
            {
                _io.Clear();
                ShowTable(_current);

                if (_current.Count == 0)
                {
                    if (!HandleEmpty()) return;
                    continue;
                }

                _io.WriteLine("0. Выйти в меню");
                _io.WriteLine("1. Сортировка");
                _io.WriteLine("2. Фильтр");
                _io.WriteLine("3. Поиск");
                _io.WriteLine("4. Отменить последнее действие");
                _io.WriteLine("5. Сбросить фильтры");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: HandleSort(); break;
                    case 2: HandleFilter(); break;
                    case 3: HandleSearch(); break;
                    case 4: Undo(); break;
                    case 5: HandleReset(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void ShowTable(List<Request> requests)
        {
            _io.WriteLine("=== ВСЕ ЗАЯВКИ ===");
            _io.WriteLine($"Всего: {requests.Count}");
            _io.WriteLine("");
            _io.WriteLine("Id  | Пользователь    | Тип        | Дата        | Дней | Статус       | Номер");
            _io.WriteLine("----|-----------------|------------|-------------|------|--------------|------");

            foreach (var request in requests)
            {
                var id = request.Id.ToString().PadRight(4);
                var user = request.UserName.PadRight(16);
                var type = request.RoomType.ToString().PadRight(11);
                var date = request.DateStarting.ToString("yyyy-MM-dd").PadRight(12);
                var days = request.QuantityDays.ToString().PadRight(5);
                var status = request.Status.ToString().PadRight(13);
                var number = request.RoomNumber.HasValue ? request.RoomNumber.Value.ToString() : "-";
                _io.WriteLine($"{id}| {user}| {type}| {date}| {days}| {status}| {number}");
            }
            _io.WriteLine("");
        }

        private bool HandleEmpty()
        {
            _io.WriteLine("Заявок по текущему фильтру нет.");
            _io.WriteLine("0. Выйти");
            _io.WriteLine("1. Отменить последнее действие");
            _io.WriteLine("2. Сбросить фильтры");

            var choice = _io.ReadInt("Выбор: ");
            switch (choice)
            {
                case 1:
                    Undo();
                    return true;
                case 2:
                    HandleReset();
                    return true;
                default:
                    return false;
            }
        }

        private void HandleSort()
        {
            _io.Clear();
            _io.WriteLine("1. По имени пользователя");
            _io.WriteLine("2. По дате начала");
            _io.WriteLine("3. По количеству дней");
            _io.WriteLine("4. По номеру комнаты");
            var fieldChoice = _io.ReadInt("Поле сортировки: ");
            if (fieldChoice < 1 || fieldChoice > 4)
            {
                _io.WriteLine("Ошибка: неверный выбор.");
                _io.WaitEnter();
                return;
            }

            _io.WriteLine("1. По возрастанию");
            _io.WriteLine("2. По убыванию");
            var directionChoice = _io.ReadInt("Направление: ");
            if (directionChoice != 1 && directionChoice != 2)
            {
                _io.WriteLine("Ошибка: неверный выбор.");
                _io.WaitEnter();
                return;
            }

            var field = fieldChoice switch
            {
                1 => RequestSortField.UserName,
                2 => RequestSortField.DateStarting,
                3 => RequestSortField.QuantityDays,
                _ => RequestSortField.RoomNumber
            };
            var ascending = directionChoice == 1;

            PushHistory();
            _current = _requestService.Sort(_current, field, ascending);
        }

        private void HandleFilter()
        {
            _io.Clear();
            _io.WriteLine("1. По имени пользователя");
            _io.WriteLine("2. По типу номера");
            _io.WriteLine("3. По дате начала (диапазон)");
            _io.WriteLine("4. По количеству дней (диапазон)");
            _io.WriteLine("5. По статусу");
            _io.WriteLine("6. По номеру комнаты (диапазон)");
            var fieldChoice = _io.ReadInt("Поле фильтра: ");
            switch (fieldChoice)
            {
                case 1:
                    {
                        var name = _io.ReadLine("Имя пользователя: ");
                        if (string.IsNullOrWhiteSpace(name))
                        {
                            return;
                        }
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByUserName(_current, name, invert: mode == 2);
                        break;
                    }
                case 2:
                    {
                        var type = _io.ReadRoomType("Выберите тип номера:");
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByRoomType(_current, type, invert: mode == 2);
                        break;
                    }
                case 3:
                    {
                        var from = _io.ReadDate("Дата начала (от):");
                        var to = _io.ReadDate("Дата начала (до):");
                        if (from > to)
                        {
                            (from, to) = (to, from);
                        }
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByDateRange(_current, from, to, invert: mode == 2);
                        break;
                    }
                case 4:
                    {
                        var min = _io.ReadPositiveInt("Минимум дней: ");
                        var max = _io.ReadPositiveInt("Максимум дней: ");
                        if (min > max)
                        {
                            (min, max) = (max, min);
                        }
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByQuantityDays(_current, min, max, invert: mode == 2);
                        break;
                    }
                case 5:
                    {
                        var status = _io.ReadRequestStatus("Выберите статус:");
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByStatus(_current, status, invert: mode == 2);
                        break;
                    }
                case 6:
                    {
                        var min = _io.ReadPositiveInt("Минимум номера: ");
                        var max = _io.ReadPositiveInt("Максимум номера: ");
                        if (min > max)
                        {
                            (min, max) = (max, min);
                        }
                        var mode = ReadFilterMode();
                        if (mode == 0) return;
                        PushHistory();
                        _current = _requestService.FilterByRoomNumber(_current, min, max, invert: mode == 2);
                        break;
                    }
                default:
                    _io.WriteLine("Ошибка: неверный выбор.");
                    _io.WaitEnter();
                    return;
            }
        }

        private int ReadFilterMode()
        {
            _io.WriteLine("1 — в пределах, 2 — вне пределов");
            var mode = _io.ReadInt("Режим: ");
            if (mode != 1 && mode != 2)
            {
                _io.WriteLine("Ошибка: неверный выбор.");
                _io.WaitEnter();
                return 0;
            }
            return mode;
        }

        private void HandleSearch()
        {
            _io.Clear();
            _io.WriteLine("1. По имени пользователя");
            _io.WriteLine("2. По дате (ГГГГ-ММ-ДД)");
            _io.WriteLine("3. По количеству дней");
            _io.WriteLine("4. По номеру комнаты");
            var fieldChoice = _io.ReadInt("Поле поиска: ");
            if (fieldChoice < 1 || fieldChoice > 4)
            {
                _io.WriteLine("Ошибка: неверный выбор.");
                _io.WaitEnter();
                return;
            }

            var query = _io.ReadLine("Поисковый запрос (пусто — отмена): ");
            if (string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            PushHistory();
            _current = fieldChoice switch
            {
                1 => _requestService.SearchByUserName(_current, query),
                2 => _requestService.SearchByDate(_current, query),
                3 => _requestService.SearchByQuantityDays(_current, query),
                _ => _requestService.SearchByRoomNumber(_current, query)
            };
        }

        private void HandleReset()
        {
            PushHistory();
            _current = new List<Request>(_original);
        }

        private void PushHistory()
        {
            _history.Push(new List<Request>(_current));
        }

        private void Undo()
        {
            if (_history.Count == 0)
            {
                _io.WriteLine("Нечего отменять.");
                _io.WaitEnter();
                return;
            }
            _current = _history.Pop();
        }
    }
}