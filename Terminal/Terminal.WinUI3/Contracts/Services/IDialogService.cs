/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                IDialogService.cs |
  +------------------------------------------------------------------+*/

using Microsoft.UI.Xaml.Controls;
using Terminal.WinUI3.ViewModels;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDialogService
{
    ContentDialog CreateDialog(DialogViewModel viewModel, string title, string primaryButtonText, string? secondaryButtonText, string? closeButtonText);
}