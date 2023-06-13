/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
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
    private const Entity TicksEntity = Entity.Ticks;
    private readonly HashSet<DateTime> _excludedDates;
    
    private readonly IFileService _fileService;
    private readonly ILogger<DataService> _logger;
    private readonly IAppNotificationService _notificationService;
    private readonly IProcessor _processor;

    private readonly string _server;
    private readonly string _solutionDatabase;
    private readonly DateTime _startDateTimeUtc;
    private IList<YearlyContribution>? _yearlyContributionsCache;
    private IDispatcherService _dispatcherService; //todo

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
    }

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

    public async Task<IEnumerable<SymbolicContribution>> GetSymbolicContributionsAsync(DateTimeOffset dateTimeOffset)
    {
        var dateTime = dateTimeOffset.DateTime.Date;
        var year = dateTime.Year;
        var week = dateTime.Week();
        var contributions = await GetTicksContributionsAsync(dateTime, dateTime.AddDays(1)).ConfigureAwait(true);

        IList<DateTime> dateTimes = contributions.Select(contribution => contribution.DateTime).ToList();
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
            symbolicContribution.Contribution = DetermineContributionStatus(symbolicContribution.HourlyContributions);
        }

        return symbolicContributions;
    }

    public async Task RecalculateTicksContributionsSelectedDayAsync(DateTime dateTime, CancellationToken cancellationToken)
    {

        var tmp = _fileService.GetTicksAsync(dateTime, dateTime);
        //var startDateTime = dateTime.Date;
        //var endDateTime = startDateTime.AddDays(1);
        //var missedHourlyContributions = await GetMissedTicksContributionsAsync(startDateTime, endDateTime).ConfigureAwait(true);
        //if (missedHourlyContributions.Count == 0)
        //{
        //    throw new NotImplementedException();

        //}
        //else
        //{
        //    var quotations = await _fileService.GetTicksMonthlyAllAsync(dateTime).ConfigureAwait(true);
        //    var missedHours = new HashSet<DateTime>(missedHourlyContributions.Select(mc => new DateTime(mc.DateTime.Year, mc.DateTime.Month, mc.DateTime.Day, mc.DateTime.Hour, 0, 0)));
        //    var filteredQuotations = quotations
        //        .Where(quotation => missedHours.Contains(new DateTime(quotation.DateTime.Year, quotation.DateTime.Month, quotation.DateTime.Day, quotation.DateTime.Hour, 0, 0)))
        //        .OrderBy(quotation => quotation.DateTime)
        //        .ToList();




        //    if (filteredQuotations.Count == 0)
        //    {
        //        throw new NotImplementedException();
        //    }
        //    else
        //    {

        //    }


        //}
    }

    public async Task RecalculateTicksContributionsAllAsync(CancellationToken ctsToken)
    {
        throw new NotImplementedException();

        //_yearlyContributionsCache = null;
        ////await ResetTicksStatusAsync().ConfigureAwait(true);//do not do it
        //var processedItems = 0;
        ////var originalMissedContributions = await GetMissedTicksContributionsAsync().ConfigureAwait(true);
        //var totalItems = originalMissedContributions.Count;
        //var missedContributionsYearly = originalMissedContributions
        //    .GroupBy(hc => hc.Year)
        //    .Select(yearGroup => new YearlyContribution
        //    {
        //        Year = yearGroup.Key,
        //        WeeklyContributions = new ObservableCollection<WeeklyContribution>(yearGroup
        //            .GroupBy(hc => hc.Week)
        //            .Select(weekGroup => new WeeklyContribution
        //            {
        //                Year = yearGroup.Key,
        //                Week = weekGroup.Key,
        //                DailyContributions = new ObservableCollection<DailyContribution>(weekGroup
        //                    .GroupBy(hc => new { hc.DateTime.Year, hc.DateTime.Month, hc.DateTime.Day })
        //                    .Select(dayGroup => new DailyContribution
        //                    {
        //                        Year = dayGroup.Key.Year,
        //                        Month = dayGroup.Key.Month,
        //                        Week = weekGroup.Key,
        //                        Day = dayGroup.Key.Day,
        //                        Contribution = null, // Set as null if not known at this stage
        //                        HourlyContributions = new List<HourlyContribution>(dayGroup.ToList())
        //                    }).ToList())
        //            }).ToList()),
        //        MonthlyContributions = null // Set as null if not required at this stage
        //    })
        //    .ToList();

        //ctsToken.ThrowIfCancellationRequested();

        //foreach (var myc in missedContributionsYearly)
        //{
        //    foreach (var mwc in myc.WeeklyContributions!)
        //    {
        //        ctsToken.ThrowIfCancellationRequested();

        //        var dateTimes = mwc.DailyContributions.SelectMany(dc => dc.HourlyContributions).Select(hc => hc.DateTime).ToList();

        //        processedItems += dateTimes.Count;
        //        var progressPercentage = processedItems * 100 / totalItems;
        //        Messenger.Send(new ProgressReportMessage(progressPercentage), DataServiceToken.Progress);

        //        var sampleTicks = await GetSampleTicksAsync(dateTimes, mwc.Year, mwc.Week).ConfigureAwait(true);
        //        var sampleTicksHourly = sampleTicks.GroupBy(q => new DateTime(q.DateTime.Year, q.DateTime.Month, q.DateTime.Day, q.DateTime.Hour, 0, 0)).ToList();
        //        var countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
        //        var completedHourNumbers = (from @group in sampleTicksHourly
        //            let distinctSymbolsInGroup = @group.Select(q => q.Symbol).Distinct().Count()
        //            where distinctSymbolsInGroup == countOfSymbols
        //            select (long)(@group.Key - DateTimeExtensionsAndHelpers.EpochStartDateTimeUtc).TotalHours).ToList();
        //        if (completedHourNumbers.Count == 0)
        //        {
        //            continue;
        //        }

        //        await UpdateTicksContributionsAsync(completedHourNumbers).ConfigureAwait(true);

        //        var completedHoursSet = new HashSet<long>(completedHourNumbers);
        //        foreach (var dailyContribution in mwc.DailyContributions)
        //        {
        //            if (!dailyContribution.HourlyContributions.Any(hc => completedHoursSet.Contains(hc.Hour)))
        //            {
        //                continue;
        //            }

        //            foreach (var hourlyContribution in dailyContribution.HourlyContributions.Where(hourlyContribution => completedHoursSet.Contains(hourlyContribution.Hour)))
        //            {
        //                hourlyContribution.HasContribution = true;
        //            }

        //            dailyContribution.Contribution = DetermineContributionStatus(dailyContribution.HourlyContributions);
        //            Messenger.Send(new DailyContributionChangedMessage(dailyContribution), DataServiceToken.DataToUpdate);
        //            ctsToken.ThrowIfCancellationRequested();
        //        }
        //    }
        //}
    }

    public async Task ImportTicksAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken).ConfigureAwait(true);
        throw new NotImplementedException();

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
        //            var quotationsToSave = weekGroup.OrderBy(quotation => quotation.DateTime).ToList();
        //            _logger.LogInformation($"ImportTicksAsync --> year: {yearNumber}, week{weekNumber}");
        //            await SavesTicksAsync(quotationsToSave, yearNumber, weekNumber).ConfigureAwait(false);
        //            throw new NotImplementedException();
        //            //Messenger.Send(new DataServiceMessage(), DataServiceToken.Ticks);

        //        }).ToList();

        //        await Task.WhenAll(tasks).ConfigureAwait(false);
        //        cancellationToken.ThrowIfCancellationRequested();
        //    }
        //}
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
    public async Task<IEnumerable<Quotation>> GetImportTicksAsync(Symbol symbol, DateTime startDateTime, DateTime endDateTime)
    {
        var quotations = await _fileService.GetTicksAsync(startDateTime, endDateTime).ConfigureAwait(true);
        var filteredQuotations = quotations.Where(quotation => quotation.Symbol == symbol);
        return filteredQuotations;
    }

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

    public async Task<ObservableCollection<SymbolicContribution>> GetSymbolicContributionsAsync1(DateTimeOffset dateTimeOffset)
    {
        var dateTime = dateTimeOffset.DateTime.Date;
        var year = dateTime.Year;
        var week = dateTime.Week();
        var contributions = await GetTicksContributionsAsync(dateTime, dateTime.AddDays(1)).ConfigureAwait(true);
        IList<DateTime> dateTimes = contributions.Select(contribution => contribution.DateTime).ToList();

        var sampleTicks = await GetSampleTicksAsync(dateTimes, year, week).ConfigureAwait(true);
        var groupBySymbol = sampleTicks.GroupBy(tick => tick.Symbol);
        var symbolicContributions = new ObservableCollection<SymbolicContribution>();

        foreach (var group in groupBySymbol)
        {
            var firstQuotationDateTime = group.First().DateTime;
            var weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(firstQuotationDateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var symbolicContribution = new SymbolicContribution
            {
                Symbol = group.Key,
                Year = firstQuotationDateTime.Year,
                Month = firstQuotationDateTime.Month,
                Week = weekNum,
                Day = firstQuotationDateTime.Day,
                HourlyContributions = new List<HourlyContribution>()
            };

            var groupByHour = group.GroupBy(tick => tick.DateTime.Hour).ToDictionary(g => g.Key, g => g.ToList());

            for (var hour = 0; hour < 24; hour++)
            {
                if (groupByHour.TryGetValue(hour, out var hourGroup))
                {
                    var hourlyContribution = new HourlyContribution
                    {
                        DateTime = hourGroup.First().DateTime,
                        Hour = hour,
                        HasContribution = true
                    };

                    symbolicContribution.HourlyContributions.Add(hourlyContribution);
                }
                else
                {
                    var hourlyContribution = new HourlyContribution
                    {
                        DateTime = new DateTime(firstQuotationDateTime.Year, firstQuotationDateTime.Month, firstQuotationDateTime.Day, hour, 0, 0),
                        Hour = hour,
                        HasContribution = false
                    };

                    symbolicContribution.HourlyContributions.Add(hourlyContribution);
                }
            }

            symbolicContribution.Contribution = DetermineContributionStatus(symbolicContribution.HourlyContributions);
            symbolicContributions.Add(symbolicContribution);
        }

        return symbolicContributions;
    }

    private async Task ResetTicksStatusAsync()
    {
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("ResetTicksStatus", connection) { CommandType = CommandType.StoredProcedure };
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
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
                        Contribution = DetermineContributionStatus(hourlyContributions),
                        HourlyContributions = hourlyContributions
                    };

                    monthlyContribution.DailyContributions.Add(dailyContribution);
                }

                yearlyContribution.MonthlyContributions.Add(monthlyContribution);
            }

            _yearlyContributionsCache.Add(yearlyContribution);
        }
    }

    private Contribution DetermineContributionStatus(IReadOnlyList<HourlyContribution> hourlyContributions)
    {
        if (_excludedDates.Contains(hourlyContributions[0].DateTime.Date) || hourlyContributions[0].DateTime.DayOfWeek == DayOfWeek.Saturday)
        {
            return Contribution.Excluded;
        }

        switch (hourlyContributions[0].DateTime.DayOfWeek)
        {
            case DayOfWeek.Friday when hourlyContributions.SkipLast(2).All(c => c.HasContribution):
            case DayOfWeek.Sunday when hourlyContributions.TakeLast(2).All(c => c.HasContribution):
                return Contribution.Full;
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Saturday:
            default:
            {
                if (hourlyContributions.All(c => c.HasContribution))
                {
                    return Contribution.Full;
                }

                return hourlyContributions.Any(c => c.HasContribution) ? Contribution.Partial : Contribution.None;
            }
        }
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

    private async Task<IEnumerable<Quotation>> GetSampleTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber)
    {
        var result = new List<Quotation>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var unContributedDateTime in dateTimes)
        {
            dataTable.Rows.Add(unContributedDateTime);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
        await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = new SqlCommand("GetFirstTicksInHourByWeek", connection) { CommandType = CommandType.StoredProcedure };
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

    private async Task SavesTicksAsync(IList<Quotation> quotationsToSave, int yearNumber, int weekNumber)
    {
        var tableName = GetTableName(weekNumber);
        DateTimeExtensionsAndHelpers.Check_ISO_8601(yearNumber, weekNumber, quotationsToSave);

        var dataTable = new DataTable();
        dataTable.Columns.Add("Symbol", typeof(int));
        dataTable.Columns.Add("DateTime", typeof(DateTime));
        dataTable.Columns.Add("Ask", typeof(double));
        dataTable.Columns.Add("Bid", typeof(double));
        foreach (var quotation in quotationsToSave)
        {
            dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);
        }

        var databaseName = GetDatabaseName(yearNumber, weekNumber, TicksEntity);
        var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction($"week:{weekNumber:00}");
        await using var command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
        command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
        command.CommandTimeout = 0;

        try
        {
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            transaction.Commit();
            _notificationService.Show($"Week:{weekNumber:00} {quotationsToSave.Count:##,###} quotations saved.");
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

            throw;
        }
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