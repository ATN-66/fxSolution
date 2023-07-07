using Microsoft.Windows.ApplicationModel.Resources;

namespace Terminal.WinUI3.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader ResourceLoader = new();
    public static string GetLocalizedString(this string resourceKey)
    {
        return ResourceLoader.GetString(resourceKey);
    }
}