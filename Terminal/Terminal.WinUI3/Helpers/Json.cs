using Newtonsoft.Json;

namespace Terminal.WinUI3.Helpers;

public static class Json
{
    public static Task<T> ToObjectAsync<T>(string value)
    {
        return Task.Run<T>(() => JsonConvert.DeserializeObject<T>(value)!);
    }

    public static Task<string> StringifyAsync(object value)
    {
        return Task.Run(() => JsonConvert.SerializeObject(value));
    }
}
