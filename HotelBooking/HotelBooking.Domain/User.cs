namespace HotelBooking.Domain
{
    public class User
    {
        private string _name;
        private string _password;
        private Role _role;

        public User(string name, string password, Role role)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(name));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Пароль не может быть пустым", nameof(password));
            }
            _name = name;
            _password = password;
            _role = role;
        }

        public string Name { get => _name; }
        public string Password { get => _password; }
        public Role Role { get => _role; }
    }
}
