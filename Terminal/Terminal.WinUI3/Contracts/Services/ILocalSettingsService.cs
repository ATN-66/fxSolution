using Microsoft.UI.Xaml;

namespace Terminal.WinUI3.Contracts.Services;

public interface ILocalSettingsService
{
    Task InitializeAsync();
    T? ReadSetting<T>(string key);
    void SaveSetting<T>(string key, T value);
    void MainWindow_Closed(object sender, WindowEventArgs args);
}