using Mediator.Activation;
using Mediator.Contracts.Services;
using Mediator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Mediator.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IWindowingService _windowingService;
    private UIElement? _main;

    public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers, IWindowingService windowingService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _windowingService = windowingService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        if (App.MainWindow.Content == null)
        {
            _main = App.GetService<MainPage>();
            App.MainWindow.Content = _main ?? new Frame();
        }

        await HandleActivationAsync(activationArgs).ConfigureAwait(false);

        App.MainWindow.Activate();

        await InitializeAsync().ConfigureAwait(false);
        //await StartupAsync().ConfigureAwait(false);
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

    private Task InitializeAsync()
    {
        return _windowingService.InitializeAsync(App.MainWindow);
    }

    //private async Task StartupAsync()
    //{
    //    await Task.CompletedTask.ConfigureAwait(false);
    //}
}
