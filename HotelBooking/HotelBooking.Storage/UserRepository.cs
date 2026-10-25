using HotelBooking.Domain;
using System;
using System.Collections.Generic;

namespace HotelBooking.Storage
{
    public class UserRepository
    {
        private const char Separator = '|';
        private readonly string _filePath;

        public UserRepository(string filePath)
        {
            _filePath = filePath;
        }

        public List<User> LoadAll(out int skippedCount)
        {
            skippedCount = 0;
            var users = new List<User>();
            if (!File.Exists(_filePath))
            {
                return users;
            }
            var seenNames = new HashSet<string>();
            var lines = File.ReadAllLines(_filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (!TryParse(line, out var user))
                {
                    skippedCount++;
                    continue;
                }
                if (!seenNames.Add(user.Name))
                {
                    skippedCount++;
                    continue;
                }
                users.Add(user);
            }
            return users;
        }

        public void SaveAll(List<User> users)
        {
            var lines = new List<string>(users.Count);
            foreach (var user in users)
            {
                lines.Add(Format(user));
            }
            File.WriteAllLines(_filePath, lines);
        }

        private bool TryParse(string line, out User user)
        {
            user = null!;
            var parts = line.Split(Separator, StringSplitOptions.TrimEntries);
            if (parts.Length != 3)
            {
                return false;
            }
            var name = parts[0];
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }
            var password = parts[1];
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }
            if (!Enum.TryParse<Role>(parts[2], out var role))
            {
                return false;
            }
            if (!Enum.IsDefined(typeof(Role), role))
            {
                return false;
            }

            user = new User(name, password, role);
            return true;
        }

        private string Format(User user)
        {
            return $"{user.Name}{Separator}{user.Password}{Separator}{user.Role}";
        }

    }
}
