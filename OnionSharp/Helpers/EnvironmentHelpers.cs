using System.Net;
using System.Runtime.InteropServices;
using NBitcoin;

namespace OnionSharp.Helpers
{
    public static class EnvironmentHelpers
    {
        public static string GetDataDir(string appName)
        {
            string directory;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    directory = Path.Combine(home, "." + appName.ToLowerInvariant());
                }
                else
                {
                    throw new DirectoryNotFoundException("Could not find suitable datadir.");
                }
            }
            else
            {
                var localAppData = Environment.GetEnvironmentVariable("APPDATA");
                if (!string.IsNullOrEmpty(localAppData))
                {
                    directory = Path.Combine(localAppData, appName);
                }
                else
                {
                    throw new DirectoryNotFoundException("Could not find suitable datadir.");
                }
            }

            if (Directory.Exists(directory))
            {
                return directory;
            }

            Directory.CreateDirectory(directory);
            return directory;
        }  
    }
}
