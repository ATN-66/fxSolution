/*+------------------------------------------------------------------+
  |                                                          Mediator|
  |                                                           App.cs |
  +------------------------------------------------------------------+*/

using System.Reflection;
using Common.DataSource;
using Common.Entities;
using Mediator.Activation;
using Mediator.Contracts.Services;
using Mediator.Models;
using Mediator.Services;
using Mediator.ViewModels;
using Mediator.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using Symbol = Common.Entities.Symbol;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

namespace Mediator;

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
        // 4) In the System variables section, click on New....Enter "Mediator" (without quotes) as the variable name.
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

            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IAppNotificationService, AppNotificationService>();
            services.AddSingleton<IWindowingService, WindowingService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataBaseService, DataBaseService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IAudioPlayer, AudioPlayer>();

            services.AddSingleton<CancellationTokenSource>();
            services.AddTransient<IDataConsumerService, DataConsumerService>();
            services.AddSingleton<IDataProviderService, DataProviderService>();
            services.AddSingleton<IExecutiveSupplierService, ExecutiveSupplierService>();
            services.AddSingleton<IExecutiveProviderService, ExecutiveProviderService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainPage>();

            services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            services.Configure<ProviderBackupSettings>(context.Configuration.GetSection(nameof(ProviderBackupSettings)));
            services.Configure<DataProviderSettings>(context.Configuration.GetSection(nameof(DataProviderSettings)));
            services.Configure<ExecutiveProviderSettings>(context.Configuration.GetSection(nameof(ExecutiveProviderSettings)));
        }).UseSerilog().Build();

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
    public static T GetService<T>()
        where T : class
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
    private void App_UnhandledException(object sender, UnhandledExceptionEventArgs exception)
    {
        //todo: save all quotations
        _cts.Cancel();

        if (exception.Exception is { } ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        else
        {
            Log.Fatal("Host terminated unexpectedly due to an unknown exception");
        }
        Log.CloseAndFlush();
        Current.Exit();
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
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainWindow.Closed += MainWindow_Closed;

        GetService<ILogger<App>>().LogInformation("<--- Start --->");
        GetService<IDispatcherService>().Initialize(DispatcherQueue.GetForCurrentThread());
        await GetService<IActivationService>().ActivateAsync(args).ConfigureAwait(false);

        using var scope = Host.Services.CreateScope();
        var indicatorToMediatorTasks = (from Symbol symbol in Enum.GetValues(typeof(Symbol))
            let serviceIndicatorToMediator = scope.ServiceProvider.GetService<IDataConsumerService>()
            select Task.Run(() => serviceIndicatorToMediator.StartAsync(symbol, _cts.Token), _cts.Token)).ToList();

        var dataProviderService = scope.ServiceProvider.GetRequiredService<IDataProviderService>();
        var dataProviderServiceTask = dataProviderService.StartAsync();

        var executiveProviderService = scope.ServiceProvider.GetRequiredService<IExecutiveProviderService>();
        var executiveProviderServiceTask = executiveProviderService.StartAsync();

        await Task.WhenAll(Task.WhenAll(indicatorToMediatorTasks), dataProviderServiceTask, executiveProviderServiceTask).ConfigureAwait(false);
    }
}