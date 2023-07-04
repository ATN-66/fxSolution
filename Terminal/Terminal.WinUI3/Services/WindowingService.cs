using Windows.Graphics;
using Windows.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class WindowingService : IWindowingService
{
    private readonly ILocalSettingsService _localSettingsService;
    private Window? _window;
    private readonly int _defaultWindowWidth;
    private readonly int _defaultWindowHeight;

    public WindowingService(ILocalSettingsService localSettingsService, IConfiguration configuration)
    {
        _localSettingsService = localSettingsService;

        _defaultWindowWidth = configuration.GetValue<int>($"{nameof(_defaultWindowWidth)}");
        _defaultWindowHeight = configuration.GetValue<int>($"{nameof(_defaultWindowHeight)}");
    }

    private string WindowWidthSettingsKey { get; set; } = "WindowWidth";
    private string WindowHeightSettingsKey { get; set; } = "WindowHeight";
    private string IsAlwaysOnTopSettingsKey { get; set; } = "IsAlwaysOnTop";

    [Obsolete("Obsolete")]
    public async Task InitializeAsync(Window window)
    {
        _window = window;
        _window.GetAppWindow().Closing += async (_, _) =>
        {
            await SaveWindowSizeSettingsAsync((int)((WindowEx)_window).Width, (int)((WindowEx)_window).Height).ConfigureAwait(false);
        };

        var (width, height) = await LoadWindowSizeSettingsAsync().ConfigureAwait(true);
        if (width > 0 && height > 0)
        {
            SetWindowSize(width, height);
        }
    }

    public async Task<(int Width, int Height)> LoadWindowSizeSettingsAsync()
    {
        var widthSetting = await _localSettingsService.ReadSettingAsync<string>(WindowWidthSettingsKey).ConfigureAwait(false);
        var heightSetting = await _localSettingsService.ReadSettingAsync<string>(WindowHeightSettingsKey).ConfigureAwait(false);

        if (int.TryParse(widthSetting, out var width) && int.TryParse(heightSetting, out var height))
        {
            return (width, height);
        }

        return (_defaultWindowWidth, _defaultWindowHeight);
    }

    public async Task SaveWindowSizeSettingsAsync(int width, int height)
    {
        await _localSettingsService.SaveSettingAsync(WindowWidthSettingsKey, width.ToString()).ConfigureAwait(false);
        await _localSettingsService.SaveSettingAsync(WindowHeightSettingsKey, height.ToString()).ConfigureAwait(false);
    }

    [Obsolete("Obsolete")]
    public (int Width, int Height)? GetWindowSize()
    {
        if (_window is null)
        {
            return null;
        }

        var currentSize = _window.GetAppWindow().Size;
        return (currentSize.Width, currentSize.Height);
    }

    [Obsolete("Obsolete")]
    public void SetWindowSize(int width, int height)
    {
        if (_window is not null && width > 0 && height > 0)
        {
            _window.GetAppWindow().Resize(new SizeInt32(width, height));
            
        }
    }

    public async Task<bool> LoadIsAlwaysOnTopSettingsAsync()
    {
        var isAlwaysOnTopSetting = await _localSettingsService.ReadSettingAsync<string>(IsAlwaysOnTopSettingsKey).ConfigureAwait(false);
        return bool.TryParse(isAlwaysOnTopSetting, out var isAlwaysOnTop) && isAlwaysOnTop;
    }

    public Task SaveIsAlwaysOnTopSettingsAsync(bool isAlwaysOnTop)
    {
        return _localSettingsService.SaveSettingAsync(IsAlwaysOnTopSettingsKey, isAlwaysOnTop.ToString());
    }

    public bool? GetIsAlwaysOnTop()
    {
        return _window?.GetIsAlwaysOnTop();
    }

    public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
    {
        _window?.SetIsAlwaysOnTop(isAlwaysOnTop);
    }
}
