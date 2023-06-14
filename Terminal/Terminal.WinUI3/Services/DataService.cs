/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using Windows.Security.Authentication.Identity.Core;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;

namespace Terminal.WinUI3.Services;

public class DataService : ObservableRecipient, IDataService
{
    private readonly IProcessor _processor;
    private readonly IFileService _fileService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<DataService> _logger;
    private IDispatcherService _dispatcherService; //todo

    private const Entity TicksEntity = Entity.Ticks;
    private readonly HashSet<DateTime> _excludedDates;
    private readonly HashSet<DateTime> _excludedHours;
    private readonly string _server;
    private readonly string _solutionDatabase;
    private readonly DateTime _startDateTimeUtc;
    
    public DataService(IProcessor processor, IFileService fileService, IConfiguration configuration, IAppNotificationService notificationService, ILogger<DataService> logger, IDispatcherService dispatcherService) //todo
    {
        _processor = processor;
        _fileService = fileService;
        _notificationService = notificationService;
        _logger = logger;
        _dispatcherService = dispatcherService; //todo

        _server = configuration.GetConnectionString("Server")!;
        _solutionDatabase = configuration.GetConnectionString("SolutionDatabase")!;

        var formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };//todo
        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>("StartDate")!, formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);
        _excludedHours = new HashSet<DateTime>(configuration.GetSection("ExcludedHours").Get<List<DateTime>>()!);
    }

    #region YearlyContributions
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
                        Days = m.GroupBy(c => c.DateTime.Day)
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
    public async Task<IEnumerable<SymbolicContribution>> GetSymbolicContributionsAsync(DateTimeOffset dateTimeOffset)
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
    public async Task<IEnumerable<Quotation>> GetImportTicksAsync(Symbol symbol, DateTime startDateTime, DateTime endDateTime)
    {
        var quotations = await _fileService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(true);
        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations;
    }
    public async Task ImportTicksAsync(CancellationToken cancellationToken)//todo:cancellationToken
    {
        try
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
                        processedItems += dailyContribution.HourlyContributions.Count;
                        var progressPercentage = processedItems * 100 / totalItems;
                        Messenger.Send(new ProgressReportMessage(progressPercentage), DataServiceToken.Progress);
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
                            var q = await _fileService.GetTicksAsync(start, end).ConfigureAwait(true);
                            var quotations = q.ToList();
                            if (quotations.Count == 0)
                            {
                                switch (hourlyContribution.DateTime)
                                {
                                    case { DayOfWeek: DayOfWeek.Friday, Hour: 22 or 23 }:
                                    case { DayOfWeek: DayOfWeek.Sunday, Hour: not 22 and not 23 }:
                                        UpdateStatus(dailyContribution, dailyContribution.ToString());
                                        break;
                                    default:
                                        if (_excludedHours.Contains(hourlyContribution.DateTime))
                                        {
                                            UpdateStatus(dailyContribution, dailyContribution.ToString());
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
                                        await UpdateTicksAsync(samples, hourlyContribution.Year, hourlyContribution.Week).ConfigureAwait(true);
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

                                var countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
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
                                await UpdateTicksContributionsAsync(hourNumbers).ConfigureAwait(true);

                                hourlyContribution.HasContribution = true;
                                UpdateStatus(dailyContribution, dailyContribution.ToString());
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError($"DataService.ImportTicksAsync:{exception.Message}");
            throw;
        }

        void UpdateStatus(DailyContribution dailyContribution, string info)
        {
            dailyContribution.Contribution = DetermineContributionStatus(dailyContribution);
            Messenger.Send(new DailyContributionChangedMessage(dailyContribution), DataServiceToken.DataToUpdate);
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

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
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
    private async Task SavesTicksAsync(IList<Quotation> quotations, int yearNumber, int weekNumber)
    {
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

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
        var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using (var transaction = connection.BeginTransaction($"week:{weekNumber:00}"))
        {
            await using var command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
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
    }
    private async Task UpdateTicksAsync(IList<Quotation> quotations, int yearNumber, int weekNumber)
    {
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

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
        var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        using (var transaction = connection.BeginTransaction($"week:{weekNumber:00}"))
        {
            await using var command = new SqlCommand("UpdateQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
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
    }
    private async Task UpdateTicksContributionsAsync(IEnumerable<long> hourNumbers)
    {
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("UpdateTicksContributions", connection) { CommandType = CommandType.StoredProcedure };

        var hourNumbersTable = new DataTable();
        hourNumbersTable.Columns.Add("HourNumber", typeof(long));
        foreach (var hourNumber in hourNumbers)
        {
            hourNumbersTable.Rows.Add(hourNumber);
        }

        var parameter = new SqlParameter("@HourNumbers", SqlDbType.Structured)
        {
            TypeName = "dbo.HourNumbersTableType",
            Value = hourNumbersTable
        };
        cmd.Parameters.Add(parameter);
        cmd.Parameters.AddWithValue("@Status", true);

        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
    private async Task DeleteSampleTicksAsync(IEnumerable<HourlyContribution> hourlyContributions, int yearNumber, int weekNumber)
    {
        var result = new List<Quotation>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var hourlyContribution in hourlyContributions)
        {
            dataTable.Rows.Add(hourlyContribution.DateTime);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
        await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = new SqlCommand("DeleteSampleTicks", connection) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber });
        command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
        command.CommandTimeout = 0;

        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

    }
    #endregion YearlyContributions



    //const Entity entity = Common.Entities.Entity.Ticks;

    //foreach (var yearlyContribution in missedContributions)
    //{
    //    var year = yearlyContribution.Year;

    //    foreach (var monthlyContribution in yearlyContribution.MonthlyContributions)
    //    {
    //        var month = monthlyContribution.Month;
    //        var missedDates = monthlyContribution.DailyContributions.Select(contribution => contribution.DateTime.Date).ToList();
    //        var quotations = GetTicksFromFile(year, month, missedDates);
    //        var weeklyQuotations = quotations.GroupBy(quotation => new { YearNumber = quotation.DateTime.Year, WeekNumber = GetWeekNumber(quotation.DateTime) }).ToList();

    //        var tasks = weeklyQuotations.Select(async weekGroup =>
    //        {
    //            var yearNumber = weekGroup.Key.YearNumber;
    //            var weekNumber = weekGroup.Key.WeekNumber;
    //            var quotations = weekGroup.OrderBy(quotation => quotation.DateTime).ToList();
    //            _logger.LogInformation($"ImportTicksAsync --> year: {yearNumber}, week{weekNumber}");
    //            await SavesTicksAsync(quotations, yearNumber, weekNumber).ConfigureAwait(false);
    //            throw new NotImplementedException();
    //            //Messenger.Send(new DataServiceMessage(), DataServiceToken.Ticks);

    //        }).ToList();

    //        await Task.WhenAll(tasks).ConfigureAwait(false);
    //        cancellationToken.ThrowIfCancellationRequested();
    //    }
    //}





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
    public async Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year) => throw new NotImplementedException();

    //var quotationsByWeek = new Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>();
    //var tasks = Enumerable.Range(1, 52).Select(async week =>
    //{
    //    var (firstQuotations, quotations) = await GetQuotationsForWeekAsync(year, week, environment, modification).ConfigureAwait(false);
    //    return (week, firstQuotations, quotations);
    //});
    //foreach (var (weekNumber, firstQuotations, quotations) in await Task.WhenAll(tasks).ConfigureAwait(false))
    //{
    //    quotationsByWeek[weekNumber] = (firstQuotations, quotations);
    //}
    //return quotationsByWeek;
    

    private async Task<List<HourlyContribution>> GetTicksContributionsAsync(DateTime startDateTime, DateTime endDateTime)
    {
        var list = new List<HourlyContribution>();
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDateTime });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDateTime });
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

   

    private async Task ResetTicksStatusAsync()
    {
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("ResetTicksStatus", connection) { CommandType = CommandType.StoredProcedure };
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    

    
    private async Task<List<HourlyContribution>> GetMissedTicksContributionsAsync(DateTime startDateTime, DateTime endDateTime)
    {
        var result = new List<HourlyContribution>();
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetMissedTicksContributions", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDateTime });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDateTime });
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var hour = reader.GetInt64(0);
            var dateTime = reader.GetDateTime(1);
            var hasContribution = reader.GetBoolean(2);
            result.Add(new HourlyContribution
            {
                Hour = hour,
                DateTime = dateTime,
                HasContribution = hasContribution
            });
        }

        return result;
    }

    

   

   
    private static string GetDatabaseName(int yearNumber, int weekNumber)
    {
        throw new NotImplementedException();
        //return $"{environment.ToString().ToLower()}.{modification.ToString().ToLower()}.{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}";
    }

    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";

    private static string GetDatabaseName(int yearNumber, int weekNumber, Entity entity) => $"{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}.{entity.ToString().ToLower()}";
}

//private Queue<Quotation> _firstQuotations = null!;
//private Queue<Quotation> _quotations = null!;

//public async Task StartAsync()
//{
//    var initializeTasks = _firstQuotations.Select(quotation => _processor.InitializeAsync(quotation)).ToArray();
//    await Task.WhenAll(initializeTasks).ConfigureAwait(false);
//    _firstQuotations.Clear();

//    while (_quotations.Any())
//    {
//        var quotation = _quotations.Dequeue();
//        await _processor.TickAsync(quotation).ConfigureAwait(false);
//    }
//}

//public async Task InitializeAsync()
//{
//    const Environment environment = Environment.Testing;
//    const Modification inputModification = Modification.UnModified;
//    const int year = 2023;
//    const int week = 8;
//    const int day = 1;

//    var (firstQuotations, quotations) = await GetQuotationsForDayAsync(year, week, day, environment, inputModification).ConfigureAwait(false);
//    _firstQuotations = firstQuotations;
//    _quotations = quotations;
//}
