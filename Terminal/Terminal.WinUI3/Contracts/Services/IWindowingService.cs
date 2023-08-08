using Microsoft.UI.Xaml;

namespace Terminal.WinUI3.Contracts.Services;

public interface IWindowingService
{
    void Initialize(Window window);

    (int Width, int Height) LoadWindowSizeSettings();
    void SaveWindowSizeSettings(int width, int height);

    (int Width, int Height)? GetWindowSize();
    void SetWindowSize(int width, int height);
}