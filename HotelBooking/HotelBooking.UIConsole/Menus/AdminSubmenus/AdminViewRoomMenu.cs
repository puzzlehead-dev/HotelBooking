using System.Collections.Generic;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.AdminSubmenus
{
    public class AdminViewRoomMenu : MenuBase
    {
        private readonly RoomService _roomService;

        private List<Room> _original = new();
        private List<Room> _current = new();
        private readonly Stack<List<Room>> _history = new();

        public AdminViewRoomMenu(ConsoleIO io, RoomService roomService) : base(io)
        {
            _roomService = roomService;
        }

        public override void Run()
        {
            _original = _roomService.GetAll();
            _current = new List<Room>(_original);
            _history.Clear();

            if (_original.Count == 0)
            {
                _io.WriteLine("Номеров нет.");
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
                    case 4: HandleUndo(); break;
                    case 5: HandleReset(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void ShowTable(List<Room> rooms)
        {
            _io.WriteLine("=== НОМЕРА ГОСТИНИЦЫ ===");
            _io.WriteLine($"Всего: {rooms.Count}");
            _io.WriteLine("");
            _io.WriteLine("Номер   | Тип           | Статус");
            _io.WriteLine("--------|---------------|--------");

            foreach (var room in rooms)
            {
                var number = room.Number.ToString().PadRight(7);
                var type = room.Type.ToString().PadRight(13);
                var status = room.Status.ToString();
                _io.WriteLine($"{number} | {type} | {status}");
            }
            _io.WriteLine("");
        }

        private bool HandleEmpty()
        {
            _io.WriteLine("Номеров по текущему фильтру нет.");
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
            _io.WriteLine("1. По номеру");
            _io.WriteLine("2. По типу");
            var fieldChoice = _io.ReadInt("Поле сортировки: ");
            if (fieldChoice != 1 && fieldChoice != 2)
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

            var field = fieldChoice == 1 ? RoomSortField.Number : RoomSortField.Type;
            var ascending = directionChoice == 1;

            PushHistory();
            _current = _roomService.Sort(_current, field, ascending);
        }

        private void HandleFilter()
        {
            _io.Clear();
            _io.WriteLine("1. По номеру");
            _io.WriteLine("2. По типу");
            _io.WriteLine("3. По статусу");
            var fieldChoice = _io.ReadInt("Поле фильтра: ");
            switch (fieldChoice)
            {
                case 1:
                    {
                        var roomNumberMin = _io.ReadPositiveInt("Минимальный номер: ");
                        var roomNumberMax = _io.ReadPositiveInt("Максимальный номер: ");
                        if (roomNumberMin > roomNumberMax)
                        {
                            (roomNumberMin, roomNumberMax) = (roomNumberMax, roomNumberMin);
                        }
                        _io.WriteLine("1. В пределах диапазона");
                        _io.WriteLine("2. За пределы диапазона");
                        var directionChoice = _io.ReadInt("Режим: ");
                        if (directionChoice != 1 && directionChoice != 2)
                        {
                            _io.WriteLine("Ошибка: неверный выбор.");
                            _io.WaitEnter();
                            return;
                        }
                        var invert = directionChoice != 1;
                        PushHistory();
                        _current = _roomService.FilterByNumber(_current, roomNumberMin, roomNumberMax, invert);
                        break;
                    }
                case 2:
                    {
                        var type = _io.ReadRoomType("Выберите тип номера:");
                        var mode = _io.ReadInt("1 — только этот тип, 2 — кроме него: ");
                        if (mode != 1 && mode != 2)
                        {
                            _io.WriteLine("Ошибка: неверный выбор.");
                            _io.WaitEnter();
                            return;
                        }
                        PushHistory();
                        _current = _roomService.FilterByType(_current, type, invert: mode == 2);
                        break;
                    }
                case 3:
                    {
                        var status = _io.ReadRoomStatus("Выберите статус:");
                        var mode = _io.ReadInt("1 — только этот статус, 2 — кроме него: ");
                        if (mode != 1 && mode != 2)
                        {
                            _io.WriteLine("Ошибка: неверный выбор.");
                            _io.WaitEnter();
                            return;
                        }
                        PushHistory();
                        _current = _roomService.FilterByStatus(_current, status, invert: mode == 2);
                        break;
                    }
                default:
                    {
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        return;
                    }
            }
        }

        private void HandleSearch()
        {
            _io.Clear();
            var key = _io.ReadLine("Поиск по номеру (пусто — отмена): ");
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }
            PushHistory();
            _current = _roomService.SearchByNumber(_current, key);
        }

        private void HandleUndo()
        {
            Undo();
        }

        private void HandleReset()
        {
            PushHistory();
            _current = new List<Room>(_original);
        }

        private void PushHistory()
        {
            _history.Push(new List<Room>(_current));
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