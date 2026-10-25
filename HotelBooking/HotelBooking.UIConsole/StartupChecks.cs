using System.IO;

namespace HotelBooking.UIConsole
{
    public static class StartupChecks
    {
        public static void EnsureAllPaths()
        {
            Directory.CreateDirectory(DataPaths.DataFolder);
            EnsureFile(DataPaths.UsersFile);
            EnsureFile(DataPaths.RequestsFile);
            EnsureFile(DataPaths.RoomsFile);
            EnsureFile(DataPaths.NotificationsFile);
        }

        private static void EnsureFile(string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }
        }
    }
}