using System.ComponentModel;

namespace Terminal.WinUI3.Helpers;
internal static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var fi = value.GetType().GetField(value.ToString());
        var attributes = (DescriptionAttribute[])fi?.GetCustomAttributes(typeof(DescriptionAttribute), false)!;
        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }

    public static T GetEnumValueFromDescription<T>(this string description)
    {
        foreach (var field in typeof(T).GetFields())
        {
            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
            {
                if (attribute.Description == description)
                {
                    return (T)field.GetValue(null)!;
                }
            }
            else if (field.Name == description)
            {
                return (T)field.GetValue(null)!;
            }
        }

        throw new ArgumentException($@"Not found: {description}", nameof(description));
    }
}