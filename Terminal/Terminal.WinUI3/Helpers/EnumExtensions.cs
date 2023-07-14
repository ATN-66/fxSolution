using System.ComponentModel;

namespace Terminal.WinUI3.Helpers;
internal static class EnumExtensions
{
    public static string? GetDescription(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null)
        {
            return null;
        }

        var field = type.GetField(name);
        if (field != null)
        {
            if (Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) is DescriptionAttribute attr)
            {
                return attr.Description;
            }
        }
        return null;
    }
}
