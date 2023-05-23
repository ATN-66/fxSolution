using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace Terminal.WinUI3.Helpers;

public static class AppWindowExtensions
{
    public static AppWindow GetAppWindow(this Window window)
    {
        var windowHandle = WindowNative.GetWindowHandle(window);
        return GetAppWindowFromWindowHandle(windowHandle);
    }

    private static AppWindow GetAppWindowFromWindowHandle(IntPtr windowHandle)
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        return AppWindow.GetFromWindowId(windowId);
    }
}