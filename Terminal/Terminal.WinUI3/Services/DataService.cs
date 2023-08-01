/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Messenger.DataService;
using Terminal.WinUI3.Models.Maintenance;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Terminal.WinUI3.Services;

public class DataService : ObservableRecipient, IDataService
{
    private readonly IDataBaseService _dataBaseService;
    private readonly IFileService _fileService;
    private readonly IDataConsumerService _dataConsumerService;
    private readonly CancellationToken _token;
    private IList<YearlyContribution>? _yearlyContributionsCache;
    private int _recalculateProcessedItems;
    private readonly HashSet<DateTime> _excludedDates;
    private readonly HashSet<DateTime> _excludedHours;
    private readonly int _countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;

    public DataService(IConfiguration configuration, IDataBaseService dataBaseService, IFileService fileService, IDataConsumerService dataConsumerService, CancellationTokenSource cancellationTokenSource)
    {
        _dataBaseService = dataBaseService;
        _fileService = fileService;
        _dataConsumerService = dataConsumerService;
        _token = cancellationTokenSource.Token;

        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);
        _excludedHours = new HashSet<DateTime>(configuration.GetSection("ExcludedHours").Get<List<DateTime>>()!);
    }

    public async Task<IDictionary<Symbol, List<Quotation>>> LoadDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        var difference = Math.Ceiling((endDateTimeInclusive - startDateTimeInclusive).TotalHours);
        if (difference < 0)
        {
            throw new InvalidOperationException("Start date cannot be later than end date.");
        }

        IList<Quotation> quotations;
        var result = new Dictionary<Symbol, List<Quotation>>();
        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            result[(Symbol)symbol] = new List<Quotation>();
        }

        var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
        var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour);

        while (start <= end)
        {
            quotations = await _dataBaseService.GetHistoricalDataAsync(start, start, _token).ConfigureAwait(true);

            if (quotations.Count == 0)
            {
                quotations = await _dataConsumerService.GetHistoricalDataAsync(start, start, _token).ConfigureAwait(true);

                if (quotations.Count == 0)
                {
                    quotations = await _fileService.GetHistoricalDataAsync(start, start, _token).ConfigureAwait(true);
                }
            }

            if (quotations.Count == 0)
            {
                if (!HourExcludedPass(start))
                {
                    throw new Exception($"No data for {start}.");
                }
            }

            if (quotations.Count != 0)
            {
                foreach (var symbol in Enum.GetValues(typeof(Symbol)))
                {
                    var filteredQuotations = quotations.Where(quotation => quotation.Symbol == (Symbol)symbol).OrderBy(quotation => quotation.DateTime).ToList();
                    result[(Symbol)symbol].AddRange(filteredQuotations);
                }
            }

            start = start.Add(new TimeSpan(1, 0, 0));
        }

        //todo: remove
        //quotations = await _dataConsumerService.GetBufferedDataAsync(_token).ConfigureAwait(true);
        //if (quotations.Count == 0)
        //{
        //    return result;
        //}
        
        //foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        //{
        //    var filteredQuotations = quotations.Where(quotation => quotation.Symbol == (Symbol)symbol).OrderBy(quotation => quotation.DateTime).ToList();
        //    result[(Symbol)symbol].AddRange(filteredQuotations);
        //}

        return result;
    }
    public Task<(Task, GrpcChannel)> StartAsync(BlockingCollection<Quotation> quotations, CancellationToken token)
    {
        return _dataConsumerService.StartAsync(quotations, token);
    }
    public async Task<IList<Quotation>> GetHistoricalDataAsync(Symbol symbol, DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, Provider provider)
    {
        var quotations = provider switch
        {
            Provider.Mediator => await _dataConsumerService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive, _token).ConfigureAwait(true),
            Provider.FileService => await _fileService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive, _token).ConfigureAwait(true),
            Provider.Terminal => await _dataBaseService.GetHistoricalDataAsync(startDateTimeInclusive, endDateTimeInclusive, _token).ConfigureAwait(true),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Unknown provider.")
        };

        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations.ToList();
    }
    private async Task<IList<Quotation>> GetHistoricalDataAsync(List<DateTime> dateTimes, Provider provider)
    {
        var quotations = provider switch
        {
            Provider.Mediator => await _dataConsumerService.GetHistoricalDataAsync(dateTimes[0], dateTimes[^1].AddHours(1), _token).ConfigureAwait(true),
            Provider.FileService => await _fileService.GetHistoricalDataAsync(dateTimes[0], dateTimes[^1].AddHours(1), _token).ConfigureAwait(true),
            Provider.Terminal => throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Provider must be DataConsumerService or FileService."),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, @"Provider must be DataConsumerService or FileService.")
        };

        return quotations;
    }
    public async Task<IList<YearlyContribution>> GetYearlyContributionsAsync()
    {
        if (_yearlyContributionsCache is not null)
        {
            return _yearlyContributionsCache;
        }

        var list = await _dataBaseService.GetTicksContributionsAsync().ConfigureAwait(true);
        CreateGroups(list);
        return _yearlyContributionsCache!;
    }
    private void CreateGroups(IEnumerable<HourlyContribution> contributions)
    {
        _yearlyContributionsCache = new List<YearlyContribution>();
        var groupedByYear = contributions
            .GroupBy(c => c.DateTime.Year)
            .Select(g => new
            {
                Year = g.Key,
                Months = g.GroupBy(c => c.DateTime.Month)
                    .Select(m => new
                    {
                        Month = m.Key,
                        Days = m.GroupBy(c => c.DateTime.Day).ToList()
                    })
            });

        foreach (var yearGroup in groupedByYear)
        {
            var yearlyContribution = new YearlyContribution
            {
                MonthlyContributions = new ObservableCollection<MonthlyContribution>()
            };

            foreach (var monthGroup in yearGroup.Months)
            {
                var monthlyContribution = new MonthlyContribution
                {
                    DailyContributions = new ObservableCollection<DailyContribution>()
                };

                foreach (var dayGroup in monthGroup.Days)
                {
                    var hourlyContributions = dayGroup.ToList();
                    var dailyContribution = new DailyContribution
                    {
                        HourlyContributions = hourlyContributions
                    };
                    dailyContribution.Contribution = DetermineDailyContributionStatus(dailyContribution);
                    monthlyContribution.DailyContributions.Add(dailyContribution);
                }

                yearlyContribution.MonthlyContributions.Add(monthlyContribution);
            }

            _yearlyContributionsCache.Add(yearlyContribution);
        }
    }
    public async Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime date)
    {
        var year = date.Date.Year;
        var month = date.Date.Month;
        var week = date.Date.Week();
        var day = date.Date.Day;
        var contributions = await _dataBaseService.GetTicksContributionsAsync(date).ConfigureAwait(true);
        var sampleTicks = await _dataBaseService.GetSampleTicksAsync(contributions, year, week).ConfigureAwait(true);
        var symbolicContributions = new List<DailyBySymbolContribution>();
        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            var symbolicContribution = new DailyBySymbolContribution
            {
                Symbol = symbol,
                HourlyContributions = new List<HourlyContribution>()
            };

            for (var hour = 0; hour < 24; hour++)
            {
                var hourlyContribution = new HourlyContribution
                {
                    DateTime = new DateTime(year, month, day, hour, 0, 0),
                    Hour = hour,
                    HasContribution = false
                };

                symbolicContribution.HourlyContributions.Add(hourlyContribution);
            }

            symbolicContributions.Add(symbolicContribution);
        }

        foreach (var tick in sampleTicks)
        {
            var symbolicContribution = symbolicContributions.First(sc => sc.Symbol == tick.Symbol);
            var hourlyContribution = symbolicContribution.HourlyContributions.First(hc => hc.Hour == tick.DateTime.Hour);
            hourlyContribution.HasContribution = true;
        }

        foreach (var symbolicContribution in symbolicContributions)
        {
            symbolicContribution.Contribution = DetermineDailyContributionStatus(symbolicContribution);
        }

        return symbolicContributions;
    }
    public async Task RecalculateAllContributionsAsync(CancellationToken cancellationToken)
    {
        var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions)).Count();
        _recalculateProcessedItems = 0;
        Messenger.Send(new ProgressReportMessage(0), DataServiceToken.Progress);
        await ClearAllContributionsAsync(cancellationToken).ConfigureAwait(true);
        foreach (var yearlyContribution in _yearlyContributionsCache!)
        {
            await ProcessYearlyContributionAsync(yearlyContribution, totalItems, cancellationToken).ConfigureAwait(true);
        }
    }
    private async Task ClearAllContributionsAsync(CancellationToken cancellationToken)
    {
        var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions.SelectMany(d => d.HourlyContributions))).Count();
        var processedItems = 0;
        for (var y = 0; y < _yearlyContributionsCache!.Count; y++)
        {
            var yearlyContribution = _yearlyContributionsCache[y];
            for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
            {
                var monthlyContribution = yearlyContribution.MonthlyContributions[m];
                for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
                {
                    var dailyContribution = monthlyContribution.DailyContributions[d];
                    processedItems = ReportProgressReport(totalItems, processedItems, dailyContribution);
                    var list = new List<long>();
                    for (var h = 0; h < dailyContribution.HourlyContributions.Count; h++)
                    {
                        var hourlyContribution = dailyContribution.HourlyContributions[h];
                        Messenger.Send(new InfoMessage(hourlyContribution.DateTime.ToString("F")), DataServiceToken.Info);
                        hourlyContribution.HasContribution = false;
                        list.Add(hourlyContribution.Hour);
                    }
                    var result = await _dataBaseService.UpdateContributionsAsync(list, false).ConfigureAwait(true);
                    Debug.Assert(result == 24);
                    UpdateStatus(dailyContribution);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }
    }
    private async Task ProcessYearlyContributionAsync(YearlyContribution yearlyContribution, int totalItems, CancellationToken cancellationToken)
    {
        for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
        {
            var monthlyContribution = yearlyContribution.MonthlyContributions[m];
            await ProcessMonthlyContributionAsync(monthlyContribution, totalItems, cancellationToken).ConfigureAwait(true);
        }
    }
    private async Task ProcessMonthlyContributionAsync(MonthlyContribution monthlyContribution, int totalItems, CancellationToken cancellationToken)
    {
        for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
        {
            var dailyContribution = monthlyContribution.DailyContributions[d];
            Interlocked.Increment(ref _recalculateProcessedItems);
            var progressPercentage = _recalculateProcessedItems * 100 / totalItems;
            Messenger.Send(new ProgressReportMessage(progressPercentage), DataServiceToken.Progress);

            await ProcessDailyContributionAsync(dailyContribution, cancellationToken).ConfigureAwait(true);
        }
    }
    private async Task ProcessDailyContributionAsync(DailyContribution dailyContribution, CancellationToken cancellationToken)
    {
        var tasks = dailyContribution.HourlyContributions.Select(ProcessHourlyContributionAsync).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(true);
        UpdateStatus(dailyContribution);
        cancellationToken.ThrowIfCancellationRequested();
    }
    private async Task ProcessHourlyContributionAsync(HourlyContribution hourlyContribution)
    {
        Messenger.Send(new InfoMessage(hourlyContribution.DateTime.ToString("D")), DataServiceToken.Info);
        var sampleTicksResult = await _dataBaseService.GetSampleTicksAsync(new[] { hourlyContribution }, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
        var sampleTicks = sampleTicksResult.ToList();
        if (sampleTicks.Count == 0)
        {
            hourlyContribution.HasContribution = false;
            var result = await _dataBaseService.UpdateContributionsAsync(new[] { hourlyContribution.Hour }, false).ConfigureAwait(true);
            Debug.Assert(result == 1);
        }
        else
        {
            var ticksHourly = sampleTicks.GroupBy(q => new DateTime(q.DateTime.Year, q.DateTime.Month, q.DateTime.Day, q.DateTime.Hour, 0, 0)).ToList();
            Debug.Assert(ticksHourly.Count == 1);
            var hourNumbers = (from @group in ticksHourly
                               let distinctSymbolsInGroup = @group.Select(q => q.Symbol).Distinct().Count()
                               where distinctSymbolsInGroup == _countOfSymbols
                               select (long)(@group.Key - DateTimeExtensionsAndHelpers.EpochStartDateTimeUtc).TotalHours).ToList();
            Debug.Assert(hourNumbers.Count == 1);
            hourlyContribution.HasContribution = true;
            var result = await _dataBaseService.UpdateContributionsAsync(new[] { hourlyContribution.Hour }, true).ConfigureAwait(true);
            Debug.Assert(result == 1);
        }
    }
    public async Task<string> ReImportSelectedAsync(DateTime dateTime, Provider provider)
    {
        var contributions = await GetYearlyContributionsAsync().ConfigureAwait(true);

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

                    _ = await _dataBaseService.DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.Week).ConfigureAwait(true);
                    var quotations = await GetHistoricalDataAsync(dateTimes, provider).ConfigureAwait(true);
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
                        var updateContributionsResult = await _dataBaseService.UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);
                        if (updateContributionsResult != 1)
                        {
                            throw new InvalidOperationException("updateContributionsResult != 1");
                        }
                        dailyContribution.HourlyContributions[hour].HasContribution = true;
                    }

                    if (quotations.Count != counter)
                    {
                        throw new InvalidOperationException("quotations.TicksCount != counter");
                    }

                    monthlyContribution.DailyContributions[d].Contribution = DetermineDailyContributionStatus(dailyContribution);
                    var list = await _dataBaseService.GetTicksContributionsAsync().ConfigureAwait(true);
                    CreateGroups(list);
                    return $"{dailyContribution.HourlyContributions[0].DateTime:D} was updated. {counter} were saved.";
                }
            }
        }

        throw new InvalidOperationException("Must never be here!");
    }
    public async Task ImportAsync(Provider provider, CancellationToken cancellationToken)
    {
        var processedItems = 0;
        var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions.SelectMany(d => d.HourlyContributions))).Count();

        for (var y = 0; y < _yearlyContributionsCache!.Count; y++)
        {
            var yearlyContribution = _yearlyContributionsCache[y];
            var year = yearlyContribution.Year;

            for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
            {
                var monthlyContribution = yearlyContribution.MonthlyContributions[m];
                var month = monthlyContribution.Month;

                for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
                {
                    var dailyContribution = monthlyContribution.DailyContributions[d];
                    var day = dailyContribution.Day;

                    processedItems = ReportProgressReport(totalItems, processedItems, dailyContribution);
                    Messenger.Send(new InfoMessage(dailyContribution.HourlyContributions[0].DateTime.ToString("dddd, MMMM d, yyyy, HH:mm")), DataServiceToken.Info);

                    if (dailyContribution.Contribution is Contribution.Excluded or Contribution.Full)
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

                    if (year != dateTimes[0].Year || month != dateTimes[0].Month || day != dateTimes[0].Day)
                    {
                        throw new Exception("year != dateTimes[0].Year || month != dateTimes[0].Month || day != dateTimes[0].Day");
                    }

                    var updateContributionsResult = await _dataBaseService.UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true);

                    if (updateContributionsResult != 24)
                    {
                        throw new Exception("updateContributionsResult != 24");
                    }

                    _ = await _dataBaseService.DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                    var quotations = await GetHistoricalDataAsync(dateTimes, provider).ConfigureAwait(true);

                    if (year != quotations[0].DateTime.Year || month != quotations[0].DateTime.Month || day != quotations[0].DateTime.Day)
                    {
                        throw new Exception("year != quotations[0].DateTime.Year || month != quotations[0].DateTime.Month || day != quotations[0].DateTime.Day");
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
                            continue;
                        }
                        var quotationsForThisHour = hourGroup.SelectMany(group => group).OrderBy(q => q.DateTime).ToList();
                        var insertTicksResult = await _dataBaseService.SaveDataAsync(quotationsForThisHour).ConfigureAwait(true);
                        counter += insertTicksResult;
                        
                        updateContributionsResult = await _dataBaseService.UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);

                        if (updateContributionsResult != 1)
                        {
                            throw new Exception("updateContributionsResult != 1");
                        }

                        dailyContribution.HourlyContributions[hour].HasContribution = true;
                        UpdateStatus(dailyContribution);
                    }

                    if (quotations.Count != counter)
                    {
                        throw new Exception("quotations.TicksCount != counter");
                    }

                    if (cancellationToken.IsCancellationRequested) { return; }
                }
            }
        }
    }
    private Contribution DetermineDailyContributionStatus(DailyContribution dailyContribution)
    {
        if (_excludedDates.Contains(dailyContribution.HourlyContributions[0].DateTime.Date) || dailyContribution.HourlyContributions[0].DateTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return Contribution.Excluded;
        }

        switch (dailyContribution.HourlyContributions[0].DateTime.DayOfWeek)
        {
            case DayOfWeek.Friday:
                if (dailyContribution.HourlyContributions.SkipLast(2).All(c => c.HasContribution))
                {
                    return Contribution.Full;
                }
                if (dailyContribution.HourlyContributions.SkipLast(3).All(c => c.HasContribution) && _excludedHours.Contains(dailyContribution.HourlyContributions[^3].DateTime))
                {
                    return Contribution.Full;
                }
                return dailyContribution.HourlyContributions.Any(c => c.HasContribution) ? Contribution.Partial : Contribution.None;
            case DayOfWeek.Sunday when dailyContribution.HourlyContributions.TakeLast(2).All(c => c.HasContribution): return Contribution.Full;
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Saturday:
            default:
                {
                    if (dailyContribution.HourlyContributions.All(c => c.HasContribution))
                    {
                        return Contribution.Full;
                    }

                    return dailyContribution.HourlyContributions.Any(c => c.HasContribution) ? Contribution.Partial : Contribution.None;
                }
        }
    }
    private bool HourExcludedPass(DateTime dateTime)
    {
        bool dateExists;

        switch (dateTime.DayOfWeek)
        {
            case DayOfWeek.Friday:
            {
                dateExists = _excludedHours.Any(dt => dt.Date == dateTime.Date);
                if (!dateExists)
                {
                    return false;
                }

                var today = _excludedHours.FirstOrDefault(dt => dt.Date == dateTime.Date);
                var nextDay = today.AddDays(1).Date;
                return dateTime >= today && dateTime <= nextDay;
            }
            case DayOfWeek.Saturday: return true;
            case DayOfWeek.Sunday:
                if (dateTime.Hour <= 21)
                {
                    return true;
                }
                break;
            case DayOfWeek.Monday:
                break;
            case DayOfWeek.Tuesday:
                break;
            case DayOfWeek.Wednesday:
                break;
            case DayOfWeek.Thursday:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        dateExists = _excludedDates.Any(dt => dt.Date == dateTime.Date);
        return dateExists;
    }
    private int ReportProgressReport(int totalItems, int processedItems, DailyContribution dailyContribution)
    {
        processedItems += dailyContribution.HourlyContributions.Count;
        var progressPercentage = processedItems * 100 / totalItems;
        Messenger.Send(new ProgressReportMessage(progressPercentage), DataServiceToken.Progress);
        return processedItems;
    }
    private void UpdateStatus(DailyContribution dailyContribution)
    {
        dailyContribution.Contribution = DetermineDailyContributionStatus(dailyContribution);
        Messenger.Send(new DailyContributionChangedMessage(dailyContribution), DataServiceToken.DataToUpdate);
    }
    public Task<object?> BackupAsync()
    {
        throw new NotImplementedException();
    }
    public Task<object?> RestoreAsync()
    {
        throw new NotImplementedException();
    }
}