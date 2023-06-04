using Windows.ApplicationModel.Activation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Terminal.WinUI3.Activation;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.AI.Services;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Notifications;
using Terminal.WinUI3.Services;
using Terminal.WinUI3.ViewModels;
using Terminal.WinUI3.Views;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using Terminal.WinUI3.Models.Settings;

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
            services.AddSingleton<IFileService, FileService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDashboardService, DashboardService>();

            // Business Services
            services.AddSingleton<IProcessor, Processor>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IVisualService, VisualService>();

            // Views and ViewModels
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsPage>();

            services.AddTransient<DashboardViewModel>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            //todo: to fabric
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
            services.AddTransient<USDEURViewModel>();
            services.AddTransient<USDEURPage>();
            services.AddTransient<GBPUSDViewModel>();
            services.AddTransient<GBPUSDPage>();
            services.AddTransient<USDGBPViewModel>();
            services.AddTransient<USDGBPPage>();
            services.AddTransient<EURGBPViewModel>();
            services.AddTransient<EURGBPPage>();
            services.AddTransient<GBPEURViewModel>();
            services.AddTransient<GBPEURPage>();
            services.AddTransient<USDJPYViewModel>();
            services.AddTransient<USDJPYPage>();
            services.AddTransient<EURJPYViewModel>();
            services.AddTransient<EURJPYPage>();
            services.AddTransient<JPYEURViewModel>();
            services.AddTransient<JPYEURPage>();
            services.AddTransient<GBPJPYViewModel>();
            services.AddTransient<GBPJPYPage>();
            services.AddTransient<JPYGBPViewModel>();
            services.AddTransient<JPYGBPPage>();
            services.AddTransient<JPYUSDViewModel>();
            services.AddTransient<JPYUSDPage>();

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

    private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e) => throw new NotImplementedException();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        if (System.Diagnostics.Debugger.IsAttached)
        {
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        }

        GetService<IDashboardService>().InitializeAsync();

        //GetService<IAppNotificationService>().Initialize();
        //GetService<IAppNotificationService>().Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));
        GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(false);
    }

    private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
    {
        throw new NotImplementedException();
    }
}


//var dataServiceTask = GetService<IDataService>().InitializeAsync();
//await dataServiceTask.ConfigureAwait(false);
//Task.Run(() => GetService<IDataService>().StartAsync());