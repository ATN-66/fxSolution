/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;
using DateTime = System.DateTime;

namespace Terminal.WinUI3.Services;

public class DataService : ObservableRecipient, IDataService // ObservableRecipient???????
{
    private readonly int _countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
    private readonly IProcessor _processor;
    private readonly IExternalDataSource _externalDataSource;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<DataService> _logger;

    private readonly HashSet<DateTime> _excludedDates;
    private readonly HashSet<DateTime> _excludedHours;
    private readonly string _server;
    private readonly string _solutionDatabase;
    private readonly DateTime _startDateTimeUtc;

    private readonly Dictionary<string, List<Quotation>> _ticksCache = new();
    private readonly Queue<string> _keys = new();
    private const int MaxItems = 4_032;

    public DataService(IProcessor processor, IExternalDataSource externalDataSource, IConfiguration configuration, IAppNotificationService notificationService, ILogger<DataService> logger) 
    {
        _processor = processor;
        _externalDataSource = externalDataSource;
        _notificationService = notificationService;
        _logger = logger;

        _server = configuration.GetConnectionString("Server")!;
        _solutionDatabase = configuration.GetConnectionString("SolutionDatabase")!;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };//todo
        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>("StartDate")!, formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);
        _excludedHours = new HashSet<DateTime>(configuration.GetSection("ExcludedHours").Get<List<DateTime>>()!);
    }

    #region YearlyContributions
    private async Task<List<HourlyContribution>> GetTicksContributionsAsync(DateTime startDay, DateTime endDay)
    {
        var start = startDay.Date;
        var end = endDay.Date.AddDays(1).AddSeconds(-1);

        var list = new List<HourlyContribution>();
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = start });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = end });
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
    private IList<YearlyContribution>? _yearlyContributionsCache;
    public async Task<IEnumerable<YearlyContribution>> GetYearlyContributionsAsync()
    {
        if (_yearlyContributionsCache is not null)
        {
            return _yearlyContributionsCache;
        }

        var list = await GetTicksContributionsAsync(_startDateTimeUtc, DateTime.Now).ConfigureAwait(true);
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
                Year = yearGroup.Year,
                MonthlyContributions = new ObservableCollection<MonthlyContribution>()
            };

            foreach (var monthGroup in yearGroup.Months)
            {
                var monthlyContribution = new MonthlyContribution
                {
                    Year = yearGroup.Year,
                    Month = monthGroup.Month,
                    DailyContributions = new ObservableCollection<DailyContribution>()
                };

                foreach (var dayGroup in monthGroup.Days)
                {
                    var hourlyContributions = dayGroup.ToList();
                    var dailyContribution = new DailyContribution
                    {
                        Year = yearGroup.Year,
                        Month = monthGroup.Month,
                        Day = hourlyContributions[0].Day,
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
    public async Task<IEnumerable<SymbolicContribution>> GetDayContributionAsync(DateTimeOffset dateTimeOffset)
    {
        var dateTime = dateTimeOffset.DateTime.Date;
        var year = dateTime.Year;
        var week = dateTime.Week();
        var contributions = await GetTicksContributionsAsync(dateTime, dateTime.AddDays(1)).ConfigureAwait(true);

        IList<HourlyContribution> dateTimes = contributions;
        var sampleTicks = await GetSampleTicksAsync(dateTimes, year, week).ConfigureAwait(true);

        var symbolicContributions = new List<SymbolicContribution>();

        // Construct the basic structure of SymbolicContributions for all symbols and hours
        foreach (Symbol symbol in Enum.GetValues(typeof(Symbol)))
        {
            var symbolicContribution = new SymbolicContribution
            {
                Symbol = symbol,
                Year = year,
                Month = dateTime.Month,
                Week = week,
                Day = dateTime.Day,
                HourlyContributions = new List<HourlyContribution>()
            };

            for (var hour = 0; hour < 24; hour++)
            {
                var hourlyContribution = new HourlyContribution
                {
                    DateTime = new DateTime(year, dateTime.Month, dateTime.Day, hour, 0, 0),
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

        // Determine the overall Contribution status for each SymbolicContribution
        foreach (var symbolicContribution in symbolicContributions)
        {
            symbolicContribution.Contribution = DetermineContributionStatus(symbolicContribution);
        }

        return symbolicContributions;
    }
    public async Task<IEnumerable<Quotation>> GetTicksAsync(Symbol symbol, DateTime startDateTime, DateTime endDateTime, Provider provider)
    {
        var quotations = provider switch
        {
            Provider.FileService => await _externalDataSource.GetTicksAsync(startDateTime, endDateTime, Provider.FileService).ConfigureAwait(true),
            Provider.Mediator => await _externalDataSource.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(true),
            Provider.Terminal => await GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(true),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };

        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations;
    }
    public async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime)
    {
        var result = new List<Quotation>();
        var start = startDateTime.Date.AddHours(startDateTime.Hour);
        var end = endDateTime.Date.AddHours(endDateTime.Hour);

        var index = start;
        do
        {
            var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";
            if (!_ticksCache.ContainsKey(key))
            {
                await LoadTicksToCacheAsync(index).ConfigureAwait(true);
            }

            if (_ticksCache.ContainsKey(key))
            {
                result.AddRange(GetQuotations(key));
            }
                
            index = index.Add(new TimeSpan(1, 0, 0));
        }
        while (index < end);
        return result;
    }

    public async Task<int> ReImportSelectedAsync(DateTime dateTime)
    {
        var year = dateTime.Year;
        var month = dateTime.Month;
        var day = dateTime.Day;

        foreach (var yearlyContribution in _yearlyContributionsCache!)
        {
            for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
            {
                var monthlyContribution = yearlyContribution.MonthlyContributions[m];
                for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
                {
                    var dailyContribution = monthlyContribution.DailyContributions[d];
                    if (year == dailyContribution.Year && month == dailyContribution.Month && day == dailyContribution.Day)
                    {
                        var hourNumbers = new List<long>();
                        var dateTimes = new List<DateTime>();
                        for (var h = 0; h < dailyContribution.HourlyContributions.Count; h++)
                        {
                            var hourlyContribution = dailyContribution.HourlyContributions[h];
                            hourlyContribution.HasContribution = false;
                            hourNumbers.Add(hourlyContribution.Hour);
                            dateTimes.Add(hourlyContribution.DateTime);
                        }
                        var updateContributionsResult = await UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true);
                        Debug.Assert(updateContributionsResult == 24);
                        var deleteTicksResult = await DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                        _logger.Log(LogLevel.Information, "ReImport:{count} ticks deleted.", deleteTicksResult);
                        var quotations = await _externalDataSource.GetTicksAsync(dateTimes[0], dateTimes[^1].AddHours(1)).ConfigureAwait(true);
                        var insertTicksResult = await SavesTicksAsync(quotations.ToList(), dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                        _logger.Log(LogLevel.Information, "ReImport:{count} ticks saved.", insertTicksResult);
                        var sampleTicksResult = await GetSampleTicksAsync(dailyContribution.HourlyContributions, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                        var sampleTicks = sampleTicksResult.ToList();
                        var ticksHourly = sampleTicks.GroupBy(q => new DateTime(q.DateTime.Year, q.DateTime.Month, q.DateTime.Day, q.DateTime.Hour, 0, 0)).ToList();
                        var hNs = (from @group in ticksHourly
                            let distinctSymbolsInGroup = @group.Select(q => q.Symbol).Distinct().Count()
                            where distinctSymbolsInGroup == _countOfSymbols
                            select (long)(@group.Key - DateTimeExtensionsAndHelpers.EpochStartDateTimeUtc).TotalHours).ToList();
                        var result = await UpdateContributionsAsync(hNs, true).ConfigureAwait(true);
                        foreach (var hourlyContribution in dailyContribution.HourlyContributions.Where(hourlyContribution => hNs.Contains(hourlyContribution.Hour)))
                        {
                            hourlyContribution.HasContribution = true;
                        }
                        dailyContribution.Contribution = DetermineContributionStatus(dailyContribution);
                        return result;
                    }
                }
            }
        }

        return 0;
    }
    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        var countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
        var processedItems = 0;
        try
        {
            var totalItems = _yearlyContributionsCache!.SelectMany(y => y.MonthlyContributions!.SelectMany(m => m.DailyContributions.SelectMany(d => d.HourlyContributions))).Count();
            for (var y = 0; y < _yearlyContributionsCache!.Count; y++)
            {
                var yearlyContribution = _yearlyContributionsCache[y];
                for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
                {
                    var monthlyContribution = yearlyContribution.MonthlyContributions[m];
                    for (var d = 0; d < monthlyContribution.DailyContributions.Count; d++)
                    {
                        var dailyContribution = monthlyContribution.DailyContributions[d];
                        processedItems = ReportProgressReport(processedItems, dailyContribution, totalItems);
                        if (dailyContribution.Contribution is Contribution.Excluded or Contribution.Full)
                        {
                            continue;
                        }
                        for (var h = 0; h < dailyContribution.HourlyContributions.Count; h++)
                        {
                            var hourlyContribution = dailyContribution.HourlyContributions[h];
                            Messenger.Send(new InfoMessage(hourlyContribution.DateTime.ToString("F")), DataServiceToken.Info);
                            if (hourlyContribution.HasContribution)
                            {
                                continue;
                            }
                            var start = hourlyContribution.DateTime;
                            var end = start.AddHours(1);
                            var q = await _externalDataSource.GetTicksAsync(start, end).ConfigureAwait(true);//todo:
                            var quotations = q.ToList();
                            if (quotations.Count == 0)
                            {
                                switch (hourlyContribution.DateTime)
                                {
                                    case { DayOfWeek: DayOfWeek.Friday, Hour: 22 or 23 }:
                                    case { DayOfWeek: DayOfWeek.Sunday, Hour: not 22 and not 23 }:
                                        UpdateStatus(dailyContribution);
                                        break;
                                    default:
                                        if (_excludedHours.Contains(hourlyContribution.DateTime))
                                        {
                                            UpdateStatus(dailyContribution);
                                        }
                                        else
                                        {
                                            _logger.LogError($"{nameof(dailyContribution)}. Excluded Hours have to be adjusted for day:{dailyContribution}");
                                            return;
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                var s = await GetSampleTicksAsync(new List<HourlyContribution>() { hourlyContribution }, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
                                var samples = s.ToList();
                                if (samples.Count != 0)
                                {
                                    if (samples[0].DateTime < hourlyContribution.DateTime)
                                    {
                                        await ShiftQuotationsTimeAsync(samples, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
                                        s = await GetSampleTicksAsync(new List<HourlyContribution>() { hourlyContribution }, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
                                        samples = s.ToList();
                                        if (samples.Count != 0)
                                        {
                                            _logger.LogError($"{nameof(samples)}.Count must be 0. Fail to adjust Ticks.");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError($"{nameof(samples)}.Count must be 0. Unwanted Ticks in this hour.");
                                        return;
                                    }
                                }

                                var quotationsHourly = quotations.GroupBy(q => new DateTime(q.DateTime.Year, q.DateTime.Month, q.DateTime.Day, q.DateTime.Hour, 0, 0)).ToList();
                                if (quotationsHourly.Count != 1)
                                {
                                    _logger.LogError($"{nameof(quotationsHourly)}.Count must be 1.");
                                    return;
                                }
                               
                                var hourNumbers = (from @group in quotationsHourly
                                                   let distinctSymbolsInGroup = @group.Select(q => q.Symbol).Distinct().Count()
                                                   where distinctSymbolsInGroup == countOfSymbols
                                                   select (long)(@group.Key - DateTimeExtensionsAndHelpers.EpochStartDateTimeUtc).TotalHours).ToList();

                                if (hourNumbers.Count != 1)
                                {
                                    _logger.LogError($"{nameof(hourNumbers)}.Count must be 1. All symbols must be present.");
                                    return;
                                }

                                await SavesTicksAsync(quotations, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
                                await UpdateContributionsAsync(hourNumbers, true).ConfigureAwait(true);

                                UpdateStatus(dailyContribution);
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"DataService.ImportAsync:{exception.Message}");
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
            dataTable.Rows.Add(hourlyContribution.DateTime);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, Provider.Terminal);
        await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = new SqlCommand("GetSampleTicks", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber });
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

        return result;
    }
    private async Task<int> SavesTicksAsync(IList<Quotation> quotations, int yearNumber, int weekNumber)
    {
        int result;
        var tableName = GetTableName(weekNumber);
        DateTimeExtensionsAndHelpers.Check_ISO_8601(yearNumber, weekNumber, quotations);

        var dataTable = new DataTable();
        dataTable.Columns.Add("Symbol", typeof(int));
        dataTable.Columns.Add("DateTime", typeof(DateTime));
        dataTable.Columns.Add("Ask", typeof(double));
        dataTable.Columns.Add("Bid", typeof(double));
        foreach (var quotation in quotations)
        {
            dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, Provider.Terminal);
        var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction($"week:{weekNumber:00}");
        await using var command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
        command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
        var rowCountParam = new SqlParameter("@RowCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(rowCountParam);
        command.CommandTimeout = 0;

        try
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            result = (int)rowCountParam.Value;
            transaction.Commit();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.InnerException?.Message);
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Debug.WriteLine(ex.Message);
                throw;
            }

            throw;
        }

        return result;
    }
    private async Task ShiftQuotationsTimeAsync(IList<Quotation> quotations, int yearNumber, int weekNumber)
    {
        var tableName = GetTableName(weekNumber);
        var dataTable = new DataTable();
        dataTable.Columns.Add("Symbol", typeof(int));
        dataTable.Columns.Add("DateTime", typeof(DateTime));
        dataTable.Columns.Add("Ask", typeof(double));
        dataTable.Columns.Add("Bid", typeof(double));
        foreach (var quotation in quotations)
        {
            dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, Provider.Terminal);
        var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction($"week:{weekNumber:00}");
        await using var command = new SqlCommand("ShiftQuotationsTime", connection, transaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
        command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
        command.CommandTimeout = 0;

        try
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            transaction.Commit();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception.Message);
            _logger.LogError(exception.InnerException?.Message);
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Debug.WriteLine(ex.Message);
                throw;
            }

            throw;
        }
    }
    private async Task<int> UpdateContributionsAsync(IEnumerable<long> hourNumbers, bool status)
    {
        var hourNumbersTable = new DataTable();
        hourNumbersTable.Columns.Add("HourNumber", typeof(long));
        foreach (var hourNumber in hourNumbers)
        {
            hourNumbersTable.Rows.Add(hourNumber);
        }

        try
        {
            await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("UpdateTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
            var parameter = new SqlParameter("@HourNumbers", SqlDbType.Structured)
            {
                TypeName = "dbo.HourNumbersTableType",
                Value = hourNumbersTable
            };
            cmd.Parameters.Add(parameter);
            cmd.Parameters.AddWithValue("@Status", status);

            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return (int)result!;
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }
    }
    private async Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var dateTime in dateTimes)
        {
            dataTable.Rows.Add(dateTime);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, Provider.Terminal);

        try
        {
            await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("DeleteTicks", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber });
            command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
            command.CommandTimeout = 0;
            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            return (int)result!;
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }
    }
    private int ReportProgressReport(int processedItems, DailyContribution dailyContribution, int totalItems)
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
        var quotations = new List<Quotation>();

        var yearNumber = dateTime.Year;
        var weekNumber = dateTime.Week();

        var databaseName = GetDatabaseName(yearNumber, weekNumber, Provider.Terminal);
        await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = new SqlCommand("GetQuotationsByWeek", connection) { CommandType = CommandType.StoredProcedure, CommandTimeout = 0 };
        command.Parameters.AddWithValue("@Week", weekNumber);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        int id = default;
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var resultSymbol = (Symbol)reader.GetInt32(0);
            var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
            var resultAsk = reader.GetDouble(2);
            var resultBid = reader.GetDouble(3);
            var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
            quotations.Add(quotation);
        }

        var groupedQuotations = quotations.OrderBy(q=>q.DateTime).
            GroupBy(q => new QuotationKey { Year = q.DateTime.Year, Quarter = q.Quarter, Week = q.Week, Day = q.DateTime.Day, Hour = q.DateTime.Hour, Month = q.DateTime.Month, });

        foreach (var hourGroup in groupedQuotations)
        {
            var year = hourGroup.Key.Year.ToString();
            var month = hourGroup.Key.Month.ToString("D2");
            var day = hourGroup.Key.Day.ToString("D2");
            var hour = hourGroup.Key.Hour.ToString("D2");
            var key = $"{year}.{month}.{day}.{hour}";

            var tmp = hourGroup.ToList();
            SetQuotations(key, tmp);
        }
    }
    private void AddQuotation(string key, List<Quotation> quotationList)
    {
        if (_ticksCache.Count >= MaxItems)
        {
            var oldestKey = _keys.Dequeue();
            _ticksCache.Remove(oldestKey);
        }

        _ticksCache[key] = quotationList;
        _keys.Enqueue(key);
    }
    private void SetQuotations(string key, List<Quotation> quotations)
    {
        AddQuotation(key, quotations);
    }
    private IEnumerable<Quotation> GetQuotations(string key)
    {
        _ticksCache.TryGetValue(key, out var quotations);
        return quotations!;
    }
    #endregion YearlyContributions



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
                await ProcessYearlyContributionAsync(yearlyContribution, cancellationToken, totalItems).ConfigureAwait(true);
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
                    processedItems = ReportProgressReport(processedItems, dailyContribution, totalItems);
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
    private async Task ProcessYearlyContributionAsync(YearlyContribution yearlyContribution, CancellationToken cancellationToken, int totalItems)
    {
        for (var m = 0; m < yearlyContribution.MonthlyContributions!.Count; m++)
        {
            var monthlyContribution = yearlyContribution.MonthlyContributions[m];
            await ProcessMonthlyContributionAsync(monthlyContribution, cancellationToken, totalItems).ConfigureAwait(true);
        }
    }
    private async Task ProcessMonthlyContributionAsync(MonthlyContribution monthlyContribution, CancellationToken cancellationToken, int totalItems)
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




















    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day) => throw new NotImplementedException();

    //switch (day)
    //{
    //    case 0: return await GetQuotationsForWeekAsync(year, week, environment, modification).ConfigureAwait(false);
    //    case < 1 or > 7: throw new ArgumentOutOfRangeException(nameof(day), ResourceManager.Current.MainResourceMap.GetValue("Resources/DataService_GetQuotationsForDayAsync_day_must_be_between_1_and_7_").ValueAsString);
    //}
    //var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
    //var firstQuotations = new Queue<Quotation>();
    //var quotations = new Queue<Quotation>();
    //var databaseName = GetDatabaseName(year, week, environment, modification);
    //var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";
    //await using (var connection = new SqlConnection(connectionString))
    //{
    //    await connection.OpenAsync().ConfigureAwait(false);
    //    await using var command = new SqlCommand("GetQuotationsByWeekAndDay", connection) { CommandTimeout = 0 };
    //    command.CommandType = CommandType.StoredProcedure;
    //    command.Parameters.AddWithValue("@Week", week);
    //    command.Parameters.AddWithValue("@DayOfWeek", day);
    //    int id = default;
    //    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    //    while (await reader.ReadAsync().ConfigureAwait(false))
    //    {
    //        var resultSymbol = (Symbol)reader.GetInt32(0);
    //        var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
    //        var resultAsk = reader.GetDouble(2);
    //        var resultBid = reader.GetDouble(3);
    //        var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
    //        if (!firstQuotationsDict.ContainsKey(quotation.Symbol))
    //        {
    //            firstQuotationsDict[quotation.Symbol] = quotation;
    //            firstQuotations.Enqueue(quotation);
    //        }
    //        else
    //        {
    //            quotations.Enqueue(quotation);
    //        }
    //    }
    //}
    //return (firstQuotations, quotations);
    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week) => throw new NotImplementedException();

    //var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
    //var firstQuotations = new Queue<Quotation>();
    //var quotations = new Queue<Quotation>();
    //var databaseName = GetDatabaseName(year, week, environment, modification);
    //var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";
    //await using (var connection = new SqlConnection(connectionString))
    //{
    //    await connection.OpenAsync().ConfigureAwait(false);
    //    await using var command = new SqlCommand("GetQuotationsByWeek", connection) { CommandTimeout = 0 };
    //    command.CommandType = CommandType.StoredProcedure;
    //    command.Parameters.AddWithValue("@Week", week);
    //    await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
    //    int id = default;
    //    while (await reader.ReadAsync().ConfigureAwait(false))
    //    {
    //        var resultSymbol = (Symbol)reader.GetInt32(0);
    //        var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
    //        var resultAsk = reader.GetDouble(2);
    //        var resultBid = reader.GetDouble(3);
    //        var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
    //        if (!firstQuotationsDict.ContainsKey(quotation.Symbol))
    //        {
    //            firstQuotationsDict[quotation.Symbol] = quotation;
    //            firstQuotations.Enqueue(quotation);
    //        }
    //        else
    //        {
    //            quotations.Enqueue(quotation);
    //        }
    //    }
    //}
    //return (firstQuotations, quotations);
    
    

   

   

    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";
    private static string GetDatabaseName(int yearNumber, int weekNumber, Provider provider) => $"{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}.{provider.ToString().ToLower()}";
}

internal readonly struct QuotationKey
{
    public int Year
    {
        get; init;
    }
    public int Quarter
    {
        get; init;
    }
    public int Week
    {
        get; init;
    }
    public int Day
    {
        get; init;
    }
    public int Hour
    {
        get; init;
    }
    public int Month
    {
        get; init;
    }

    public override string ToString()
    {
        return $"{Year:0000}|{Month:00}|{Week:00}|{Day:00}|{Hour:00}";
    }
}