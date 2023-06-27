/*+------------------------------------------------------------------+
  |                                                   Terminal.WinUI3|
  |                                                           App.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
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

namespace Terminal.WinUI3;

public partial class App
{
    private readonly CancellationTokenSource _cts;
    public App()
    {
        InitializeComponent();

        // To set an environment variable on your computer, you can use the following steps.Please note these steps are for Windows 10, so they may vary slightly depending on your version of Windows:
        // 1) Right - click on the Computer icon on your desktop or in File Explorer, then choose Properties.
        // 2) Click on Advanced system settings.
        // 3) Click on Environment Variables.
        // 4) In the System variables section, click on New....Enter "Terminal.WinUI3.ENVIRONMENT"(without quotes) as the variable name.
        // 5) Enter the value you want in the variable value field.
        // 6) Click OK in all dialog boxes.

        const string environmentStr = "Terminal.WinUI3.ENVIRONMENT";
        var environment = Environment.GetEnvironmentVariable(environmentStr)!;
        if (Enum.TryParse<Workplace>(environment, out var workplace))
        {
            Workplace = workplace;
        }
        else
        {
            throw new InvalidOperationException($"{GetType()}: {environmentStr} is null.");
        }

        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directoryPath = Path.GetDirectoryName(assemblyLocation);
        var builder = new ConfigurationBuilder().SetBasePath(directoryPath!)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment.ToLower()}.json", optional: true, reloadOnChange: true);
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
            services.AddSingleton<CancellationTokenSource>();
            services.AddSingleton<IDashboardService, DashboardService>();

            // Business Services
            services.AddSingleton<IProcessor, Processor>();
            services.AddSingleton<IExternalDataSource, ExternalDataSource>();
            services.AddSingleton<IMediator, Mediator>();
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

            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

        }).UseSerilog().Build();

        DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        DebugSettings.XamlResourceReferenceFailed += DebugSettings_XamlResourceReferenceFailed;
        UnhandledException += App_UnhandledException;

        App.GetService<IAppNotificationService>().Initialize();
        _cts = Host.Services.GetRequiredService<CancellationTokenSource>();
    }

    public Workplace Workplace
    {
        get;
        set;
    }
    private IHost Host
    {
        get;
    }
    public static WindowEx MainWindow
    {
        get;
    } = new MainWindow();
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
    private void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs exception)
    {
        Log.Fatal(exception.Message, "DebugSettings_BindingFailed");
        throw new NotImplementedException("DebugSettings_BindingFailed");
    }
    private void DebugSettings_XamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs args)
    {
        throw new NotImplementedException();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        if (System.Diagnostics.Debugger.IsAttached)
        {
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        }
        GetService<ILogger<App>>().LogInformation("<--- Start --->");
        GetService<IDispatcherService>().Initialize(DispatcherQueue.GetForCurrentThread());
        GetService<IDashboardService>().InitializeAsync();
        GetService<IActivationService>().ActivateAsync(args);
    }
}

//var dataServiceTask = GetService<IDataService>().InitializeAsync();
//await dataServiceTask.ConfigureAwait(false);
//Task.Run(() => GetService<IDataService>().StartAsync());