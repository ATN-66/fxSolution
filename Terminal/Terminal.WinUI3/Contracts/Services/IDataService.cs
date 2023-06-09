/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{    
    Task<List<HourlyContribution>> GetAllTicksContributionsAsync();
    Task UpdateTicksContributionsAsync(CancellationToken ctsToken);
    Task ImportTicksAsync(List<YearlyContribution> yearlyContributions, CancellationToken cancellationToken);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week);
    Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year);
}