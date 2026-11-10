
using System.Reflection;

namespace LANRescue.ConsoleApp.Services;

public static class AppVersion
{
    public static string Display =>
        Assembly.GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3) ?? "Unknown";
}
