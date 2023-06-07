/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections;
using Common.Entities;
using System.Data;
using System.Data.SqlClient;
using ABI.Windows.Networking.Sockets;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models;
using Terminal.WinUI3.Models.Maintenance;
using Environment = Common.Entities.Environment;
using Symbol = Common.Entities.Symbol;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;

// ReSharper disable StringLiteralTypo

namespace Terminal.WinUI3.Services;

public class DataService : IDataService
{
    private readonly IProcessor _processor;
    private Queue<Quotation> _firstQuotations = null!;
    private Queue<Quotation> _quotations = null!;

    public DataService(IProcessor processor)
    {
        _processor  = processor;
    }

    public async Task InitializeAsync()
    {
        const Environment environment = Environment.Testing;
        const Modification inputModification = Modification.UnModified;
        const int year = 2023;
        const int week = 8;
        const int day = 1;

        var (firstQuotations, quotations) = await GetQuotationsForDayAsync(year, week, day, environment, inputModification).ConfigureAwait(false);
        _firstQuotations = firstQuotations;
        _quotations = quotations;
    }

    public async Task StartAsync()
    {
        var initializeTasks = _firstQuotations.Select(quotation => _processor.InitializeAsync(quotation)).ToArray();
        await Task.WhenAll(initializeTasks).ConfigureAwait(false);
        _firstQuotations.Clear();

        while (_quotations.Any())
        {
            var quotation = _quotations.Dequeue();
            await _processor.TickAsync(quotation).ConfigureAwait(false);
        }
    }

    public async Task<List<HourlyContribution>> GetTicksContributionsAsync()
    {
        var result = new List<HourlyContribution>();

        var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);//todo: from settings
        var endDate = DateTime.Now;
        
        const string connectionString = $"Server=localhost\\SQLEXPRESS;Database=forex.solution;Trusted_Connection=True;";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var cmd = new SqlCommand("GetTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDate });
        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDate });
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var hour = reader.GetInt64(0);
            var date = reader.GetDateTime(1);
            var hasContribution = reader.GetBoolean(2);
            result.Add(new HourlyContribution(hour, date, hasContribution));
        }

        return result;
    }

    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day, Environment environment, Modification modification)
    {
        switch (day)
        {
            case 0: return await GetQuotationsForWeekAsync(year, week, environment, modification).ConfigureAwait(false);
            case < 1 or > 7: throw new ArgumentOutOfRangeException(nameof(day), Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap.GetValue("Resources/DataService_GetQuotationsForDayAsync_day_must_be_between_1_and_7_").ValueAsString);
        }

        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = GetDatabaseName(year, week, environment, modification);
        var connectionString = $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;";

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeekAndDay", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", week);
            command.Parameters.AddWithValue("@DayOfWeek", day);
            int id = default;
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);

                if (!firstQuotationsDict.ContainsKey(quotation.Symbol))
                {
                    firstQuotationsDict[quotation.Symbol] = quotation;
                    firstQuotations.Enqueue(quotation);
                }
                else
                {
                    quotations.Enqueue(quotation);
                }
            }
        }

        return (firstQuotations, quotations);
    }

    private async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week, Environment environment, Modification modification)
    {
        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = GetDatabaseName(year, week, environment, modification);
        var connectionString = $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;";

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeek", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", week);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            int id = default;
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);

                if (!firstQuotationsDict.ContainsKey(quotation.Symbol))
                {
                    firstQuotationsDict[quotation.Symbol] = quotation;
                    firstQuotations.Enqueue(quotation);
                }
                else
                {
                    quotations.Enqueue(quotation);
                }
            }
        }

        return (firstQuotations, quotations);
    }

    public async Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year, Environment environment, Modification modification)
    {
        var quotationsByWeek = new Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>();

        var tasks = Enumerable.Range(1, 52).Select(async week =>
        {
            var (firstQuotations, quotations) = await GetQuotationsForWeekAsync(year, week, environment, modification).ConfigureAwait(false);
            return (week, firstQuotations, quotations);
        });

        foreach (var (weekNumber, firstQuotations, quotations) in await Task.WhenAll(tasks).ConfigureAwait(false))
        {
            quotationsByWeek[weekNumber] = (firstQuotations, quotations);
        }

        return quotationsByWeek;
    }

    private static string GetDatabaseName(int yearNumber, int weekNumber, Environment environment, Modification modification)
    {
        return $"{environment.ToString().ToLower()}.{modification.ToString().ToLower()}.{yearNumber}.{GetQuarterNumber(weekNumber)}";
    }

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
            _ => throw new Exception(errorMessage),
        };
    }

    public List<SampleDataObject> GetSampleDataObjects()
    {
        var dummyTexts = new[] {
            @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer id facilisis lectus. Cras nec convallis ante, quis pulvinar tellus. Integer dictum accumsan pulvinar. Pellentesque eget enim sodales sapien vestibulum consequat.",
                @"Nullam eget mattis metus. Donec pharetra, tellus in mattis tincidunt, magna ipsum gravida nibh, vitae lobortis ante odio vel quam.",
                @"Quisque accumsan pretium ligula in faucibus. Mauris sollicitudin augue vitae lorem cursus condimentum quis ac mauris. Pellentesque quis turpis non nunc pretium sagittis. Nulla facilisi. Maecenas eu lectus ante. Proin eleifend vel lectus non tincidunt. Fusce condimentum luctus nisi, in elementum ante tincidunt nec.",
                @"Aenean in nisl at elit venenatis blandit ut vitae lectus. Praesent in sollicitudin nunc. Pellentesque justo augue, pretium at sem lacinia, scelerisque semper erat. Ut cursus tortor at metus lacinia dapibus.",
                @"Ut consequat magna luctus justo egestas vehicula. Integer pharetra risus libero, et posuere justo mattis et.",
                @"Proin malesuada, libero vitae aliquam venenatis, diam est faucibus felis, vitae efficitur erat nunc non mauris. Suspendisse at sodales erat.",
                @"Aenean vulputate, turpis non tincidunt ornare, metus est sagittis erat, id lobortis orci odio eget quam. Suspendisse ex purus, lobortis quis suscipit a, volutpat vitae turpis.",
                @"Duis facilisis, quam ut laoreet commodo, elit ex aliquet massa, non varius tellus lectus et nunc. Donec vitae risus ut ante pretium semper. Phasellus consectetur volutpat orci, eu dapibus turpis. Fusce varius sapien eu mattis pharetra.",
            };

        var rand = new Random();
        const int numberOfLocations = 8;
        var objects = new List<SampleDataObject>();
        for (var i = 0; i < numberOfLocations; i++)
        {
            objects.Add(new SampleDataObject()
            {
                Title = $"Item {i + 1}",
                ImageLocation = $"/Assets/SampleMedia/LandscapeImage{i + 1}.jpg",
                Views = rand.Next(100, 999).ToString(),
                Likes = rand.Next(10, 99).ToString(),
                Description = dummyTexts[i % dummyTexts.Length],
            });
        }

        return objects;
    }
}