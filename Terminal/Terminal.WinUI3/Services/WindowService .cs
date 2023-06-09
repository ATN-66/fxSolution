using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class WindowService : IWindowService
{
    public WindowEx CurrentWindow => App.MainWindow;
}