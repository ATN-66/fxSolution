/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                              IDataBaseService.cs |
  +------------------------------------------------------------------+*/

using Common.DataSource;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataBaseService : IDataBaseSource
{
    Task<IList<YearlyContribution>> GetYearlyContributionsAsync();
    Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime selectedDate);
    Task<int> UpdateContributionsAsync(IEnumerable<long> hourNumbers, bool status);
    Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber);

    Task RecalculateAllContributionsAsync(CancellationToken ctsToken);

    //Task<Contribution> ReImportSelectedAsync(DateTime dateTime, Provider provider);
    //Task ImportAsync(CancellationToken cancellationToken, Provider provider);
}