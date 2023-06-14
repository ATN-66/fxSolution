using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
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
using Microsoft.Extensions.Logging;
using Serilog.Core;

namespace Terminal.WinUI3;

public partial class App
{
    public App()
    {
        InitializeComponent();

        Environment.SetEnvironmentVariable("Terminal.WinUI3.ENVIRONMENT", "Development"); //appsettings.development
        //Environment.SetEnvironmentVariable("Terminal.WinUI3.ENVIRONMENT", "Production"); //appsettings.production
        var environment = Environment.GetEnvironmentVariable("Terminal.WinUI3.ENVIRONMENT")!.ToLower();
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directoryPath = Path.GetDirectoryName(assemblyLocation);
        var builder = new ConfigurationBuilder().SetBasePath(directoryPath!).
            AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).
            AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
        IConfiguration configuration = builder.Build();

        var logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
        Log.Logger = logger;

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory).ConfigureServices((context, services) =>
        {
            services.AddSingleton(configuration);
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(logger, dispose: true);
            });

            // Configuration
            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

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
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IDialogService, DialogService>();

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

            //DatabaseMaintenance
            services.AddTransient<TicksOverviewViewModel>();
            services.AddTransient<TicksOverviewPage>();
            
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

           
        }).UseSerilog().Build();
        
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

    private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs exception)
    {
        if (exception.Exception is { } ex)//todo
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        else
        {
            Log.Fatal("Host terminated unexpectedly due to an unknown exception");
        }
        Log.CloseAndFlush();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        if (System.Diagnostics.Debugger.IsAttached)
        {
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        }

        GetService<IDispatcherService>().Initialize(DispatcherQueue.GetForCurrentThread());
        GetService<IDashboardService>().InitializeAsync();
        GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(false);
        GetService<IAppNotificationService>().Initialize();
        GetService<ILogger<App>>().LogInformation("This is an information message: Launched");
    }

    private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs e)
    {
        throw new NotImplementedException();
    }
}


//var dataServiceTask = GetService<IDataService>().InitializeAsync();
//await dataServiceTask.ConfigureAwait(false);
//Task.Run(() => GetService<IDataService>().StartAsync());