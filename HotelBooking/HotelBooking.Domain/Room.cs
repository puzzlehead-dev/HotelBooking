namespace HotelBooking.Domain
{
    public class Room
    {
        private int _number;
        private RoomType _type;
        private RoomStatus _status;

        public Room(int number, RoomType type, RoomStatus status)
        {
            if (number <= 0)
            {
                throw new ArgumentException("Номер комнаты должен быть положительным", nameof(number));
            }
            _number = number;
            _type = type;
            _status = status;
        }

        public int Number { get => _number;}
        public RoomType Type { get => _type;}
        public RoomStatus Status { get => _status;}

        public void ChangeStatus(RoomStatus status)
        {
            _status = status;
        }
    }
}
