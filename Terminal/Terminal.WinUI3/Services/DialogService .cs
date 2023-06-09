/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                 DialogService.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.Services;

public class DialogService : IDialogService
{
    private readonly IWindowService _windowService;

    public DialogService(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public async Task<ContentDialogResult> ShowDialogAsync(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText,
            XamlRoot = _windowService.CurrentWindow.Content.XamlRoot
        };

        return await dialog.ShowAsync();
    }
}