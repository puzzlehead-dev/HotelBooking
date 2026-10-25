using System;
using HotelBooking.Services;

namespace HotelBooking.Tests.Fakes
{
    public class FakeClock : Clock
    {
        private readonly DateOnly _today;

        public FakeClock(DateOnly today)
        {
            _today = today;
        }

        public override DateOnly Today => _today;
    }
}