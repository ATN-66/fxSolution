using CommunityToolkit.WinUI;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        //Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
        //    Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
        //    () =>
        //    {
                
        //    });
    }
}