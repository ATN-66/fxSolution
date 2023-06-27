using System.Reflection;
using Common.Entities;

namespace Terminal.WinUI3.Helpers;

public static class EnvironmentHelper
{
    public static string GetExecutingAssemblyName()
    {
        return Assembly.GetExecutingAssembly().GetName().Name!;
    }

    public static Workplace SetWorkplaceFromEnvironment()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var environment = Environment.GetEnvironmentVariable(assemblyName!)!;

        if (Enum.TryParse<Workplace>(environment, out var workplace))
        {
            return workplace;
        }

        throw new InvalidOperationException($"{nameof(environment)} setting for {assemblyName} is null or invalid.");
    }
}