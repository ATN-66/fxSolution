/*+------------------------------------------------------------------+
  |                                               MetaQuotes.Console |
  |                                               MSSQLRepository.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using Common.Entities;
using Environment = Common.Entities.Environment;

namespace MetaQuotes.Console;

public class MSSQLRepository
{
    private static readonly object ConsoleLock = new();
    private static readonly object SyncRoot = new();
    private static volatile MSSQLRepository? _instance;
    public static MSSQLRepository Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            lock (SyncRoot) _instance = new MSSQLRepository();
            return _instance;
        }
    }

    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day, Environment env, Tick dest)
    {
        switch (day)
        {
            case 0: return await GetQuotationsForWeekAsync(year, week, env, dest).ConfigureAwait(false);
            case < 1 or > 7: throw new ArgumentOutOfRangeException(nameof(day), "day must be between 1 and 7.");
        }

        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = GetDatabaseName(year, week, env, dest);
        var connectionString = $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;";

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeekAndDay", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", week);
            command.Parameters.AddWithValue("@DayOfWeek", day);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var quotation = new Quotation((Symbol)reader.GetInt32(0), reader.GetDateTime(1), reader.GetInt32(2), reader.GetInt32(3));
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

        lock (ConsoleLock)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Week:{week:00}, Day:{day:00} -> {quotations.Count:##,##0} quotations.");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        return (firstQuotations, quotations);
    }
    
    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week, Environment env, Tick dest)
    {
        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = GetDatabaseName(year, week, env, dest);
        var connectionString = $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;";

        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeek", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", week);
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var quotation = new Quotation((Symbol)reader.GetInt32(0), reader.GetDateTime(1), reader.GetInt32(2), reader.GetInt32(3));
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

        lock (ConsoleLock)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Week:{week:00} -> {quotations.Count:##,##0} quotations.");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        return (firstQuotations, quotations);
    }
    
    public async Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year, Environment env, Tick dest)
    {
        var totalQuotations = 0;
        var quotationsByWeek = new Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>();

        var tasks = Enumerable.Range(1, 52).Select(async week =>
        {
            var (firstQuotations, quotations) = await GetQuotationsForWeekAsync(year, week, env, dest).ConfigureAwait(false);
            return (week, firstQuotations, quotations);
        });

        foreach (var (weekNumber, firstQuotations, quotations) in await Task.WhenAll(tasks).ConfigureAwait(false))
        {
            totalQuotations += firstQuotations.Count;
            totalQuotations += quotations.Count;
            quotationsByWeek[weekNumber] = (firstQuotations, quotations);
        }

        lock (ConsoleLock)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Year:{year:00} -> {totalQuotations:##,###} quotations.");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        return quotationsByWeek;
    }
   
    public static string GetDatabaseName(int yearNumber, int weekNumber, Environment environment, Tick tick)
    {
        return $"{environment.ToString().ToLower()}.{tick.ToString().ToLower()}.{yearNumber}.{GetQuarterNumber(weekNumber)}";
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
}