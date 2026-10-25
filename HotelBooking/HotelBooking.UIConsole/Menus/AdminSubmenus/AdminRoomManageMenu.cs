using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus.AdminSubmenus
{
    public class AdminRoomManageMenu : MenuBase
    {
        private readonly RoomService _roomService;

        public AdminRoomManageMenu(ConsoleIO io, RoomService roomService) : base(io)
        {
            _roomService = roomService;
        }

        public override void Run()
        {
            while (true)
            {
                _io.Clear();
                _io.WriteLine("=== УПРАВЛЕНИЕ НОМЕРАМИ ===");
                _io.WriteLine("1. Добавить номер");
                _io.WriteLine("2. Изменить статус");
                _io.WriteLine("3. Удалить номер");
                _io.WriteLine("0. Назад");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: AddRoom(); break;
                    case 2: ChangeStatus(); break;
                    case 3: DeleteRoom(); break;
                    case 0: return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private void AddRoom()
        {
            _io.Clear();
            _io.WriteLine("=== ДОБАВЛЕНИЕ НОМЕРА ===");
            var number = _io.ReadPositiveInt("Номер комнаты: ");
            var type = _io.ReadRoomType("Выберите тип номера:");

            Execute("Номер успешно добавлен.", () => _roomService.Add(number, type));
        }

        private void ChangeStatus()
        {
            _io.Clear();
            _io.WriteLine("=== ИЗМЕНЕНИЕ СТАТУСА НОМЕРА ===");
            var number = _io.ReadPositiveInt("Номер комнаты: ");
            var status = _io.ReadRoomStatus("Выберите новый статус:");

            Execute("Статус номера изменён.", () => _roomService.ChangeStatus(number, status));
        }

        private void DeleteRoom()
        {
            _io.Clear();
            _io.WriteLine("=== УДАЛЕНИЕ НОМЕРА ===");
            var number = _io.ReadPositiveInt("Номер комнаты: ");

            Execute("Номер удалён.", () => _roomService.Delete(number));
        }
    }
}