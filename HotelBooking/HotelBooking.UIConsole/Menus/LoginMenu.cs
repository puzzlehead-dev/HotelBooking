using System;
using HotelBooking.Domain;
using HotelBooking.Services;

namespace HotelBooking.UIConsole.Menus
{
    public class LoginMenu : MenuBase
    {
        private readonly AuthService _auth;

        public LoginMenu(ConsoleIO io, AuthService auth) : base(io)
        {
            _auth = auth;
        }

        public User? AuthenticatedUser { get; private set; }

        public override void Run()
        {
            AuthenticatedUser = null;

            while (AuthenticatedUser == null)
            {
                _io.Clear();
                _io.WriteLine("=== ГЛАВНОЕ МЕНЮ ВХОДА ===");
                _io.WriteLine("1. Войти как администратор");
                _io.WriteLine("2. Войти как пользователь");
                _io.WriteLine("0. Выйти из программы");

                var choice = _io.ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1:
                        AuthenticatedUser = HandleAuth(Role.Admin);
                        break;
                    case 2:
                        AuthenticatedUser = HandleAuth(Role.User);
                        break;
                    case 0:
                        return;
                    default:
                        _io.WriteLine("Ошибка: неверный выбор.");
                        _io.WaitEnter();
                        break;
                }
            }
        }

        private User? HandleAuth(Role role)
        {
            _io.Clear();
            _io.WriteLine($"=== МЕНЮ ВХОДА ({role}) ===");
            _io.WriteLine("1. Зарегистрироваться");
            _io.WriteLine("2. Войти");
            _io.WriteLine("0. Назад");

            var choice = _io.ReadInt("Выбор: ");
            switch (choice)
            {
                case 1:
                    return Register(role);
                case 2:
                    return Login(role);
                case 0:
                    return null;
                default:
                    _io.WriteLine("Ошибка: неверный выбор.");
                    _io.WaitEnter();
                    return null;
            }
        }

        private User? Register(Role role)
        {
            _io.Clear();
            _io.WriteLine($"=== РЕГИСТРАЦИЯ ({role}) ===");
            var name = _io.ReadLine("Логин: ");
            var password = _io.ReadLine("Пароль: ");
            var confirm = _io.ReadLine("Подтвердите пароль: ");
            if (password != confirm)
            {
                _io.WriteLine("Ошибка: пароли не совпадают.");
                _io.WaitEnter();
                return null;
            }
            try
            {
                var user = _auth.Register(name, password, role);
                _io.WriteLine($"Регистрация успешна. Добро пожаловать, {user.Name}!");
                _io.WaitEnter();
                return user;
            }
            catch (InvalidOperationException ex)
            {
                _io.WriteLine($"Ошибка: {ex.Message}");
                _io.WaitEnter();
                return null;
            }
            catch (ArgumentException ex)
            {
                _io.WriteLine($"Некорректные данные: {ex.Message}");
                _io.WaitEnter();
                return null;
            }
        }

        private User? Login(Role role)
        {
            _io.Clear();
            _io.WriteLine($"=== ВХОД ({role}) ===");
            var name = _io.ReadLine("Логин: ");
            var password = _io.ReadLine("Пароль: ");
            if (_auth.TryLogin(name, password, out var user))
            {
                if (user.Role != role)
                {
                    _io.WriteLine($"Ошибка: этот пользователь не {role}.");
                    _io.WaitEnter();
                    return null;
                }
                _io.WriteLine($"Добро пожаловать, {user.Name}!");
                _io.WaitEnter();
                return user;
            }
            _io.WriteLine("Ошибка: неверный логин или пароль.");
            _io.WaitEnter();
            return null;
        }
    }
}