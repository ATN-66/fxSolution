/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{    
    Task<IEnumerable<YearlyContribution>> GetYearlyContributionsAsync();
    Task<IEnumerable<SymbolicContribution>> GetSymbolicContributionsAsync(DateTimeOffset selectedDate);
    Task RecalculateTicksContributionsSelectedDayAsync(DateTime dateTime, CancellationToken cancellationToken);
    Task RecalculateTicksContributionsAllAsync(CancellationToken cancellationToken);
    Task ImportTicksAsync(CancellationToken cancellationToken);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week);
    Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year);
    Task<IEnumerable<Quotation>> GetImportTicksAsync(Symbol symbol, DateTime startDateTime, DateTime endDateTime);
}