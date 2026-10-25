namespace HotelBooking.Domain
{
    public class Notification
    {
        private string _userName;
        private DateOnly _date;
        private string _message;

        public Notification(string userName, DateOnly date, string message)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentException("Имя пользователя не может быть пустым", nameof(userName));
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Сообщение не может быть пустым", nameof(message));
            }
            if (date == default)
            {
                throw new ArgumentException("Дата не может быть значением по умолчанию", nameof(date));
            }
            _userName = userName;
            _date = date;
            _message = message;
        }

        public string UserName { get => _userName; }
        public DateOnly Date { get => _date; }
        public string Message { get => _message; }
    }
}
