/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                    AccountPropertiesViewModel.cs |
  +------------------------------------------------------------------+*/

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;
using Terminal.WinUI3.Helpers;
using Terminal.WinUI3.Models.Account;

namespace Terminal.WinUI3.ViewModels;

public partial class AccountPropertiesViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private AccountInfo  _accountInfo;

    public AccountPropertiesViewModel(IAccountService accountService)
    {
        _accountInfo = accountService.GetAccountInfo();
    }

    public string? TradeModeDescription => AccountInfo.TradeMode.GetDescription();
    public string? StopOutModeDescription => AccountInfo.StopOutMode.GetDescription();
    public string? MarginModeDescription => AccountInfo.MarginMode.GetDescription();

    public static string GetDescription(string propertyName)
    {
        var prop = typeof(AccountInfo).GetProperty(propertyName);
        if (prop == null)
        {
            return string.Empty;
        }

        var attr = (DescriptionAttribute)prop.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault()!;
        return attr.Description;
    }

    public void OnNavigatedTo(object parameter)
    {
        // Run code when the app navigates to this page
    }

    public void OnNavigatedFrom()
    {
        // Run code when the app navigates away from this page
    }
}