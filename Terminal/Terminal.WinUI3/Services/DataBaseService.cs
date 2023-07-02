/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                               DataBaseService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Common.DataSource;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;
using DateTime = System.DateTime;

namespace Terminal.WinUI3.Services;

public class DataBaseService : DataBaseSource, IDataBaseService
{
    private readonly SolutionDataBaseSettings _solutionDataBaseSettings;

    private IList<YearlyContribution>? _yearlyContributionsCache;
    private readonly int _countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;

    private readonly HashSet<DateTime> _excludedDates;
    private readonly HashSet<DateTime> _excludedHours;

    private const string Format = "dddd, MMMM d, yyyy";//todo

    public DataBaseService(IConfiguration configuration, IOptions<ProviderBackupSettings> providerBackupSettings, IOptions<SolutionDataBaseSettings> solutionDataBaseSettings, ILogger<IDataSource> logger, IAudioPlayer audioPlayer) : base(configuration, providerBackupSettings, logger, audioPlayer)
    {
        _solutionDataBaseSettings = solutionDataBaseSettings.Value;

        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);
        _excludedHours = new HashSet<DateTime>(configuration.GetSection("ExcludedHours").Get<List<DateTime>>()!);
    }

    #region Contributions
    public async Task<IList<YearlyContribution>> GetYearlyContributionsAsync()
    {
        if (_yearlyContributionsCache is not null)
        {
            return _yearlyContributionsCache;
        }

        var list = await GetTicksContributionsAsync(_startDateTimeUtc, DateTime.Now).ConfigureAwait(true);
        CreateGroups(list);
        return _yearlyContributionsCache!;
    }
    private async Task<List<HourlyContribution>> GetTicksContributionsAsync(DateTime startDay, DateTime endDay)
    {
        var start = startDay.Date;
        var end = endDay.Date.AddDays(1).AddSeconds(-1);

        var list = new List<HourlyContribution>();
        
        await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={_solutionDataBaseSettings.DataBaseName};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = start.ToString(CultureInfo.InvariantCulture) });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = end.ToString(CultureInfo.InvariantCulture) });
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var hour = reader.GetInt64(0);
            var dateTime = reader.GetDateTime(1);
            var hasContribution = reader.GetBoolean(2);
            list.Add(new HourlyContribution
            {
                Hour = hour,
                DateTime = dateTime,
                HasContribution = hasContribution
            });
        }

        return list;
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
                    dailyContribution.Contribution = DetermineContributionStatus(dailyContribution);
                    monthlyContribution.DailyContributions.Add(dailyContribution);
                }

                yearlyContribution.MonthlyContributions.Add(monthlyContribution);
            }

            _yearlyContributionsCache.Add(yearlyContribution);
        }
    }
    private Contribution DetermineContributionStatus(DailyContribution dailyContribution)
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
    public async Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime date)
    {
        Debug.Assert(date is { Hour: 0, Minute: 0 });
        var year = date.Year;
        var week = date.Week();
        var contributions = await GetTicksContributionsAsync(date, date.AddDays(1)).ConfigureAwait(true);

        IList<HourlyContribution> dateTimes = contributions;
        var sampleTicks = await GetSampleTicksAsync(dateTimes, year, week).ConfigureAwait(true);

        var symbolicContributions = new List<DailyBySymbolContribution>();

        // Construct the basic structure of SymbolicContributions for all symbols and hours
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
                    DateTime = new DateTime(year, date.Month, date.Day, hour, 0, 0),
                    Hour = hour,
                    HasContribution = false
                };

                symbolicContribution.HourlyContributions.Add(hourlyContribution);
            }

            symbolicContributions.Add(symbolicContribution);
        }

        // Update the SymbolicContributions based on the sampleTicks
        foreach (var tick in sampleTicks)
        {
            var symbolicContribution = symbolicContributions.First(sc => sc.Symbol == tick.Symbol);
            var hourlyContribution = symbolicContribution.HourlyContributions.First(hc => hc.Hour == tick.DateTime.Hour);
            hourlyContribution.HasContribution = true;
        }

        // Determine the overall Contribution status for each DailyBySymbolContribution
        foreach (var symbolicContribution in symbolicContributions)
        {
            symbolicContribution.Contribution = DetermineContributionStatus(symbolicContribution);
        }

        return symbolicContributions;
    }
    #endregion Contributions

    #region RecalculateAllContributionsAsync
    private int _recalculateProcessedItems;
    public async Task RecalculateAllContributionsAsync(CancellationToken cancellationToken)
    {
        var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions)).Count();
        _recalculateProcessedItems = 0;
        Messenger.Send(new ProgressReportMessage(0), DataServiceToken.Progress);

        try
        {
            await ClearAllContributionsAsync(cancellationToken).ConfigureAwait(true);

            foreach (var yearlyContribution in _yearlyContributionsCache!)
            {
                await ProcessYearlyContributionAsync(yearlyContribution, totalItems, cancellationToken).ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            throw;
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
                    var result = await UpdateContributionsAsync(list, false).ConfigureAwait(true);
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
        var tasks = dailyContribution.HourlyContributions.Select(hourlyContribution => ProcessHourlyContributionAsync(hourlyContribution)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(true);
        UpdateStatus(dailyContribution);
        cancellationToken.ThrowIfCancellationRequested();
    }
    private async Task ProcessHourlyContributionAsync(HourlyContribution hourlyContribution)
    {
        Messenger.Send(new InfoMessage(hourlyContribution.DateTime.ToString("D")), DataServiceToken.Info);
        var sampleTicksResult = await GetSampleTicksAsync(new[] { hourlyContribution }, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
        var sampleTicks = sampleTicksResult.ToList();
        if (sampleTicks.Count == 0)
        {
            hourlyContribution.HasContribution = false;
            var result = await UpdateContributionsAsync(new[] { hourlyContribution.Hour }, false).ConfigureAwait(true);
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
            var result = await UpdateContributionsAsync(new[] { hourlyContribution.Hour }, true).ConfigureAwait(true);
            Debug.Assert(result == 1);
        }
    }
    #endregion RecalculateAllContributionsAsync

    public async Task<int> UpdateContributionsAsync(IEnumerable<long> hourNumbers, bool status)
    {
        var hourNumbersTable = new DataTable();
        hourNumbersTable.Columns.Add("HourNumber", typeof(long));
        foreach (var hourNumber in hourNumbers)
        {
            hourNumbersTable.Rows.Add(hourNumber);
        }

        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={_solutionDataBaseSettings.DataBaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("UpdateTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
            var parameter = new SqlParameter("@HourNumbers", SqlDbType.Structured)
            {
                TypeName = "dbo.HourNumbersTableType",
                Value = hourNumbersTable
            };
            cmd.Parameters.Add(parameter);
            cmd.Parameters.AddWithValue("@Status", status.ToString());

            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return (int)result!;
        }
        catch (Exception exception)
        {
            LogException(exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    public async Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var dateTime in dateTimes)
        {
            dataTable.Rows.Add(dateTime);
        }

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, _thisProvider);

        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("DeleteTicks", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber.ToString() });
            command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
            command.CommandTimeout = 0;
            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            _logger.LogTrace("{count} ticks deleted.", (int)result!);
            return (int)result!;
        }
        catch (Exception exception)
        {
            LogException(exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }


    private async Task<IEnumerable<Quotation>> GetSampleTicksAsync(IEnumerable<HourlyContribution> hourlyContributions, int yearNumber, int weekNumber)
    {
        var result = new List<Quotation>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var hourlyContribution in hourlyContributions)
        {
            dataTable.Rows.Add(hourlyContribution.DateTime.ToString(CultureInfo.InvariantCulture));
        }

        try
        {
            var databaseName = GetDatabaseName(yearNumber, weekNumber, _thisProvider);
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetSampleTicks", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber.ToString() });
            command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
            command.CommandTimeout = 0;
            int id = default;
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
                result.Add(quotation);
            }
        }
        catch (Exception exception)
        {
            LogException(exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }

        return result;
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
        dailyContribution.Contribution = DetermineContributionStatus(dailyContribution);
        Messenger.Send(new DailyContributionChangedMessage(dailyContribution), DataServiceToken.DataToUpdate);
    }









    private async Task LoadTicksToCacheAsync(DateTime dateTime)
    {
        throw new NotImplementedException();

        //var quotations = new List<Quotation>();

        //var yearNumber = dateTime.Year;
        //var weekNumber = dateTime.Week();

        //var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, ThisProvider);
        //await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
        //await connection.OpenAsync().ConfigureAwait(false);
        //await using var command = new SqlCommand("GetQuotationsByWeek", connection) { CommandType = CommandType.StoredProcedure, CommandTimeout = 0 };
        //command.Parameters.AddWithValue("@Week", weekNumber);
        //await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        //int id = default;
        //while (await reader.ReadAsync().ConfigureAwait(false))
        //{
        //    var resultSymbol = (Symbol)reader.GetInt32(0);
        //    var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
        //    var resultAsk = reader.GetDouble(2);
        //    var resultBid = reader.GetDouble(3);
        //    var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
        //    quotations.Add(quotation);
        //}

        //var groupedQuotations = quotations.OrderBy(q=>q.DateTime).
        //    GroupBy(q => new QuotationKey { Year = q.DateTime.Year, Quarter = q.Quarter, Week = q.Week, Day = q.DateTime.Day, Hour = q.DateTime.Hour, Month = q.DateTime.Month, });

        //foreach (var hourGroup in groupedQuotations)
        //{
        //    var year = hourGroup.Key.Year.ToString();
        //    var month = hourGroup.Key.Month.ToString("D2");
        //    var day = hourGroup.Key.Day.ToString("D2");
        //    var hour = hourGroup.Key.Hour.ToString("D2");
        //    var key = $"{year}.{month}.{day}.{hour}";

        //    var tmp = hourGroup.ToList();
        //    SetQuotations(key, tmp);
        //}
    }
    
}