/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                IDialogService.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Controls;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDialogService
{
    Task<ContentDialogResult> ShowDialogAsync(string title, string content, string primaryButtonText, string secondaryButtonText, string closeButtonText);
}