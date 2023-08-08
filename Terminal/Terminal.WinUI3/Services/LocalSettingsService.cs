using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Settings;

namespace Terminal.WinUI3.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string DefaultApplicationDataFolder = @"Terminal.WinUI3\\ApplicationData";
    private const string DefaultLocalSettingsFile = "LocalSettings.json";
    private readonly string _applicationDataFolder;
    private readonly IFileService _fileService;
    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _localsettingsFile;
    private IDictionary<string, object> _settings = null!;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _applicationDataFolder = Path.Combine(_localApplicationData, options.Value.ApplicationDataFolder ?? DefaultApplicationDataFolder);
        _localsettingsFile = options.Value.LocalSettingsFile ?? DefaultLocalSettingsFile;
    }

    public async Task InitializeAsync()
    {
        _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)).ConfigureAwait(false) ?? new Dictionary<string, object>();
    }

    public T? ReadSetting<T>(string key)
    {
        if (!_settings.TryGetValue(key, out var obj))
        {
            return default;
        }

        var json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public void SaveSetting<T>(string key, T value)
    {
        _settings[key] = value!;
    }

    public void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings);
    }
}