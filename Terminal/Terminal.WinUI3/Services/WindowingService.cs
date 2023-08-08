using Windows.Graphics;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class WindowingService : IWindowingService
{
    private readonly ILocalSettingsService _localSettingsService;
    private Window _window = null!;
    private readonly int _defaultWindowWidth;
    private readonly int _defaultWindowHeight;
    private string WindowWidthSettingsKey { get; set; } = "WindowWidth";
    private string WindowHeightSettingsKey { get; set; } = "WindowHeight";

    [Obsolete("Obsolete")]
    public WindowingService(ILocalSettingsService localSettingsService, IConfiguration configuration)
    {
        _localSettingsService = localSettingsService;

        _defaultWindowWidth = configuration.GetValue<int>($"{nameof(_defaultWindowWidth)}");
        _defaultWindowHeight = configuration.GetValue<int>($"{nameof(_defaultWindowHeight)}");

        App.MainWindow.GetAppWindow().Closing += OnClosing;
    }

    [Obsolete("Obsolete")]
    public void Initialize(Window window)
    {
        _window = window;
        var (width, height) = LoadWindowSizeSettings();
        if (width > 0 && height > 0)
        {
            SetWindowSize(width, height);
        }
    }

    public (int Width, int Height) LoadWindowSizeSettings()
    {
        var widthSetting = _localSettingsService.ReadSetting<string>(WindowWidthSettingsKey);
        var heightSetting = _localSettingsService.ReadSetting<string>(WindowHeightSettingsKey);

        if (int.TryParse(widthSetting, out var width) && int.TryParse(heightSetting, out var height))
        {
            return (width, height);
        }

        return (_defaultWindowWidth, _defaultWindowHeight);
    }

    public void SaveWindowSizeSettings(int width, int height)
    {
        _localSettingsService.SaveSetting(WindowWidthSettingsKey, width.ToString());
        _localSettingsService.SaveSetting(WindowHeightSettingsKey, height.ToString());
    }

    [Obsolete("Obsolete")]
    public (int Width, int Height)? GetWindowSize()
    {
        var currentSize = _window.GetAppWindow().Size;
        return (currentSize.Width, currentSize.Height);
    }

    [Obsolete("Obsolete")]
    public void SetWindowSize(int width, int height)
    {
        if (width > 0 && height > 0)
        {
            _window.GetAppWindow().Resize(new SizeInt32(width, height));
            
        }
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        SaveWindowSizeSettings((int)((WindowEx)_window).Width, (int)((WindowEx)_window).Height);
    }
}