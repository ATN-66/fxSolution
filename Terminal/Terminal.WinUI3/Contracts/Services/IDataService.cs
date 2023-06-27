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
    Task<IEnumerable<DailyBySymbolContribution>> GetDayContributionAsync(DateTime selectedDate);
    Task<IEnumerable<Quotation>> GetTicksAsync(Symbol symbol, DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider = Provider.Terminal, bool exactly = false);

    Task<Contribution> ReImportSelectedAsync(DateTime dateTime);
    Task ImportAsync(CancellationToken cancellationToken);
    
    Task RecalculateAllContributionsAsync(CancellationToken ctsToken);

    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week);
    Task<int> BackupAsync();
    Task<int> RestoreAsync();
}