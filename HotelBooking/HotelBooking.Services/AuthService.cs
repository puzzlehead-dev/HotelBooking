using HotelBooking.Domain;
using HotelBooking.Storage;
using System;
using System.Collections.Generic;

namespace HotelBooking.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public User Register(string name, string password, Role role)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));
            }
            var users = _userRepository.LoadAll(out _);
            if (IsNameTaken(name, users))
            {
                throw new InvalidOperationException($"Пользователь '{name}' уже существует");
            }
            var user = new User(name, password, role);
            users.Add(user);
            _userRepository.SaveAll(users);

            return user;
        }

        public bool TryLogin(string name, string password, out User user)
        {
            user = null!;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var users = _userRepository.LoadAll(out _);
            foreach (var existing in users)
            {
                if (existing.Name == name && existing.Password == password)
                {
                    user = existing;
                    return true;
                }
            }
            return false;
        }

        private bool IsNameTaken(string name, List<User> users)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            foreach (var user in users)
            {
                if (user.Name == name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}