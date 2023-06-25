using Microsoft.UI.Xaml;

namespace Mediator.Contracts.Services;

public interface IWindowingService
{
    Task InitializeAsync(Window window);
    Task<(int Width, int Height)> LoadWindowSizeSettingsAsync();
    Task SaveWindowSizeSettingsAsync(int width, int height);
    (int Width, int Height)? GetWindowSize();
    void SetWindowSize(int width, int height);
    Task<bool> LoadIsAlwaysOnTopSettingsAsync();
    Task SaveIsAlwaysOnTopSettingsAsync(bool isAlwaysOnTop);
    bool? GetIsAlwaysOnTop();
    void SetIsAlwaysOnTop(bool isAlwaysOnTop);
}