/*+------------------------------------------------------------------+
  |                                        Terminal.WinUI3.ViewModels|
  |                                           FileSourceViewModel.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Common.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.SqlServer.Server;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Contracts.ViewModels;

namespace Terminal.WinUI3.ViewModels;

public partial class FileSourceViewModel : ObservableRecipient, INavigationAware
{
    private readonly IDataService _dataService;
    private readonly IDispatcherService _dispatcherService;

    public List<Symbol> Symbols { get; } = Enum.GetValues(typeof(Symbol)).Cast<Symbol>().ToList();
    [ObservableProperty] private ObservableCollection<Quotation> _quotations = new();
    [ObservableProperty] private string _headerContext = "File Source";
    [ObservableProperty] private DateTimeOffset _selectedDate;
    [ObservableProperty] private TimeSpan _selectedTime;
    [ObservableProperty] private Symbol _selectedSymbol;
    [ObservableProperty] private bool _isLoading;

    public FileSourceViewModel(IDataService dataService, IConfiguration configuration, IDispatcherService dispatcherService)
    {
        _dataService = dataService;
        _dispatcherService = dispatcherService;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };
        _selectedDate = DateTimeOffset.ParseExact(configuration.GetValue<string>("StartDate")!, formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();

        SubmitCommand = new AsyncRelayCommand(SubmitAsync);
    }

    public IAsyncRelayCommand SubmitCommand
    {
        get;
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task SubmitAsync()
    {
        IsLoading = true;
        var input = await _dataService.GetTicksAsync(SelectedSymbol, SelectedDate, SelectedTime).ConfigureAwait(true);
        Debug.Assert(_dispatcherService.HasThreadAccess);

        Quotations.Clear();
        foreach (var quotation in input)
        {
            Quotations.Add(quotation);
        }
        IsLoading = false;
    }
}