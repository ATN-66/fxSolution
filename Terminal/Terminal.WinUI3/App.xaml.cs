﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Activation;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.AI.Services;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Core.Contracts.Services;
using Terminal.WinUI3.Core.Services;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models;
using Terminal.WinUI3.Notifications;
using Terminal.WinUI3.Services;
using Terminal.WinUI3.ViewModels;
using Terminal.WinUI3.Views;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Terminal.WinUI3;

public partial class App
{
    public App()
    {
        InitializeComponent();
        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory).ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers
            services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

            // Services
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Business Services
            services.AddSingleton<IProcessor, Processor>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IVisualService, VisualService>();

            // Kernel Services
            services.AddSingleton<ISampleDataService, SampleDataService>();
            services.AddSingleton<IFileService, FileService>();

            // ViewModels
            services.AddTransient<USDViewModel>();
            services.AddTransient<USDPage>();
            services.AddTransient<EURViewModel>();
            services.AddTransient<EURPage>();
            services.AddTransient<GBPViewModel>();
            services.AddTransient<GBPPage>();
            services.AddTransient<JPYViewModel>();
            services.AddTransient<JPYPage>();

            services.AddTransient<EURUSDViewModel>();
            services.AddTransient<EURUSDPage>();
            services.AddTransient<GBPUSDViewModel>();
            services.AddTransient<GBPUSDPage>();
            services.AddTransient<EURGBPViewModel>();
            services.AddTransient<EURGBPPage>();
            services.AddTransient<USDJPYViewModel>();
            services.AddTransient<USDJPYPage>();
            services.AddTransient<EURJPYViewModel>();
            services.AddTransient<EURJPYPage>();
            services.AddTransient<GBPJPYViewModel>();
            services.AddTransient<GBPJPYPage>();

            services.AddTransient<JPYUSDViewModel>();
            services.AddTransient<JPYUSDPage>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();
            services.AddTransient<DataGridViewModel>();
            services.AddTransient<DataGridPage>();
            services.AddTransient<ContentGridDetailViewModel>();
            services.AddTransient<ContentGridDetailPage>();
            services.AddTransient<ContentGridViewModel>();
            services.AddTransient<ContentGridPage>();
            services.AddTransient<ListDetailsViewModel>();
            services.AddTransient<ListDetailsPage>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<HomePage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).Build();
        UnhandledException += App_UnhandledException;
    }

    private IHost Host
    {
        get;
    }

    public static WindowEx MainWindow
    {
        get;
    } = new MainWindow();

    public static UIElement? AppTitlebar
    {
        get;
        set;
    }

    public static T GetService<T>() where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e) => throw new NotImplementedException();

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var dataServiceTask = GetService<IDataService>().InitializeAsync();

        GetService<IAppNotificationService>().Initialize();
        GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
        await GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(false);

        await dataServiceTask.ConfigureAwait(false);
#pragma warning disable CS4014
        Task.Run(() => GetService<IDataService>().StartAsync());
#pragma warning restore CS4014
    }
}