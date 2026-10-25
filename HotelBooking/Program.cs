using System;
using HotelBooking.Domain;
using HotelBooking.Services;
using HotelBooking.Storage;
using HotelBooking.UIConsole;
using HotelBooking.UIConsole.Menus;

internal class Program
{
    private static void Main(string[] args)
    {
        StartupChecks.EnsureAllPaths();

        var io = new ConsoleIO();

        var userRepo = new UserRepository(DataPaths.UsersFile);
        var roomRepo = new RoomRepository(DataPaths.RoomsFile);
        var requestRepo = new RequestRepository(DataPaths.RequestsFile);
        var notifRepo = new NotificationRepository(DataPaths.NotificationsFile);

        var clock = new Clock();

        var auth = new AuthService(userRepo);
        var roomService = new RoomService(roomRepo);
        var notificationService = new NotificationService(notifRepo);
        var requestService = new RequestService(requestRepo, notificationService, clock);
        var bookingService = new BookingService(roomService, requestService, clock);

        requestService.CompleteOverdue();
        requestService.RejectOverdue();

        while (true)
        {
            var loginMenu = new LoginMenu(io, auth);
            loginMenu.Run();

            if (loginMenu.AuthenticatedUser == null)
            {
                io.WriteLine("До свидания!");
                return;
            }

            var user = loginMenu.AuthenticatedUser;
            io.WriteLine($"Вы вошли как: {user.Name} ({user.Role})");
            io.WaitEnter();

            if (user.Role == Role.Admin)
            {
                new AdminMenu(io, user.Name, roomService, requestService, bookingService).Run();
            }
            else
            {
                new UserMenu(io, user.Name, roomService, requestService, bookingService, notificationService).Run();
            }
        }
    }
}