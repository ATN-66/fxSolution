/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                 DialogService.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Services;

public class DialogService : IDialogService
{
    private readonly IWindowService _windowService;

    public DialogService(IWindowService windowService)
    {
        _windowService = windowService;
    }
    
    public ContentDialog CreateDialog(DialogViewModel viewModel, string title, string primaryButtonText, string? secondaryButtonText, string? closeButtonText)
    {
        var content = new StackPanel();

        var cautionMessage = new TextBlock();
        cautionMessage.SetBinding(TextBlock.TextProperty, new Binding
        {
            Source = viewModel,
            Path = new PropertyPath("CautionMessage"),
            Mode = BindingMode.OneWay
        });
        content.Children.Add(cautionMessage);

        var infoMessage = new TextBlock();
        infoMessage.SetBinding(TextBlock.TextProperty, new Binding
        {
            Source = viewModel,
            Path = new PropertyPath("InfoMessage"),
            Mode = BindingMode.OneWay
        });
        content.Children.Add(infoMessage);

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = 20,

            Margin = new Thickness(0, 10, 0, 0)
        };
        progressBar.SetBinding(RangeBase.ValueProperty, new Binding
        {
            Source = viewModel,
            Path = new PropertyPath("ProgressPercentage"),
            Mode = BindingMode.OneWay
        });

        content.Children.Add(progressBar);

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            SecondaryButtonText = secondaryButtonText,
            CloseButtonText = closeButtonText,
            XamlRoot = _windowService.CurrentWindow.Content.XamlRoot
        };

        return dialog;
    }

    //public async Task<ContentDialogResult> ShowDialogAsync(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText)
    //{
    //    var dialog = new ContentDialog
    //    {
    //        Title = title,
    //        Content = content,
    //        PrimaryButtonText = primaryButtonText,
    //        SecondaryButtonText = secondaryButtonText,
    //        CloseButtonText = closeButtonText,
    //        XamlRoot = _windowService.CurrentWindow.Content.XamlRoot
    //    };

    //    return await dialog.ShowAsync();
    //}

    //public ContentDialog CreateDialog(string title, string content, string primaryButtonText, string? secondaryButtonText, string? closeButtonText)
    //{
    //    var dialog = new ContentDialog
    //    {
    //        Title = title,
    //        Content = content,
    //        PrimaryButtonText = primaryButtonText,
    //        SecondaryButtonText = secondaryButtonText,
    //        CloseButtonText = closeButtonText,
    //        XamlRoot = _windowService.CurrentWindow.Content.XamlRoot
    //    };

    //    return dialog;
    //}
}