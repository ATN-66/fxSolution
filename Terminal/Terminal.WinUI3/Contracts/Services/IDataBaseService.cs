/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                              IDataBaseService.cs |
  +------------------------------------------------------------------+*/

using Common.DataSource;
using Common.Entities;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataBaseService : IDataBaseSource
{
    Task<IList<HourlyContribution>> GetTicksContributionsAsync();
    Task<IList<HourlyContribution>> GetTicksContributionsAsync(DateTime date);
    Task<IList<Quotation>> GetSampleTicksAsync(IEnumerable<HourlyContribution> hourlyContributions, int yearNumber, int weekNumber);
    Task<int> UpdateContributionsAsync(IEnumerable<long> hourNumbers, bool status);
    Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber);
}