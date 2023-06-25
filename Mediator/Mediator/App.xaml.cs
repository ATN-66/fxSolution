/*+------------------------------------------------------------------+
  |                                                          Mediator|
  |                                                           App.cs |
  +------------------------------------------------------------------+*/

using Mediator.Activation;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.Services;
using Mediator.ViewModels;
using Mediator.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using Environment = System.Environment;
using Symbol = Common.Entities.Symbol;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Mediator;

public partial class App
{
    private readonly CancellationTokenSource _cts;

    public App()
    {
        InitializeComponent();

        Environment.SetEnvironmentVariable("Mediator.ENVIRONMENT", "Development"); //appsettings.development
        //Environment.SetEnvironmentVariable("Mediator.ENVIRONMENT", "Production"); //appsettings.production
        var environment = Environment.GetEnvironmentVariable("Mediator.ENVIRONMENT")!.ToLower();
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

            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<IWindowingService, WindowingService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();

            services.AddSingleton<CancellationTokenSource>();
            services.AddTransient<IIndicatorToMediatorService, IndicatorToMediatorService>();
            services.AddSingleton<ITicksProcessor, TicksProcessor>();
            services.AddSingleton<ITicksDataProviderService, TicksDataProviderService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainPage>();

            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
        }).UseSerilog().Build();

        DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        DebugSettings.XamlResourceReferenceFailed += DebugSettings_XamlResourceReferenceFailed;
        UnhandledException += App_UnhandledException;

        App.GetService<IAppNotificationService>().Initialize();
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

    public static T GetService<T>()
        where T : class
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

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        if (System.Diagnostics.Debugger.IsAttached)
        {
            DebugSettings.BindingFailed += DebugSettings_BindingFailed;
        }

        GetService<IDispatcherService>().Initialize(DispatcherQueue.GetForCurrentThread());
        await GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(false);

        using var scope = Host.Services.CreateScope();
        var indicatorToMediatorTasks = (from Symbol symbol in Enum.GetValues(typeof(Symbol))
            let serviceIndicatorToMediator = scope.ServiceProvider.GetService<IIndicatorToMediatorService>()
            select Task.Run(() => serviceIndicatorToMediator.StartAsync(symbol, _cts.Token), _cts.Token)).ToList();

        var ticksDataProviderService = scope.ServiceProvider.GetRequiredService<ITicksDataProviderService>();
        var ticksDataProviderServiceTask = ticksDataProviderService.StartAsync();

        await Task.WhenAny(Task.WhenAll(indicatorToMediatorTasks), ticksDataProviderServiceTask).ConfigureAwait(false);
    }
}



//var mediatorToTerminalClient = scope.ServiceProvider.GetRequiredService<Client>();
//var administrator = scope.ServiceProvider.GetRequiredService<Settings>();
//administrator.TerminalConnectedChanged += async (_, _) => { await mediatorToTerminalClient.StartAsync(cts.Token).ConfigureAwait(false); };
//administrator.TerminalConnectedChanged += async (_, _) => { await mediatorToTerminalClient.ProcessAsync(cts.Token).ConfigureAwait(false); };
//cts.Cancel();
