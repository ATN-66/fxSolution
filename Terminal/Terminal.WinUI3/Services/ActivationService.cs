using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Activation;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Views;

namespace Terminal.WinUI3.Services;

public class ActivationService : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IWindowingService _windowingService;
    private UIElement? _shell;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, ILocalSettingsService localSettingsService, IWindowingService windowingService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _localSettingsService = localSettingsService;
        _windowingService = windowingService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        await InitializeAsync().ConfigureAwait(true);
        if (App.MainWindow.Content == null)
        {
            _shell = App.GetService<ShellPage>();
            App.MainWindow.Content = _shell ?? new Frame();
        }

        await HandleActivationAsync(activationArgs).ConfigureAwait(true);
        App.MainWindow.Activate();
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs).ConfigureAwait(false);
        }

        if (_defaultHandler.CanHandle(activationArgs))
        {
            await _defaultHandler.HandleAsync(activationArgs).ConfigureAwait(false);
        }
    }

    private async Task InitializeAsync()
    {
        await _localSettingsService.InitializeAsync().ConfigureAwait(true);
        _windowingService.Initialize(App.MainWindow);
        await Task.CompletedTask.ConfigureAwait(true);
    }
}