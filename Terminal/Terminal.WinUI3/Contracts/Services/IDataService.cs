/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                           IDataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{
    Task<IEnumerable<Quotation>> GetHistoricalDataAsync(Symbol symbol, DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider);
    Task<IList<YearlyContribution>> GetYearlyContributionsAsync();
    Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime dateTime);
    Task ImportAsync(CancellationToken token, Provider provider);
    Task<string> ReImportSelectedAsync(DateTime dateTime, Provider provider);
    Task RecalculateAllContributionsAsync(CancellationToken token);
    Task<object?> BackupAsync();
    Task<object?> RestoreAsync();
}
