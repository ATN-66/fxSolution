/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
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

public class DataService : ObservableRecipient, IDataService // ObservableRecipient???????//todo:ObservableRecipient
{
    private const Provider Provider = Common.Entities.Provider.Terminal;
    private readonly int _countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
    private readonly IList<Symbol> _allSymbols = Enum.GetValues<Symbol>().ToList();

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
    private const string Format = "dddd, MMMM d, yyyy";
    private const int QuartersInYear = 4;
    private readonly string _dbBackupDrive;
    private readonly string _dbProviderBackupFolder;
    private readonly string _dbSolutionBackupFolder;

    public DataService(IProcessor processor, IExternalDataSource externalDataSource, IConfiguration configuration, IAppNotificationService notificationService, ILogger<DataService> logger) 
    {
        _processor = processor;
        _externalDataSource = externalDataSource;
        _notificationService = notificationService;
        _logger = logger;

        _server = configuration.GetConnectionString("Server")!;
        _solutionDatabase = configuration.GetConnectionString("SolutionDatabase")!;

        _dbBackupDrive = configuration.GetValue<string>("dbBackupDrive")!;
        _dbProviderBackupFolder = configuration.GetValue<string>("dbProviderBackupFolder")!;
        _dbSolutionBackupFolder = configuration.GetValue<string>("dbSolutionBackupFolder")!;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };
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
    public async Task<IEnumerable<DailyBySymbolContribution>> GetDayContributionAsync(DateTime date)
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
    public async Task<IEnumerable<Quotation>> GetTicksAsync(Symbol symbol, DateTime startDateTime, DateTime endDateTime, Provider provider, bool exactly)
    {
        var quotations = provider switch
        {
            Provider.FileService => await _externalDataSource.GetTicksAsync(startDateTime, endDateTime, provider, exactly).ConfigureAwait(true),
            Provider.Mediator => await _externalDataSource.GetTicksAsync(startDateTime, endDateTime, provider, exactly).ConfigureAwait(true),
            Provider.Terminal => await GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(true),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };

        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations;
    }
    private async Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime)
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
    public async Task<Contribution> ReImportSelectedAsync(DateTime dateTime)
    {
        var year = dateTime.Year;
        var month = dateTime.Month;
        var day = dateTime.Day;

        for (var y = 0; y < _yearlyContributionsCache!.Count; y++)
        {
            var yearlyContribution = _yearlyContributionsCache[y];
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
                    var updateContributionsResult = await UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true);
                    Debug.Assert(updateContributionsResult == 24);
                    var deleteTicksResult = await DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                    _logger.Log(LogLevel.Information, "ReImport:{count} ticks deleted.", deleteTicksResult);

                    var input = await _externalDataSource.GetTicksAsync(dateTimes[0], dateTimes[^1].AddHours(1)).ConfigureAwait(true);
                    var quotations = input.ToList();
                    var groupedQuotations = quotations.GroupBy(q => new QuotationKey { Symbol = q.Symbol, Hour = q.DateTime.Hour, });
                    var sortedGroups = groupedQuotations.OrderBy(g => g.Key.Hour).ThenBy(g => g.Key.Symbol);
                    var groupsGroupedByHour = sortedGroups.GroupBy(g => g.Key.Hour);
                    var counter = 0;
                    foreach (var hourGroup in groupsGroupedByHour)
                    {
                        var hour = hourGroup.Key;
                        var symbolsInHour = hourGroup.Count();
                        var quotationsForThisHour = hourGroup.SelectMany(group => group).OrderBy(q => q.DateTime).ToList();
                        var insertTicksResult = await SavesTicksAsync(quotationsForThisHour, dailyContribution.Year, dailyContribution.Week).ConfigureAwait(true);
                        counter += insertTicksResult;

                        if (_countOfSymbols != symbolsInHour)
                        {
                            continue;
                        }

                        updateContributionsResult = await UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);
                        Debug.Assert(updateContributionsResult == 1);
                        dailyContribution.HourlyContributions[hour].HasContribution = true;
                    }
                    Debug.Assert(quotations.Count == counter);
                    monthlyContribution.DailyContributions[d].Contribution = DetermineContributionStatus(dailyContribution);
                    _logger.LogInformation("{date} was updated. {count} were saved.", dailyContribution.HourlyContributions[0].DateTime, counter);
                    return 0;
                }
            }
        }
        throw new InvalidOperationException("Never should be here.");
    }
    public async Task ImportAsync(CancellationToken cancellationToken)
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
                    Messenger.Send(new InfoMessage(dailyContribution.HourlyContributions[0].DateTime.ToString(Format)), DataServiceToken.Info);

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

                    Debug.Assert(year == dateTimes[0].Year);
                    Debug.Assert(month == dateTimes[0].Month);
                    Debug.Assert(day == dateTimes[0].Day);

                    var updateContributionsResult = await UpdateContributionsAsync(hourNumbers, false).ConfigureAwait(true);
                    Debug.Assert(updateContributionsResult == 24);
                    var deleteTicksResult = await DeleteTicksAsync(dateTimes, dailyContribution.Year, dailyContribution.HourlyContributions[0].DateTime.Week()).ConfigureAwait(true);
                    _logger.Log(LogLevel.Information, "Import:{count} ticks deleted.", deleteTicksResult);

                    var input = await _externalDataSource.GetTicksAsync(dateTimes[0], dateTimes[^1].AddHours(1)).ConfigureAwait(true);
                    var quotations = input.ToList();

                    Debug.Assert(year == quotations[0].DateTime.Year);
                    Debug.Assert(month == quotations[0].DateTime.Month);
                    Debug.Assert(day == quotations[0].DateTime.Day);

                    var groupedQuotations = quotations.GroupBy(q => new QuotationKey { Symbol = q.Symbol, Hour = q.DateTime.Hour, });
                    var sortedGroups = groupedQuotations.OrderBy(g => g.Key.Hour).ThenBy(g => g.Key.Symbol);
                    var groupsGroupedByHour = sortedGroups.GroupBy(g => g.Key.Hour);
                    var counter = 0;
                    foreach (var hourGroup in groupsGroupedByHour)
                    {
                        var hour = hourGroup.Key;
                        var symbolsInHour = hourGroup.Count();
                        var quotationsForThisHour = hourGroup.SelectMany(group => group).OrderBy(q => q.DateTime).ToList();
                        var insertTicksResult = await SavesTicksAsync(quotationsForThisHour, dailyContribution.Year, dailyContribution.Week).ConfigureAwait(true);
                        counter += insertTicksResult;

                        if (_countOfSymbols != symbolsInHour)
                        {
                            continue;
                        }

                        updateContributionsResult = await UpdateContributionsAsync(new[] { dailyContribution.HourlyContributions[hour].Hour }, true).ConfigureAwait(true);
                        Debug.Assert(updateContributionsResult == 1);
                        dailyContribution.HourlyContributions[hour].HasContribution = true;
                        UpdateStatus(dailyContribution);
                        
                    }
                    Debug.Assert(quotations.Count == counter);
                    _logger.LogInformation("Import: {date} was updated. {count} were saved.", dailyContribution.HourlyContributions[0].DateTime.ToString(Format), counter);
                    if (cancellationToken.IsCancellationRequested) { return; }
                }
            }
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

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
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
        var tableName = DatabaseExtensionsAndHelpers.GetTableName(weekNumber);
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

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
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
                _logger.LogError(exception.Message);
                _logger.LogError(exception.InnerException?.Message);
                throw;
            }

            throw;
        }

        return result;
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
    
    //todo: make it from to
    private async Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var dateTime in dateTimes)
        {
            dataTable.Rows.Add(dateTime);
        }

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);

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
        var quotations = new List<Quotation>();

        var yearNumber = dateTime.Year;
        var weekNumber = dateTime.Week();

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
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

    public async Task<int> BackupAsync()
    {
        var startYear = _startDateTimeUtc.Year;
        var endYear = DateTime.UtcNow.Year;
        var anyFailures = false;

        try
        {
            int result;
            for (var yearToBackup = startYear; yearToBackup <= endYear; yearToBackup++)
            {
                for (var quarter = 1; quarter <= QuartersInYear; quarter++)
                {
                    result = await BackupProviderDatabase(yearToBackup, quarter).ConfigureAwait(false);
                    if (result == -1)
                    {
                        anyFailures = true;
                    }
                }
            }

            result = await BackupSolutionDatabase().ConfigureAwait(false);
            if (result == -1)
            {
                anyFailures = true;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }

        return anyFailures ? -1 : 1;

        async Task<int> BackupProviderDatabase(int yearNumber, int quarterNumber)
        {
            await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("BackupProviderDatabase", connection) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Drive", _dbBackupDrive)); //"D:"
            cmd.Parameters.Add(new SqlParameter("@Folder", _dbProviderBackupFolder)); //"forex.ms-sql-db.terminal.backup"
            cmd.Parameters.Add(new SqlParameter("@Year", yearNumber));
            cmd.Parameters.Add(new SqlParameter("@Quarter", quarterNumber));
            cmd.Parameters.Add(new SqlParameter("@Provider", Provider.ToString().ToLower()));

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var result = (int)returnParameter.Value;
            return result;
        }

        async Task<int> BackupSolutionDatabase()
        {
            await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("BackupSolutionDatabase", connection) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Drive", _dbBackupDrive)); //"D:"
            cmd.Parameters.Add(new SqlParameter("@Folder", _dbSolutionBackupFolder)); //"forex.ms-sql-db.solution.backup"

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var result = (int)returnParameter.Value;
            return result;
        }
    }
    public Task<int> RestoreAsync()
    {
        return Task.FromResult(0);
    }







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
    
}
