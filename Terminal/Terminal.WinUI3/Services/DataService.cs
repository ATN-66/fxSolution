/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Common.ExtensionsAndHelpers;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Services;

public class DataService : IDataService
{
    private readonly IDataBaseService _dataBaseService;
    private readonly IFileService _fileService;
    private readonly IMediator _mediator;

    private readonly int _countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;

    public DataService(IDataBaseService dataBaseService, IFileService fileService, IMediator mediator)
    {
        _dataBaseService = dataBaseService;
        _fileService = fileService;
        _mediator = mediator;
    }

    public Task<IList<YearlyContribution>> GetYearlyContributionsAsync()
    {
        return _dataBaseService.GetYearlyContributionsAsync();
    }
    public Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime dateTime)
    {
        return _dataBaseService.GetDayContributionAsync(dateTime);
    }

    public Task RecalculateAllContributionsAsync(CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public Task<object?> BackupAsync()
    {
        throw new NotImplementedException();
    }

    public Task<object?> RestoreAsync()
    {
        throw new NotImplementedException();
    }


    public async Task<IEnumerable<Quotation>> GetHistoricalDataAsync(Symbol symbol, DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider)
    {
        var input = provider switch
        {
            Provider.Mediator => new List<Quotation>(), // await _mediator.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false),
            Provider.FileService => await _fileService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false),
            Provider.Terminal => new List<Quotation>(), //await _dataBaseService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Unknown provider.")
        };

        IList<Quotation> quotations = input.ToList();
        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations;
    }





    public async Task<string> ReImportSelectedAsync(DateTime dateTime, Provider provider)
    {
        var contributions = await _dataBaseService.GetYearlyContributionsAsync().ConfigureAwait(true);

        var year = dateTime.Year;
        var month = dateTime.Month;
        var day = dateTime.Day;

        for (var y = 0; y < contributions.Count; y++)
        {
            var yearlyContribution = contributions[y];
            for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
            {
                var monthlyContribution = yearlyContribution.MonthlyContributions[m];
                for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
                {
                    var dailyContribution = monthlyContribution.DailyContributions[d];
                    if (year != dailyContribution.Year || month != dailyContribution.Month || day != dailyContribution.Day)
                    {
                        continue;
                    }

                    var hourNumbers = new List<long>();
                    var dateTimes = new List<DateTime>();
                    for (var h = 0; h < dailyContribution.HourlyContributions.Count; h++)
                    {
                        var hourlyContribution = dailyContribution.HourlyContributions[h];
                        hourlyContribution.HasContribution = false;
                        hourNumbers.Add(hourlyContribution.Hour);
                        dateTimes.Add(hourlyContribution.DateTime);
                    }
                    if (await _dataBaseService.UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true) != 24)
                    {
                        throw new InvalidOperationException("updateContributionsResult != 24");
                    }
                    
                    _ = await _dataBaseService.DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);

                    IList<Quotation> quotations;
                    switch (provider)
                    {
                        case Provider.Mediator: quotations = await _mediator.GetHistoricalDataAsync(dateTimes[0], dateTimes[^1].AddHours(1)).ConfigureAwait(true); break;
                        case Provider.FileService: quotations = await _fileService.GetHistoricalDataAsync(dateTimes[0], dateTimes[^1].AddHours(1)).ConfigureAwait(true); break;
                        case Provider.Terminal:
                        default: throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Provider must be Mediator or FileService.");
                    }

                    var groupedQuotations = quotations.GroupBy(q => new QuotationKey { Symbol = q.Symbol, Hour = q.DateTime.Hour, });
                    var sortedGroups = groupedQuotations.OrderBy(g => g.Key.Hour).ThenBy(g => g.Key.Symbol);
                    var groupsGroupedByHour = sortedGroups.GroupBy(g => g.Key.Hour);
                    var counter = 0;
                    foreach (var hourGroup in groupsGroupedByHour)
                    {
                        var hour = hourGroup.Key;
                        var symbolsInHour = hourGroup.Count();
                        if (_countOfSymbols != symbolsInHour)
                        {
                            throw new InvalidOperationException("_countOfSymbols != symbolsInHour");
                        }

                        var quotationsForThisHour = hourGroup.SelectMany(group => group).OrderBy(q => q.DateTime).ToList();
                        var insertTicksResult = await _dataBaseService.SaveDataAsync(quotationsForThisHour).ConfigureAwait(true);
                        counter += insertTicksResult;

                      

                        //updateContributionsResult = await UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);
                        //Debug.Assert(updateContributionsResult == 1);
                        //dailyContribution.HourlyContributions[hour].HasContribution = true;
                    }
                    //Debug.Assert(quotations.Count == counter);
                    //monthlyContribution.DailyContributions[d].Contribution = DetermineContributionStatus(dailyContribution);
                    //_logger.LogInformation("{date} was updated. {count} were saved.", dailyContribution.HourlyContributions[0].DateTime.ToString(), counter.ToString());
                    //return 0;
                }
            }
        }

        throw new InvalidOperationException("Never should be here.");
    }
    public async Task ImportAsync(CancellationToken cancellationToken, Provider provider)
    {
        throw new NotImplementedException();

        //var processedItems = 0;
        //var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions.SelectMany(d => d.HourlyContributions))).Count();

        //for (var y = 0; y < _yearlyContributionsCache!.Count; y++)
        //{
        //    var yearlyContribution = _yearlyContributionsCache[y];
        //    var year = yearlyContribution.Year;

        //    for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
        //    {
        //        var monthlyContribution = yearlyContribution.MonthlyContributions[m];
        //        var month = monthlyContribution.Month;

        //        for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
        //        {
        //            var dailyContribution = monthlyContribution.DailyContributions[d];
        //            var day = dailyContribution.Day;

        //            processedItems = ReportProgressReport(totalItems, processedItems, dailyContribution);
        //            Messenger.Send(new InfoMessage(dailyContribution.HourlyContributions[0].DateTime.ToString(Format)), DataServiceToken.Info);

        //            if (dailyContribution.Contribution is Contribution.Excluded or Contribution.Full)
        //            {
        //                continue;
        //            }

        //            var hourNumbers = new List<long>();
        //            var dateTimes = new List<DateTime>();
        //            for (var h = 0; h < dailyContribution.HourlyContributions.Count; h++)
        //            {
        //                var hourlyContribution = dailyContribution.HourlyContributions[h];
        //                hourlyContribution.HasContribution = false;
        //                hourNumbers.Add(hourlyContribution.Hour);
        //                dateTimes.Add(hourlyContribution.DateTime);
        //            }

        //            Debug.Assert(year == dateTimes[0].Year);
        //            Debug.Assert(month == dateTimes[0].Month);
        //            Debug.Assert(day == dateTimes[0].Day);

        //            var updateContributionsResult = await UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true);
        //            Debug.Assert(updateContributionsResult == 24);
        //            var deleteTicksResult = await DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
        //            _logger.Log(LogLevel.Information, "Import:{count} ticks deleted.", deleteTicksResult);

        //            var input = await _externalDataSource.GetHistoricalDataAsync(dateTimes[0], dateTimes[^1].AddHours(1), provider).ConfigureAwait(true);
        //            var quotations = input.ToList();

        //            Debug.Assert(year == quotations[0].DateTime.Year);
        //            Debug.Assert(month == quotations[0].DateTime.Month);
        //            Debug.Assert(day == quotations[0].DateTime.Day);

        //            var groupedQuotations = quotations.GroupBy(q => new QuotationKey { Symbol = q.Symbol, Hour = q.DateTime.Hour, });
        //            var sortedGroups = groupedQuotations.OrderBy(g => g.Key.Hour).ThenBy(g => g.Key.Symbol);
        //            var groupsGroupedByHour = sortedGroups.GroupBy(g => g.Key.Hour);
        //            var counter = 0;
        //            foreach (var hourGroup in groupsGroupedByHour)
        //            {
        //                var hour = hourGroup.Key;
        //                var symbolsInHour = hourGroup.Count();
        //                var quotationsForThisHour = hourGroup.SelectMany(group => group).OrderBy(q => q.DateTime).ToList();
        //                var insertTicksResult = await SavesTicksAsync(quotationsForThisHour, dailyContribution.Year, dailyContribution.Week).ConfigureAwait(true);
        //                counter += insertTicksResult;

        //                if (_countOfSymbols != symbolsInHour)
        //                {
        //                    continue;
        //                }

        //                updateContributionsResult = await UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);
        //                Debug.Assert(updateContributionsResult == 1);
        //                dailyContribution.HourlyContributions[hour].HasContribution = true;
        //                UpdateStatus(dailyContribution);

        //            }
        //            Debug.Assert(quotations.Count == counter);
        //            _logger.LogInformation("Import: {date} was updated. {count} were saved.", dailyContribution.HourlyContributions[0].DateTime.ToString(Format), counter.ToString());
        //            if (cancellationToken.IsCancellationRequested) { return; }
        //        }
        //    }
        //}
    }
}