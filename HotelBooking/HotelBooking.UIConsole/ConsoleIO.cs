using System;
using HotelBooking.Domain;

namespace HotelBooking.UIConsole
{
    public class ConsoleIO
    {
        public string ReadLine(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? string.Empty;
        }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void Clear()
        {
            Console.Clear();
        }

        public int ReadInt(string prompt)
        {
            while (true)
            {
                var input = ReadLine(prompt);
                if (int.TryParse(input, out var value))
                {
                    return value;
                }
                WriteLine("Ошибка: введите целое число.");
            }
        }

        public int ReadPositiveInt(string prompt)
        {
            while (true)
            {
                var value = ReadInt(prompt);
                if (value > 0)
                {
                    return value;
                }
                WriteLine("Ошибка: число должно быть положительным.");
            }
        }

        public DateOnly ReadDate(string prompt)
        {
            while (true)
            {
                WriteLine(prompt);
                var day = ReadPositiveInt("День (1-31): ");
                var month = ReadPositiveInt("Месяц (1-12): ");
                var year = ReadPositiveInt("Год: ");

                if (month > 12 || day > 31)
                {
                    WriteLine("Ошибка: некорректная дата. Попробуйте снова.");
                    continue;
                }

                try
                {
                    var date = new DateOnly(year, month, day);
                    return date;
                }
                catch (ArgumentOutOfRangeException)
                {
                    WriteLine("Ошибка: такой даты не существует. Попробуйте снова.");
                }
            }
        }

        public RoomType ReadRoomType(string prompt)
        {
            while (true)
            {
                WriteLine(prompt);
                WriteLine("1. Single");
                WriteLine("2. Double");
                WriteLine("3. Triple");
                WriteLine("4. Quadruple");

                var choice = ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: return RoomType.Single;
                    case 2: return RoomType.Double;
                    case 3: return RoomType.Triple;
                    case 4: return RoomType.Quadruple;
                    default:
                        WriteLine("Ошибка: неверный выбор.");
                        break;
                }
            }
        }

        public RoomStatus ReadRoomStatus(string prompt)
        {
            while (true)
            {
                WriteLine(prompt);
                WriteLine("1. Open");
                WriteLine("2. Closed");

                var choice = ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: return RoomStatus.Open;
                    case 2: return RoomStatus.Closed;
                    default:
                        WriteLine("Ошибка: неверный выбор.");
                        break;
                }
            }
        }

        public RequestStatus ReadRequestStatus(string prompt)
        {
            while (true)
            {
                WriteLine(prompt);
                WriteLine("1. Unprocessed");
                WriteLine("2. Accepted");
                WriteLine("3. Rejected");
                WriteLine("4. Completed");

                var choice = ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: return RequestStatus.Unprocessed;
                    case 2: return RequestStatus.Accepted;
                    case 3: return RequestStatus.Rejected;
                    case 4: return RequestStatus.Completed;
                    default:
                        WriteLine("Ошибка: неверный выбор.");
                        break;
                }
            }
        }

        public Role ReadRole(string prompt)
        {
            while (true)
            {
                WriteLine(prompt);
                WriteLine("1. User");
                WriteLine("2. Admin");

                var choice = ReadInt("Выбор: ");
                switch (choice)
                {
                    case 1: return Role.User;
                    case 2: return Role.Admin;
                    default:
                        WriteLine("Ошибка: неверный выбор.");
                        break;
                }
            }
        }

        public bool Confirm(string prompt)
        {
            while (true)
            {
                var input = ReadLine(prompt + " (y/n): ").Trim().ToLower();
                if (input == "y" || input == "yes" || input == "д" || input == "да")
                {
                    return true;
                }
                if (input == "n" || input == "no" || input == "н" || input == "нет")
                {
                    return false;
                }
                WriteLine("Ошибка: введите y или n.");
            }
        }

        public void WaitEnter(string message = "Нажмите Enter...")
        {
            ReadLine(message);
        }
    }
}