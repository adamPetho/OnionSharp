using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace OnionSharp.Microservices;

public static class EnvironmentHelpers
{
    // appName, dataDir
    private static ConcurrentDictionary<string, string> DataDirDict { get; } = new ConcurrentDictionary<string, string>();

    // Do not change the output of this function. Backwards compatibility depends on it.
    public static string GetDataDir(string appName)
    {
        if (DataDirDict.TryGetValue(appName, out string? dataDir))
        {
            return dataDir;
        }

        string directory;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
            {
                directory = Path.Combine(home, "." + appName.ToLowerInvariant());
                Console.WriteLine($"Using HOME environment variable for initializing application data at `{directory}`.");
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
                Console.WriteLine($"Using APPDATA environment variable for initializing application data at `{directory}`.");
            }
            else
            {
                throw new DirectoryNotFoundException("Could not find suitable datadir.");
            }
        }

        if (Directory.Exists(directory))
        {
            DataDirDict.TryAdd(appName, directory);
            return directory;
        }

        Console.WriteLine($"Creating data directory at `{directory}`.");
        Directory.CreateDirectory(directory);

        DataDirDict.TryAdd(appName, directory);
        return directory;
    }

    public static string GetFullBaseDirectory()
    {
        var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (!fullBaseDirectory.StartsWith('/'))
            {
                fullBaseDirectory = fullBaseDirectory.Insert(0, "/");
            }
        }

        return fullBaseDirectory;
    }
}
