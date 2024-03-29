﻿/*+------------------------------------------------------------------+
  |                                               MetaQuotes.Console |
  |                                               MSSQLRepository.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using Common.Entities;
using Common.ExtensionsAndHelpers;

namespace MetaQuotes.Console;

public class MSSQLRepository
{
    private const Provider Provider = Common.Entities.Provider.Terminal;
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

    public async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day)
    {
        switch (day)
        {
            case 0: return await GetQuotationsForWeekAsync(year, week).ConfigureAwait(false);
            case < 1 or > 7: throw new ArgumentOutOfRangeException(nameof(day), "day must be between 1 and 7.");
        }

        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(year, week, Provider);
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
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(resultSymbol, resultDateTime, resultAsk, resultBid);

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
            System.Console.WriteLine($"Week:{week:00}, Day:{day:00} -> {quotations.Count + Enum.GetValues(typeof(Symbol)).Length:##,##0} quotations.");
            System.Console.ForegroundColor = ConsoleColor.White;
        }

        return (firstQuotations, quotations);
    }
    private async Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week)
    {
        var firstQuotationsDict = new Dictionary<Symbol, Quotation>();
        var firstQuotations = new Queue<Quotation>();
        var quotations = new Queue<Quotation>();

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(year, week, Provider);
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
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(resultSymbol, resultDateTime, resultAsk, resultBid);

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
}