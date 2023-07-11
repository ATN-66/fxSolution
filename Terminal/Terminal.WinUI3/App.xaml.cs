/*+------------------------------------------------------------------+
  |                                                   Terminal.WinUI3|
  |                                                           App.cs |
  +------------------------------------------------------------------+*/

using System.Reflection;
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
using Terminal.WinUI3.Services;
using Terminal.WinUI3.ViewModels;
using Terminal.WinUI3.Views;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using Terminal.WinUI3.Models.Settings;
using Microsoft.Extensions.Logging;
using Common.DataSource;

namespace Terminal.WinUI3;

public partial class App
{
    private readonly CancellationTokenSource _cts;
    public App()
    {
        Workplace appWorkplace;
        InitializeComponent();

        // To set an environment variable on your computer, you can use the following steps.Please note these steps are for Windows 10, so they may vary slightly depending on your version of Windows:
        // 1) Right - click on the Computer icon on your desktop or in File Explorer, then choose Properties.
        // 2) Click on Advanced system settings.
        // 3) Click on Environment Variables.
        // 4) In the System variables section, click on New....Enter "Terminal.WinUI3" (without quotes) as the variable name.
        // 5) Enter the value you want in the variable value field (Development or Production).
        // 6) Click OK in all dialog boxes.

        var environmentVariable = Environment.GetEnvironmentVariable(Assembly.GetExecutingAssembly().GetName().Name!)!;
        if (Enum.TryParse<Workplace>(environmentVariable, out var workplace))
        {
            appWorkplace = workplace;
        }
        else
        {
            throw new InvalidOperationException($"Environment variable for {Assembly.GetExecutingAssembly().GetName().Name} is null.");
        }

        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var directoryPath = Path.GetDirectoryName(assemblyLocation);
        var builder = new ConfigurationBuilder().SetBasePath(directoryPath!)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{appWorkplace.ToString().ToLower()}.json", optional: false, reloadOnChange: false);
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

            // Default Activation Handler and Other Activation Handlers
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
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
            services.AddSingleton<IWindowingService, WindowingService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<CancellationTokenSource>();
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<IAudioPlayer, AudioPlayer>();
            services.AddSingleton<ISplashScreenService, SplashScreenService>();

            // Business Services
            services.AddSingleton<IProcessor, Processor>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IDataConsumerService, DataConsumerService>();
            services.AddSingleton<IExecutiveConsumerService, ExecutiveConsumerService>();
            services.AddSingleton<IDataBaseService, DataBaseService>();
            services.AddSingleton<IVisualService, VisualService>();

            // Navigation and settings
            services.AddTransient<ShellPage>(); services.AddTransient<ShellViewModel>();
            services.AddTransient<DashboardViewModel>(); services.AddTransient<DashboardPage>();
            services.AddTransient<SettingsViewModel>(); services.AddTransient<SettingsPage>();

            // Currencies and Symbols
            services.AddTransient<CurrenciesOverviewViewModel>(); services.AddTransient<CurrenciesOverviewPage>();
            services.AddTransient<CurrencyViewModel>(); services.AddTransient<CurrencyPage>();
            services.AddTransient<SymbolViewModel>(); services.AddTransient<SymbolPage>();

            // DatabaseMaintenance
            services.AddTransient<HistoricalDataOverviewViewModel>(); services.AddTransient<HistoricalDataOverviewPage>();
            services.AddTransient<TicksContributionsViewModel>(); services.AddTransient<TicksContributionsPage>();

            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            services.Configure<ProviderBackupSettings>(context.Configuration.GetSection(nameof(ProviderBackupSettings)));
            services.Configure<SolutionDataBaseSettings>(context.Configuration.GetSection(nameof(SolutionDataBaseSettings)));
        }).UseSerilog().Build();

        DebugSettings.IsBindingTracingEnabled = true;
        DebugSettings.IsXamlResourceReferenceTracingEnabled = true;

        DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        DebugSettings.XamlResourceReferenceFailed += DebugSettings_XamlResourceReferenceFailed;
        UnhandledException += App_UnhandledException;

        GetService<IAppNotificationService>().Initialize();
        _cts = Host.Services.GetRequiredService<CancellationTokenSource>();
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
    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _cts.Cancel();
        GetService<IAudioPlayer>().Dispose();
        GetService<ILogger<App>>().LogInformation("<--- end --->");
    }
    private static void DebugSettings_BindingFailed(object sender, BindingFailedEventArgs exception)
    {
        Log.Fatal(exception.Message, "DebugSettings_BindingFailed");
        throw new NotImplementedException("DebugSettings_BindingFailed");
    }
    private static void DebugSettings_XamlResourceReferenceFailed(DebugSettings sender, XamlResourceReferenceFailedEventArgs args)
    {
        throw new NotImplementedException();
    }
    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs exception)
    {
        if (exception.Exception is { } ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        else
        {
            Log.Fatal("Host terminated unexpectedly due to an unknown exception");
        }
        Log.CloseAndFlush();

        _cts.Cancel();
        GetService<IAudioPlayer>().Dispose();
        GetService<ILogger<App>>().LogInformation("<--- end --->");
        Current.Exit();
    }
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow.Closed += MainWindow_Closed;

        GetService<ILogger<App>>().LogInformation("<--- start --->");
        GetService<IDispatcherService>().Initialize(DispatcherQueue.GetForCurrentThread());
        await GetService<IDashboardService>().InitializeAsync().ConfigureAwait(true);
        await GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(true);

        using var scope = Host.Services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IProcessor>();
        var processorTask = processor.StartAsync(_cts.Token);
        await Task.WhenAny(processorTask).ConfigureAwait(false);
    }
}