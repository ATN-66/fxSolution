using Windows.Storage;
using Mediator.Contracts.Services;
using Mediator.Helpers;
using Mediator.Models;
using Microsoft.Extensions.Options;

namespace Mediator.Services;

public class LocalSettingsService : ILocalSettingsService
{
    private const string DefaultApplicationDataFolder = "Mediator/ApplicationData";
    private const string DefaultLocalSettingsFile = "LocalSettings.json";
    private readonly IFileService _fileService;
    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;
    private IDictionary<string, object> _settings;
    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _applicationDataFolder = Path.Combine(_localApplicationData, options.Value.ApplicationDataFolder ?? DefaultApplicationDataFolder);
        _localsettingsFile = options.Value.LocalSettingsFile ?? DefaultLocalSettingsFile;
        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)).ConfigureAwait(false) ?? new Dictionary<string, object>();
            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (RuntimeHelper.IsMSIX)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj).ConfigureAwait(false);
            }
        }
        else
        {
            await InitializeAsync().ConfigureAwait(false);
            if (_settings.TryGetValue(key, out var obj))
            {
                return await Json.ToObjectAsync<T>((string)obj).ConfigureAwait(false);
            }
        }

        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value!).ConfigureAwait(false);
        }
        else
        {
            await InitializeAsync().ConfigureAwait(false);
            _settings[key] = await Json.StringifyAsync(value!).ConfigureAwait(false);
            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings)).ConfigureAwait(false);
        }
    }
}