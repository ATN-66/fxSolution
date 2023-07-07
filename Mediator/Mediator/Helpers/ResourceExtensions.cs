using Microsoft.Windows.ApplicationModel.Resources;

namespace Mediator.Helpers;

public static class ResourceExtensions
{
    private static readonly ResourceLoader ResourceLoader = new();
    public static string GetLocalizedString(this string resourceKey)
    {
        return ResourceLoader.GetString(resourceKey);
    }
}
