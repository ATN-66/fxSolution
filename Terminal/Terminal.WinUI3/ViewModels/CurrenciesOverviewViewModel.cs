/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                   CurrenciesOverviewViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public partial class CurrenciesOverviewViewModel : ObservableRecipient, INavigationAware
{
    private readonly ICurrencyViewModelFactory _currencyViewModelFactory;
    private readonly ILogger<CurrenciesOverviewViewModel> _logger;

    [ObservableProperty] private int _centuriesPercent;
    [ObservableProperty] private int _unitsPercent;
    [ObservableProperty] private int _kernelShiftPercent;

    public ObservableCollection<CurrencyViewModel> CurrencyViewModels { get; set; } = new();

    public CurrenciesOverviewViewModel(IConfiguration configuration, ICurrencyViewModelFactory currencyViewModelFactory, ILogger<CurrenciesOverviewViewModel> logger)
    {
        _currencyViewModelFactory = currencyViewModelFactory;
        _logger = logger;

        _centuriesPercent = configuration.GetValue<int>($"{nameof(_centuriesPercent)}");
        _unitsPercent = configuration.GetValue<int>($"{nameof(_unitsPercent)}");
        _kernelShiftPercent = configuration.GetValue<int>($"{nameof(_kernelShiftPercent)}");
    }

    partial void OnCenturiesPercentChanged(int value)
    {
        foreach (var model in CurrencyViewModels)
        {
            model.CenturiesPercent = value;
        }
    }

    partial void OnUnitsPercentChanged(int value)
    {
        foreach (var model in CurrencyViewModels)
        {
            model.UnitsPercent = value;
        }
    }

    partial void OnKernelShiftPercentChanged(int value)
    {
        foreach (var model in CurrencyViewModels)
        {
            model.KernelShiftPercent = value;
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        try
        {
            CurrencyViewModels.Clear();
            var input = parameter.ToString();
            var sets = input!.Split(';');
            for (var i = 0; i < sets.Length; i++)
            {
                sets[i] = sets[i].Trim('[', ']');
            }

            foreach (var set in sets)
            {
                var model = _currencyViewModelFactory.Create();
                model.OnNavigatedTo(set);
                CurrencyViewModels.Add(model);
            }

        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, "");
            throw;
        }
    }

    public void OnNavigatedFrom()
    {
        foreach (var model in CurrencyViewModels)
        {
            model.OnNavigatedFrom();
        }
    }
}