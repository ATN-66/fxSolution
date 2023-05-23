using Windows.UI.ViewManagement;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Dispatching;
using Terminal.WinUI3.Helpers;
using static Windows.Win32.PInvoke;
using System.Runtime.InteropServices;
using Windows.Win32.Graphics.Gdi;
using Microsoft.UI.Windowing;
using WinUIEx;
using Microsoft.UI.Xaml.Input;

namespace Terminal.WinUI3;

public sealed partial class MainWindow
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;
    public static AppWindow MainAppWindow;

    public MainWindow()
    {
        InitializeComponent();

        MainAppWindow = AppWindowExtensions.GetAppWindow(this);

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/WindowIcon.ico"));
        Content = null;
        Title = "AppDisplayName".GetLocalized();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _settings = new UISettings();
        _settings.ColorValuesChanged += Settings_ColorValuesChanged; // cannot use FrameworkElement.ActualThemeChanged event

        HWND hwnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(this);
        SetWindowSize(hwnd, 1050, 800);
        PlacementCenterWindowInMonitorWin32(hwnd);
    }
    
    private void Settings_ColorValuesChanged(UISettings sender, object args) => _dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);

    private void SetWindowSize(HWND hwnd, int width, int height)
    {
        uint dpi = GetDpiForWindow(hwnd);
        float scalingFactor = (float)dpi / 96;
        width = (int)(width * scalingFactor);
        height = (int)(height * scalingFactor);

        SetWindowPos(hwnd, default, 0, 0, width, height, SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
    }

    private void PlacementCenterWindowInMonitorWin32(HWND hwnd)
    {
        RECT windowMonitorRectToAdjust;
        GetWindowRect(hwnd, out windowMonitorRectToAdjust);
        ClipOrCenterRectToMonitorWin32(ref windowMonitorRectToAdjust);
        SetWindowPos(hwnd, default, windowMonitorRectToAdjust.left, windowMonitorRectToAdjust.top, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private void ClipOrCenterRectToMonitorWin32(ref RECT prc)
    {
        HMONITOR hMonitor;
        RECT rc;
        int w = prc.right - prc.left;
        int h = prc.bottom - prc.top;

        hMonitor = MonitorFromRect(prc, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        MONITORINFO mi = new MONITORINFO();
        mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();

        GetMonitorInfo(hMonitor, ref mi);

        rc = mi.rcWork;
        prc.left = rc.left + (rc.right - rc.left - w) / 2;
        prc.top = rc.top + (rc.bottom - rc.top - h) / 2;
        prc.right = prc.left + w;
        prc.bottom = prc.top + h;
    }

    private void MyWindowIcon_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        this.Close();
    }
}