using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class SplashScreenService : ISplashScreenService
{
    private readonly Models.SplashScreen.SplashScreen _mSc = new();

    public void DisplaySplash()
    {
        _mSc.Initialize();
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        _mSc.DisplaySplash(hWnd, _mSc.GetBitmap(@"Assets\cad.png"));
    }

    public void HideSplash()
    {
        _mSc.HideSplash(5);
    }
}