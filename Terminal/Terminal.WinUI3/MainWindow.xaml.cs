using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Terminal.WinUI3.Helpers;
using WinRT.Interop;
using static Windows.Win32.PInvoke;

namespace Terminal.WinUI3;

public sealed partial class MainWindow
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _settings;
    public static AppWindow MainAppWindow = null!;
    private const uint DOT_KEY = 0x1B; // VK_ESCAPE	0x1B ESC key
    private const uint WM_HOTKEY = 0x0312;
    private WNDPROC origPrc;
    private WNDPROC hotKeyPrc;

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

        //HWND hwnd = (HWND)WindowNative.GetWindowHandle(this);
        //SetWindowSize(hwnd, 1050, 800);//todo: check
        //PlacementCenterWindowInMonitorWin32(hwnd);

        //hwnd = new HWND(WindowNative.GetWindowHandle(this).ToInt32());
        //var success = RegisterHotKey(hwnd, 0, HOT_KEY_MODIFIERS.MOD_WIN | HOT_KEY_MODIFIERS.MOD_CONTROL, DOT_KEY);

        //hotKeyPrc = HotKeyPrc;
        //var hotKeyPrcPointer = Marshal.GetFunctionPointerForDelegate(hotKeyPrc);
        //origPrc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, hotKeyPrcPointer));
    }

    private void Settings_ColorValuesChanged(UISettings sender, object args) => _dispatcherQueue.TryEnqueue(TitleBarHelper.ApplySystemThemeToCaptionButtons);
    
    private LRESULT HotKeyPrc(HWND hwnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        if (uMsg == WM_HOTKEY)
        {
            if (!Visible)
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
                PInvoke.SetForegroundWindow(hwnd);
                PInvoke.SetFocus(hwnd);
                PInvoke.SetActiveWindow(hwnd);
            }
            else
            {
                PInvoke.ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
            }

            return (LRESULT)IntPtr.Zero;
        }

        return PInvoke.CallWindowProc(origPrc, hwnd, uMsg, wParam, lParam);
    }

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
        Close();
    }
}