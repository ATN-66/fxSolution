/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using Common.Entities;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{    
    Task<ObservableCollection<YearlyContribution>> GetYearlyContributionsAsync();
    Task<ObservableCollection<SymbolicContribution>> GetSymbolicContributionsAsync(DateTimeOffset selectedDate);
    Task RecalculateTicksContributionsAsync(CancellationToken cancellationToken);
    Task ImportTicksAsync(CancellationToken cancellationToken);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week);
    Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year);
    Task<IEnumerable<Quotation>> GetTicksAsync(Symbol symbol, DateTimeOffset dateTimeOffset, TimeSpan selectedTime);
}