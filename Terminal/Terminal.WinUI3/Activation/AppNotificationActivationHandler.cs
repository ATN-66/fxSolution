﻿using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Activation;

public class AppNotificationActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    //private readonly INavigationService _navigationService;
    //private readonly IAppNotificationService _notificationService;

    //public AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService)
    //{
    //    _navigationService = navigationService;
    //    _notificationService = notificationService;
    //}

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args) => AppInstance.GetCurrent().GetActivatedEventArgs()?.Kind == ExtendedActivationKind.AppNotification;
    protected override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        // TODO: Handle notifications activations.

        //// // Access the AppNotificationActivatedEventArgs.
        //// var activatedEventArgs = (AppNotificationActivatedEventArgs)AppInstance.GetCurrent().GetActivatedEventArgs().Data;

        //// // Navigate to a specific page based on the notifications arguments.
        //// if (_notificationService.ParseArguments(activatedEventArgs.Argument)["action"] == "Settings")
        //// {
        ////     // Queue navigation with low priority to allow the UI to initialize.
        ////     App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        ////     {
        ////         _navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
        ////     });
        //// }

        App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            App.MainWindow.ShowMessageDialogAsync("TODO: Handle notifications activations.", "Notifications Activation");
        });

        return Task.CompletedTask;
    }
}