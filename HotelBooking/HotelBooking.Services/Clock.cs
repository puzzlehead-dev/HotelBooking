namespace HotelBooking.Services
{
    public class Clock
    {
        public virtual DateOnly Today { get => DateOnly.FromDateTime(DateTime.Now);}
    }
}