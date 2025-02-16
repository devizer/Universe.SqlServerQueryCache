using System;
using System.IO;

namespace Universe
{
    public class SystemDriveAccess
    {
        private static Lazy<string> _WindowsSystemDrive = new Lazy<string>(() =>
        {
            bool isWindows = Environment.OSVersion.Platform.ToString().ToLower().StartsWith("win");
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (string.IsNullOrEmpty(systemDrive)) return "C:\\";

            if (!systemDrive.EndsWith(Path.DirectorySeparatorChar.ToString()))
                systemDrive += Path.DirectorySeparatorChar;

            return systemDrive;
        });

        public static string WindowsSystemDrive => _WindowsSystemDrive.Value;
    }
}
