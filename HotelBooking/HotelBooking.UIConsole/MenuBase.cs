using System;

namespace HotelBooking.UIConsole
{
    public abstract class MenuBase
    {
        protected readonly ConsoleIO _io;

        protected MenuBase(ConsoleIO io)
        {
            _io = io;
        }

        public abstract void Run();

        protected void Execute(string successMessage, Action action)
        {
            try
            {
                action();
                _io.WriteLine(successMessage);
            }
            catch (InvalidOperationException ex)
            {
                _io.WriteLine($"Ошибка: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                _io.WriteLine($"Некорректные данные: {ex.Message}");
            }
            _io.WaitEnter();
        }
    }
}