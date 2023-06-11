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
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;
using Terminal.WinUI3.Services.Messenger.Messages;
using Environment = Common.Entities.Environment;

namespace Terminal.WinUI3.Services;

public class DataService : ObservableRecipient, IDataService
{
    private const Entity TicksEntity = Entity.Ticks;
    private readonly DateTime _epochStartDateTimeUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly HashSet<DateTime> _excludedDates;

    private readonly Dictionary<string, List<Quotation>> _fileTicksCache = new();
    private readonly string[] _formats;
    private readonly string _inputDirectoryPath;
    private readonly ILogger<DataService> _logger;
    private readonly IAppNotificationService _notificationService;
    private readonly IProcessor _processor;

    private readonly string _server;
    private readonly string _solutionDatabase;
    private readonly DateTime _startDateTimeUtc;
    private ObservableCollection<YearlyContribution>? _cache;
    private IDispatcherService _dispatcherService; //todo

    public DataService(IProcessor processor, IConfiguration configuration, IAppNotificationService notificationService, ILogger<DataService> logger, IDispatcherService dispatcherService) //todo
    {
        _processor = processor;
        _notificationService = notificationService;
        _logger = logger;
        _dispatcherService = dispatcherService; //todo

        _server = configuration.GetConnectionString("Server")!;
        _solutionDatabase = configuration.GetConnectionString("SolutionDatabase")!;
        _inputDirectoryPath = configuration.GetValue<string>("InputDirectoryPath")!;
        _formats = new[] { configuration.GetValue<string>("DucascopyTickstoryDateTimeFormat")! };
        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>("StartDate")!, _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _excludedDates = new HashSet<DateTime>(configuration.GetSection("ExcludedDates").Get<List<DateTime>>()!);
    }

    public async Task<ObservableCollection<YearlyContribution>> GetYearlyContributionsAsync()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var list = await GetTicksContributionsAsync(_startDateTimeUtc, DateTime.Now).ConfigureAwait(true);
        CreateGroups(list);
        return _cache!;
    }

    public async Task<ObservableCollection<SymbolicContribution>> GetSymbolicContributionsAsync(DateTimeOffset dateTimeOffset)
    {
        var dateTime = dateTimeOffset.DateTime.Date;
        var year = dateTime.Year;
        var week = GetWeekNumber(dateTime);
        var contributions = await GetTicksContributionsAsync(dateTime, dateTime.AddDays(1)).ConfigureAwait(true);

        IList<DateTime> dateTimes = contributions.Select(contribution => contribution.DateTime).ToList();
        var sampleTicks = await GetSampleTicksAsync(dateTimes, year, week).ConfigureAwait(true);

        var symbolicContributions = new ObservableCollection<SymbolicContribution>();

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

    public async Task RecalculateTicksContributionsAsync(CancellationToken ctsToken)
    {
        _cache = null;
        await ResetTicksStatusAsync().ConfigureAwait(true);
        var processedItems = 0;
        var originalMissedContributions = await GetMissedTicksContributionsAsync().ConfigureAwait(true);
        var totalItems = originalMissedContributions.Count;
        var missedContributionsYearly = originalMissedContributions
            .GroupBy(hc => hc.Year)
            .Select(yearGroup => new YearlyContribution
            {
                Year = yearGroup.Key,
                WeeklyContributions = new ObservableCollection<WeeklyContribution>(yearGroup
                    .GroupBy(hc => hc.Week)
                    .Select(weekGroup => new WeeklyContribution
                    {
                        Year = yearGroup.Key,
                        Week = weekGroup.Key,
                        DailyContributions = new ObservableCollection<DailyContribution>(weekGroup
                            .GroupBy(hc => new { hc.DateTime.Year, hc.DateTime.Month, hc.DateTime.Day })
                            .Select(dayGroup => new DailyContribution
                            {
                                Year = dayGroup.Key.Year,
                                Month = dayGroup.Key.Month,
                                Week = weekGroup.Key,
                                Day = dayGroup.Key.Day,
                                Contribution = null, // Set as null if not known at this stage
                                HourlyContributions = new List<HourlyContribution>(dayGroup.ToList())
                            }).ToList())
                    }).ToList()),
                MonthlyContributions = null // Set as null if not required at this stage
            })
            .ToList();

        ctsToken.ThrowIfCancellationRequested();

        foreach (var myc in missedContributionsYearly)
        {
            foreach (var mwc in myc.WeeklyContributions!)
            {
                ctsToken.ThrowIfCancellationRequested();

                var dateTimes = mwc.DailyContributions.SelectMany(dc => dc.HourlyContributions).Select(hc => hc.DateTime).ToList();

                processedItems += dateTimes.Count;
                var progressPercentage = processedItems * 100 / totalItems;
                Messenger.Send(new ProgressReportMessage(progressPercentage), DataServiceToken.Progress);

                var sampleTicks = await GetSampleTicksAsync(dateTimes, mwc.Year, mwc.Week).ConfigureAwait(true);
                var sampleTicksHourly = sampleTicks.GroupBy(q => new DateTime(q.DateTime.Year, q.DateTime.Month, q.DateTime.Day, q.DateTime.Hour, 0, 0)).ToList();
                var countOfSymbols = Enum.GetValues(typeof(Symbol)).Length;
                var completedHourNumbers = (from @group in sampleTicksHourly
                    let distinctSymbolsInGroup = @group.Select(q => q.Symbol).Distinct().Count()
                    where distinctSymbolsInGroup == countOfSymbols
                    select (long)(@group.Key - _epochStartDateTimeUtc).TotalHours).ToList();
                if (completedHourNumbers.Count == 0)
                {
                    continue;
                }

                await UpdateTicksContributionsAsync(completedHourNumbers).ConfigureAwait(true);

                var completedHoursSet = new HashSet<long>(completedHourNumbers);
                foreach (var dailyContribution in mwc.DailyContributions)
                {
                    if (!dailyContribution.HourlyContributions.Any(hc => completedHoursSet.Contains(hc.Hour)))
                    {
                        continue;
                    }

                    foreach (var hourlyContribution in dailyContribution.HourlyContributions.Where(hourlyContribution => completedHoursSet.Contains(hourlyContribution.Hour)))
                    {
                        hourlyContribution.HasContribution = true;
                    }

                    dailyContribution.Contribution = DetermineContributionStatus(dailyContribution.HourlyContributions);
                    Messenger.Send(new DailyContributionChangedMessage(dailyContribution), DataServiceToken.DataToUpdate);
                    ctsToken.ThrowIfCancellationRequested();
                }
            }
        }
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
    public async Task<IEnumerable<Quotation>> GetTicksAsync(Symbol symbol, DateTimeOffset dateTimeOffset, TimeSpan selectedTime)
    {
        var cacheKey = $"{symbol}_{dateTimeOffset.Year}_{dateTimeOffset.Month}";

        if (!_fileTicksCache.ContainsKey(cacheKey))
        {
            await LoadFileAsync(symbol, dateTimeOffset).ConfigureAwait(true);
        }

        var filteredQuotations = _fileTicksCache[cacheKey].Where(quotation =>
            quotation.Symbol == symbol &&
            quotation.DateTime.Date == dateTimeOffset.Date &&
            quotation.DateTime.TimeOfDay.Hours == selectedTime.Hours
        );

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
        var week = GetWeekNumber(dateTime);
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

    private async Task LoadFileAsync(Symbol symbol, DateTimeOffset dateTimeOffset)
    {
        var cacheKey = $"{symbol}_{dateTimeOffset.Year}_{dateTimeOffset.Month}";
        var symbolsDict = Enum.GetValues(typeof(Symbol)).Cast<Symbol>().ToDictionary(e => e.ToString(), e => e);
        var year = dateTimeOffset.Year;
        var month = dateTimeOffset.Month;

        var symbolDirectory = Directory.GetDirectories(Path.Combine(_inputDirectoryPath, year.ToString()))
                                  .FirstOrDefault(directory => Path.GetFileName(directory) == symbol.ToString())
                              ?? throw new InvalidOperationException($"Symbol directory not found for {symbol} and year {year}");

        var fileName = $"{symbol}.{year}.{month:D2}.csv";
        var filePath = Path.Combine(symbolDirectory, fileName);

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"File not found: {filePath}");
        }

        var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(true);
        var quotations = lines.Skip(1)
            .AsParallel()
            .Select(line => line.Split(',').Select(str => str.Trim()).ToArray())
            .Select((items, index) =>
            {
                var quotationId = index + 1;
                var symbolResult = symbolsDict[items[0]];
                var dateTimeResult = DateTime.ParseExact(items[1], _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
                var askResult = double.Parse(items[2]);
                var bidResult = double.Parse(items[3]);
                return new Quotation(quotationId, symbolResult, dateTimeResult, askResult, bidResult);
            })
            .OrderBy(quotation => quotation.DateTime)
            .ToList();

        _fileTicksCache[cacheKey] = quotations;
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
        _cache = new ObservableCollection<YearlyContribution>();
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

            _cache.Add(yearlyContribution);
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

    private async Task<List<HourlyContribution>> GetMissedTicksContributionsAsync()
    {
        var result = new List<HourlyContribution>();
        await using var connection = new SqlConnection($"{_server};Database={_solutionDatabase};Trusted_Connection=True;");
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetMissedTicksContributions", connection);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = _startDateTimeUtc });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = DateTime.Now });
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

    private IEnumerable<Quotation> GetTicksFromFile(int year, int month, IList<DateTime> missedDates)
    {
        var symbolDirectories = Directory.GetDirectories(Path.Combine(_inputDirectoryPath, year.ToString()));
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\EURGBP
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\EURJPY
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\EURUSD
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\GBPJPY
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\GBPUSD
        //D:\forex\tickstory\tickstory.com.data.symbolic\2022\USDJPY
        //PS D:\forex\tickstory\tickstory.com.data.symbolic\2022\EURGBP > dir
        //Directory: D:\forex\tickstory\tickstory.com.data.symbolic\2022\EURGBP
        //Mode                 LastWriteTime Length Name
        //---------------- - ----------
        //- ar-- - 2023 - 04 - 28   7:39 PM       76119567 EURGBP.2022.01.csv <--- Format: Symbol.YEAR.Month.csv
        //- ar-- - 2023 - 04 - 28   7:41 PM       85154886 EURGBP.2022.02.csv
        //- ar-- - 2023 - 04 - 28   7:43 PM      118479086 EURGBP.2022.03.csv
        //- ar-- - 2023 - 04 - 28   7:44 PM       94936307 EURGBP.2022.04.csv
        //- ar-- - 2023 - 04 - 28   7:46 PM      110459706 EURGBP.2022.05.csv
        //- ar-- - 2023 - 04 - 28   7:47 PM      132576217 EURGBP.2022.06.csv
        //- ar-- - 2023 - 04 - 28   7:49 PM      142310720 EURGBP.2022.07.csv
        //- ar-- - 2023 - 04 - 28   7:51 PM      141602323 EURGBP.2022.08.csv
        //- ar-- - 2023 - 04 - 28   7:52 PM      213100972 EURGBP.2022.09.csv
        //- ar-- - 2023 - 04 - 28   7:54 PM      230236936 EURGBP.2022.10.csv
        //- ar-- - 2023 - 04 - 28   7:55 PM      174291766 EURGBP.2022.11.csv
        //- ar-- - 2023 - 04 - 28   7:57 PM      145776951 EURGBP.2022.12.csv

        var allLines = new ConcurrentBag<string>();
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = System.Environment.ProcessorCount };
        Parallel.ForEach(symbolDirectories, parallelOptions, symbolDirectory =>
        {
            var file = Directory.GetFiles(symbolDirectory, $"*.{year}.{month:D2}.csv").FirstOrDefault();
            if (file == null)
            {
                return;
            }

            var lines = File.ReadAllLines(file).Skip(1);
            foreach (var line in lines)
            {
                allLines.Add(line);
            }
        });

        var id = 0;
        var symbolsDict = Enum.GetValues(typeof(Symbol)).Cast<Symbol>().ToDictionary(e => e.ToString(), e => e);
        var quotations = allLines.ToList().AsParallel()
            .Select(line => line.Split(',').Select(str => str.Trim()).ToArray())
            .Select(items =>
            {
                var quotationId = Interlocked.Increment(ref id); // This will be thread-safe
                var symbol = symbolsDict[items[0]];
                var dateTime = DateTime.ParseExact(items[1], _formats, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
                var ask = double.Parse(items[2]);
                var bid = double.Parse(items[3]);
                return new Quotation(quotationId, symbol, dateTime, ask, bid);
            }).OrderBy(quotation => quotation.DateTime).ToList();

        var filteredQuotations = quotations.Where(quotation => missedDates.Contains(quotation.DateTime.Date)).ToList();
        return filteredQuotations;
    }

    private async Task SavesTicksAsync(IList<Quotation> quotationsToSave, int yearNumber, int weekNumber)
    {
        var tableName = GetTableName(weekNumber);
        Check_ISO_8601(yearNumber, weekNumber, quotationsToSave);

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

    private static string GetDatabaseName(int yearNumber, int weekNumber, Environment environment, Modification modification) =>
        $"{environment.ToString().ToLower()}.{modification.ToString().ToLower()}.{yearNumber}.{GetQuarterNumber(weekNumber)}";

    private static int GetQuarterNumber(int weekNumber)
    {
        const string errorMessage = $"{nameof(weekNumber)} is out of range.";
        return weekNumber switch
        {
            <= 0 => throw new Exception(errorMessage),
            <= 13 => 1,
            <= 26 => 2,
            <= 39 => 3,
            <= 52 => 4,
            _ => throw new Exception(errorMessage)
        };
    }

    private static int GetWeekNumber(DateTime date)
    {
        var weekNum = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return weekNum;
    }

    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";

    private static string GetDatabaseName(int yearNumber, int weekNumber, Entity entity) => $"{yearNumber}.{GetQuarterNumber(weekNumber)}.{entity.ToString().ToLower()}";

    private static void Check_ISO_8601(int yearNumber, int weekNumber, IList<Quotation> list)
    {
        var start = list[0].DateTime.Date;
        var end = list[^1].DateTime.Date;

        switch (yearNumber, weekNumber)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            case (2022, 1):
                Debug.Assert(start >= DateTime.Parse("Monday, January 3, 2022") && end <= DateTime.Parse("Sunday, January 9, 2022"));
                break;
            case (2022, 2):
                Debug.Assert(start >= DateTime.Parse("Monday, January 10, 2022") && end <= DateTime.Parse("Sunday, January 16, 2022"));
                break;
            case (2022, 3):
                Debug.Assert(start >= DateTime.Parse("Monday, January 17, 2022") && end <= DateTime.Parse("Sunday, January 23, 2022"));
                break;
            case (2022, 4):
                Debug.Assert(start >= DateTime.Parse("Monday, January 24, 2022") && end <= DateTime.Parse("Sunday, January 30, 2022"));
                break;
            case (2022, 5):
                Debug.Assert(start >= DateTime.Parse("Monday, January 31, 2022") && end <= DateTime.Parse("Sunday, February 6, 2022"));
                break;
            case (2022, 6):
                Debug.Assert(start >= DateTime.Parse("Monday, February 7, 2022") && end <= DateTime.Parse("Sunday, February 13, 2022"));
                break;
            case (2022, 7):
                Debug.Assert(start >= DateTime.Parse("Monday, February 14, 2022") && end <= DateTime.Parse("Sunday, February 20, 2022"));
                break;
            case (2022, 8):
                Debug.Assert(start >= DateTime.Parse("Monday, February 21, 2022") && end <= DateTime.Parse("Sunday, February 27, 2022"));
                break;
            case (2022, 9):
                Debug.Assert(start >= DateTime.Parse("Monday, February 28, 2022") && end <= DateTime.Parse("Sunday, March 6, 2022"));
                break;
            case (2022, 10):
                Debug.Assert(start >= DateTime.Parse("Monday, March 7, 2022") && end <= DateTime.Parse("Sunday, March 13, 2022"));
                break;
            case (2022, 11):
                Debug.Assert(start >= DateTime.Parse("Monday, March 14, 2022") && end <= DateTime.Parse("Sunday, March 20, 2022"));
                break;
            case (2022, 12):
                Debug.Assert(start >= DateTime.Parse("Monday, March 21, 2022") && end <= DateTime.Parse("Sunday, March 27, 2022"));
                break;
            case (2022, 13):
                Debug.Assert(start >= DateTime.Parse("Monday, March 28, 2022") && end <= DateTime.Parse("Sunday, April 3, 2022"));
                break;
            case (2022, 14):
                Debug.Assert(start >= DateTime.Parse("Monday, April 4, 2022") && end <= DateTime.Parse("Sunday, April 10, 2022"));
                break;
            case (2022, 15):
                Debug.Assert(start >= DateTime.Parse("Monday, April 11, 2022") && end <= DateTime.Parse("Sunday, April 17, 2022"));
                break;
            case (2022, 16):
                Debug.Assert(start >= DateTime.Parse("Monday, April 18, 2022") && end <= DateTime.Parse("Sunday, April 24, 2022"));
                break;
            case (2022, 17):
                Debug.Assert(start >= DateTime.Parse("Monday, April 25, 2022") && end <= DateTime.Parse("Sunday, May 1, 2022"));
                break;
            case (2022, 18):
                Debug.Assert(start >= DateTime.Parse("Monday, May 2, 2022") && end <= DateTime.Parse("Sunday, May 8, 2022"));
                break;
            case (2022, 19):
                Debug.Assert(start >= DateTime.Parse("Monday, May 9, 2022") && end <= DateTime.Parse("Sunday, May 15, 2022"));
                break;
            case (2022, 20):
                Debug.Assert(start >= DateTime.Parse("Monday, May 16, 2022") && end <= DateTime.Parse("Sunday, May 22, 2022"));
                break;
            case (2022, 21):
                Debug.Assert(start >= DateTime.Parse("Monday, May 23, 2022") && end <= DateTime.Parse("Sunday, May 29, 2022"));
                break;
            case (2022, 22):
                Debug.Assert(start >= DateTime.Parse("Monday, May 30, 2022") && end <= DateTime.Parse("Sunday, June 5, 2022"));
                break;
            case (2022, 23):
                Debug.Assert(start >= DateTime.Parse("Monday, June 6, 2022") && end <= DateTime.Parse("Sunday, June 12, 2022"));
                break;
            case (2022, 24):
                Debug.Assert(start >= DateTime.Parse("Monday, June 13, 2022") && end <= DateTime.Parse("Sunday, June 19, 2022"));
                break;
            case (2022, 25):
                Debug.Assert(start >= DateTime.Parse("Monday, June 20, 2022") && end <= DateTime.Parse("Sunday, June 26, 2022"));
                break;
            case (2022, 26):
                Debug.Assert(start >= DateTime.Parse("Monday, June 27, 2022") && end <= DateTime.Parse("Sunday, July 3, 2022"));
                break;
            case (2022, 27):
                Debug.Assert(start >= DateTime.Parse("Monday, July 4, 2022") && end <= DateTime.Parse("Sunday, July 10, 2022"));
                break;
            case (2022, 28):
                Debug.Assert(start >= DateTime.Parse("Monday, July 11, 2022") && end <= DateTime.Parse("Sunday, July 17, 2022"));
                break;
            case (2022, 29):
                Debug.Assert(start >= DateTime.Parse("Monday, July 18, 2022") && end <= DateTime.Parse("Sunday, July 24, 2022"));
                break;
            case (2022, 30):
                Debug.Assert(start >= DateTime.Parse("Monday, July 25, 2022") && end <= DateTime.Parse("Sunday, July 31, 2022"));
                break;
            case (2022, 31):
                Debug.Assert(start >= DateTime.Parse("Monday, August 1, 2022") && end <= DateTime.Parse("Sunday, August 7, 2022"));
                break;
            case (2022, 32):
                Debug.Assert(start >= DateTime.Parse("Monday, August 8, 2022") && end <= DateTime.Parse("Sunday, August 14, 2022"));
                break;
            case (2022, 33):
                Debug.Assert(start >= DateTime.Parse("Monday, August 15, 2022") && end <= DateTime.Parse("Sunday, August 21, 2022"));
                break;
            case (2022, 34):
                Debug.Assert(start >= DateTime.Parse("Monday, August 22, 2022") && end <= DateTime.Parse("Sunday, August 28, 2022"));
                break;
            case (2022, 35):
                Debug.Assert(start >= DateTime.Parse("Monday, August 29, 2022") && end <= DateTime.Parse("Sunday, September 4, 2022"));
                break;
            case (2022, 36):
                Debug.Assert(start >= DateTime.Parse("Monday, September 5, 2022") && end <= DateTime.Parse("Sunday, September 11, 2022"));
                break;
            case (2022, 37):
                Debug.Assert(start >= DateTime.Parse("Monday, September 12, 2022") && end <= DateTime.Parse("Sunday, September 18, 2022"));
                break;
            case (2022, 38):
                Debug.Assert(start >= DateTime.Parse("Monday, September 19, 2022") && end <= DateTime.Parse("Sunday, September 25, 2022"));
                break;
            case (2022, 39):
                Debug.Assert(start >= DateTime.Parse("Monday, September 26, 2022") && end <= DateTime.Parse("Sunday, October 2, 2022"));
                break;
            case (2022, 40):
                Debug.Assert(start >= DateTime.Parse("Monday, October 3, 2022") && end <= DateTime.Parse("Sunday, October 9, 2022"));
                break;
            case (2022, 41):
                Debug.Assert(start >= DateTime.Parse("Monday, October 10, 2022") && end <= DateTime.Parse("Sunday, October 16, 2022"));
                break;
            case (2022, 42):
                Debug.Assert(start >= DateTime.Parse("Monday, October 17, 2022") && end <= DateTime.Parse("Sunday, October 23, 2022"));
                break;
            case (2022, 43):
                Debug.Assert(start >= DateTime.Parse("Monday, October 24, 2022") && end <= DateTime.Parse("Sunday, October 30, 2022"));
                break;
            case (2022, 44):
                Debug.Assert(start >= DateTime.Parse("Monday, October 31, 2022") && end <= DateTime.Parse("Sunday, November 6, 2022"));
                break;
            case (2022, 45):
                Debug.Assert(start >= DateTime.Parse("Monday, November 7, 2022") && end <= DateTime.Parse("Sunday, November 13, 2022"));
                break;
            case (2022, 46):
                Debug.Assert(start >= DateTime.Parse("Monday, November 14, 2022") && end <= DateTime.Parse("Sunday, November 20, 2022"));
                break;
            case (2022, 47):
                Debug.Assert(start >= DateTime.Parse("Monday, November 21, 2022") && end <= DateTime.Parse("Sunday, November 27, 2022"));
                break;
            case (2022, 48):
                Debug.Assert(start >= DateTime.Parse("Monday, November 28, 2022") && end <= DateTime.Parse("Sunday, December 4, 2022"));
                break;
            case (2022, 49):
                Debug.Assert(start >= DateTime.Parse("Monday, December 5, 2022") && end <= DateTime.Parse("Sunday, December 11, 2022"));
                break;
            case (2022, 50):
                Debug.Assert(start >= DateTime.Parse("Monday, December 12, 2022") && end <= DateTime.Parse("Sunday, December 18, 2022"));
                break;
            case (2022, 51):
                Debug.Assert(start >= DateTime.Parse("Monday, December 19, 2022") && end <= DateTime.Parse("Sunday, December 25, 2022"));
                break;
            case (2022, 52):
                Debug.Assert(start >= DateTime.Parse("Monday, December 26, 2022") && end <= DateTime.Parse("Friday, December 30, 2022"));
                break;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            case (2023, 1):
                Debug.Assert(start >= DateTime.Parse("Monday, January 2, 2023") && end <= DateTime.Parse("Sunday, January 8, 2023"));
                break;
            case (2023, 2):
                Debug.Assert(start >= DateTime.Parse("Monday, January 9, 2023") && end <= DateTime.Parse("Sunday, January 15, 2023"));
                break;
            case (2023, 3):
                Debug.Assert(start >= DateTime.Parse("Monday, January 16, 2023") && end <= DateTime.Parse("Sunday, January 22, 2023"));
                break;
            case (2023, 4):
                Debug.Assert(start >= DateTime.Parse("Monday, January 23, 2023") && end <= DateTime.Parse("Sunday, January 29, 2023"));
                break;
            case (2023, 5):
                Debug.Assert(start >= DateTime.Parse("Monday, January 30, 2023") && end <= DateTime.Parse("Sunday, February 5, 2023"));
                break;
            case (2023, 6):
                Debug.Assert(start >= DateTime.Parse("Monday, February 6, 2023") && end <= DateTime.Parse("Sunday, February 12, 2023"));
                break;
            case (2023, 7):
                Debug.Assert(start >= DateTime.Parse("Monday, February 13, 2023") && end <= DateTime.Parse("Sunday, February 19, 2023"));
                break;
            case (2023, 8):
                Debug.Assert(start >= DateTime.Parse("Monday, February 20, 2023") && end <= DateTime.Parse("Sunday, February 26, 2023"));
                break;
            case (2023, 9):
                Debug.Assert(start >= DateTime.Parse("Monday, February 27, 2023") && end <= DateTime.Parse("Sunday, March 5, 2023"));
                break;
            case (2023, 10):
                Debug.Assert(start >= DateTime.Parse("Monday, March 6, 2023") && end <= DateTime.Parse("Sunday, March 12, 2023"));
                break;
            case (2023, 11):
                Debug.Assert(start >= DateTime.Parse("Monday, March 13, 2023") && end <= DateTime.Parse("Sunday, March 19, 2023"));
                break;
            case (2023, 12):
                Debug.Assert(start >= DateTime.Parse("Monday, March 20, 2023") && end <= DateTime.Parse("Sunday, March 26, 2023"));
                break;
            case (2023, 13):
                Debug.Assert(start >= DateTime.Parse("Monday, March 27, 2023") && end <= DateTime.Parse("Sunday, April 2, 2023"));
                break;
            case (2023, 14):
                Debug.Assert(start >= DateTime.Parse("Monday, April 3, 2023") && end <= DateTime.Parse("Sunday, April 9, 2023"));
                break;
            case (2023, 15):
                Debug.Assert(start >= DateTime.Parse("Monday, April 10, 2023") && end <= DateTime.Parse("Sunday, April 16, 2023"));
                break;
            case (2023, 16):
                Debug.Assert(start >= DateTime.Parse("Monday, April 17, 2023") && end <= DateTime.Parse("Sunday, April 23, 2023"));
                break;
            case (2023, 17):
                Debug.Assert(start >= DateTime.Parse("Monday, April 24, 2023") && end <= DateTime.Parse("Sunday, April 30, 2023"));
                break;
            case (2023, 18):
                Debug.Assert(start >= DateTime.Parse("Monday, May 1, 2023") && end <= DateTime.Parse("Sunday, May 7, 2023"));
                break;
            case (2023, 19):
                Debug.Assert(start >= DateTime.Parse("Monday, May 8, 2023") && end <= DateTime.Parse("Sunday, May 14, 2023"));
                break;
            case (2023, 20):
                Debug.Assert(start >= DateTime.Parse("Monday, May 15, 2023") && end <= DateTime.Parse("Sunday, May 21, 2023"));
                break;
            case (2023, 21):
                Debug.Assert(start >= DateTime.Parse("Monday, May 22, 2023") && end <= DateTime.Parse("Sunday, May 28, 2023"));
                break;
            case (2023, 22):
                Debug.Assert(start >= DateTime.Parse("Monday, May 29, 2023") && end <= DateTime.Parse("Sunday, June 4, 2023"));
                break;
            case (2023, 23):
                Debug.Assert(start >= DateTime.Parse("Monday, June 5, 2023") && end <= DateTime.Parse("Sunday, June 11, 2023"));
                break;
            case (2023, 24):
                Debug.Assert(start >= DateTime.Parse("Monday, June 12, 2023") && end <= DateTime.Parse("Sunday, June 18, 2023"));
                break;
            case (2023, 25):
                Debug.Assert(start >= DateTime.Parse("Monday, June 19, 2023") && end <= DateTime.Parse("Sunday, June 25, 2023"));
                break;
            case (2023, 26):
                Debug.Assert(start >= DateTime.Parse("Monday, June 26, 2023") && end <= DateTime.Parse("Sunday, July 2, 2023"));
                break;
            case (2023, 27):
                Debug.Assert(start >= DateTime.Parse("Monday, July 3, 2023") && end <= DateTime.Parse("Sunday, July 9, 2023"));
                break;
            case (2023, 28):
                Debug.Assert(start >= DateTime.Parse("Monday, July 10, 2023") && end <= DateTime.Parse("Sunday, July 16, 2023"));
                break;
            case (2023, 29):
                Debug.Assert(start >= DateTime.Parse("Monday, July 17, 2023") && end <= DateTime.Parse("Sunday, July 23, 2023"));
                break;
            case (2023, 30):
                Debug.Assert(start >= DateTime.Parse("Monday, July 24, 2023") && end <= DateTime.Parse("Sunday, July 30, 2023"));
                break;
            case (2023, 31):
                Debug.Assert(start >= DateTime.Parse("Monday, July 31, 2023") && end <= DateTime.Parse("Sunday, August 6, 2023"));
                break;
            case (2023, 32):
                Debug.Assert(start >= DateTime.Parse("Monday, August 7, 2023") && end <= DateTime.Parse("Sunday, August 13, 2023"));
                break;
            case (2023, 33):
                Debug.Assert(start >= DateTime.Parse("Monday, August 14, 2023") && end <= DateTime.Parse("Sunday, August 20, 2023"));
                break;
            case (2023, 34):
                Debug.Assert(start >= DateTime.Parse("Monday, August 21, 2023") && end <= DateTime.Parse("Sunday, August 27, 2023"));
                break;
            case (2023, 35):
                Debug.Assert(start >= DateTime.Parse("Monday, August 28, 2023") && end <= DateTime.Parse("Sunday, September 3, 2023"));
                break;
            case (2023, 36):
                Debug.Assert(start >= DateTime.Parse("Monday, September 4, 2023") && end <= DateTime.Parse("Sunday, September 10, 2023"));
                break;
            case (2023, 37):
                Debug.Assert(start >= DateTime.Parse("Monday, September 11, 2023") && end <= DateTime.Parse("Sunday, September 17, 2023"));
                break;
            case (2023, 38):
                Debug.Assert(start >= DateTime.Parse("Monday, September 18, 2023") && end <= DateTime.Parse("Sunday, September 24, 2023"));
                break;
            case (2023, 39):
                Debug.Assert(start >= DateTime.Parse("Monday, September 25, 2023") && end <= DateTime.Parse("Sunday, October 1, 2023"));
                break;
            case (2023, 40):
                Debug.Assert(start >= DateTime.Parse("Monday, October 2, 2023") && end <= DateTime.Parse("Sunday, October 8, 2023"));
                break;
            case (2023, 41):
                Debug.Assert(start >= DateTime.Parse("Monday, October 9, 2023") && end <= DateTime.Parse("Sunday, October 15, 2023"));
                break;
            case (2023, 42):
                Debug.Assert(start >= DateTime.Parse("Monday, October 16, 2023") && end <= DateTime.Parse("Sunday, October 22, 2023"));
                break;
            case (2023, 43):
                Debug.Assert(start >= DateTime.Parse("Monday, October 23, 2023") && end <= DateTime.Parse("Sunday, October 29, 2023"));
                break;
            case (2023, 44):
                Debug.Assert(start >= DateTime.Parse("Monday, October 30, 2023") && end <= DateTime.Parse("Sunday, November 5, 2023"));
                break;
            case (2023, 45):
                Debug.Assert(start >= DateTime.Parse("Monday, November 6, 2023") && end <= DateTime.Parse("Sunday, November 12, 2023"));
                break;
            case (2023, 46):
                Debug.Assert(start >= DateTime.Parse("Monday, November 13, 2023") && end <= DateTime.Parse("Sunday, November 19, 2023"));
                break;
            case (2023, 47):
                Debug.Assert(start >= DateTime.Parse("Monday, November 20, 2023") && end <= DateTime.Parse("Sunday, November 26, 2023"));
                break;
            case (2023, 48):
                Debug.Assert(start >= DateTime.Parse("Monday, November 27, 2023") && end <= DateTime.Parse("Sunday, December 3, 2023"));
                break;
            case (2023, 49):
                Debug.Assert(start >= DateTime.Parse("Monday, December 4, 2023") && end <= DateTime.Parse("Sunday, December 10, 2023"));
                break;
            case (2023, 50):
                Debug.Assert(start >= DateTime.Parse("Monday, December 11, 2023") && end <= DateTime.Parse("Sunday, December 17, 2023"));
                break;
            case (2023, 51):
                Debug.Assert(start >= DateTime.Parse("Monday, December 18, 2023") && end <= DateTime.Parse("Sunday, December 24, 2023"));
                break;
            case (2023, 52):
                Debug.Assert(start >= DateTime.Parse("Monday, December 25, 2023") && end <= DateTime.Parse("Sunday, December 31, 2023"));
                break;
            default: throw new Exception();
        }
    }
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