using System.Diagnostics;
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
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IWindowingService _windowingService;
    private UIElement? _shell;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IThemeSelectorService themeSelectorService, IWindowingService windowingService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _windowingService = windowingService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        try
        {
            await InitializeAsync().ConfigureAwait(true);
            if (App.MainWindow.Content == null)
            {
                _shell = App.GetService<ShellPage>();
                App.MainWindow.Content = _shell ?? new Frame();
            }

            await HandleActivationAsync(activationArgs).ConfigureAwait(true);
            App.MainWindow.Activate();
            await StartupAsync().ConfigureAwait(true);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
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
        await _windowingService.InitializeAsync(App.MainWindow).ConfigureAwait(false);
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task StartupAsync()
    {
        await _themeSelectorService.SetRequestedThemeAsync().ConfigureAwait(false);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}