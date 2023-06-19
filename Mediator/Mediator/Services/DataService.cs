/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator.Contracts.Services;
using Mediator.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quotation = Common.Entities.Quotation;

namespace Mediator.Services;

public class DataService : ObservableRecipient, IDataService
{
    private readonly Guid _guid = Guid.NewGuid();
    private const Entity Entity = Common.Entities.Entity.Mediator;
    private readonly IConfiguration _configuration;
    private readonly MainViewModel _mainViewModel;
    private readonly ILogger<IDataService> _logger;
    private const string Development = "Development";
    private const string Testing = "Testing";
    private const string Production = "Production";
    private const string Default = "Server=localhost\\SQLEXPRESS";
    private readonly Dictionary<string, List<Quotation>> _ticksCache = new();
    private readonly Queue<string> _keys = new();
    private const int MaxItems = 2_016;

    public DataService(IConfiguration configuration, MainViewModel mainViewModel, ILogger<IDataService> logger)
    {
        _configuration = configuration;
        _mainViewModel = mainViewModel;
        _logger = logger;
        _logger.Log(LogLevel.Trace, "{TypeName}.{Guid} is ON.", GetType().Name, _guid);
    }

    public async Task SaveQuotationsAsync(List<Quotation> quotations)
    {
        if (_mainViewModel.Workplace is not (Workplace.Production or Workplace.Development))
        {
            return;
        }

        var quotationsGrouped = quotations.GroupBy(q => new { q.DateTime.Year, Week = q.DateTime.Week() }).ToList();
        foreach (var grouping in quotationsGrouped)
        {
            var yearNumber = grouping.Key.Year;
            var weekNumber = grouping.Key.Week;
            var quotationsWeekly = grouping.OrderBy(q => q.DateTime).ToList();
            
            var tableName = GetTableName(weekNumber);
            DateTimeExtensionsAndHelpers.Check_ISO_8601(yearNumber, weekNumber, quotations);

            var dataTable = new DataTable();
            dataTable.Columns.Add("Symbol", typeof(int));
            dataTable.Columns.Add("DateTime", typeof(DateTime));
            dataTable.Columns.Add("Ask", typeof(double));
            dataTable.Columns.Add("Bid", typeof(double));
            foreach (var quotation in quotationsWeekly)
            {
                dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);
            }

            var server = GetConnectionString(_mainViewModel.Workplace);
            var databaseName = GetDatabaseName(yearNumber, weekNumber, Entity);
            var connectionString = $"{server};Database={databaseName};Trusted_Connection=True;";

            try
            {
                SqlConnection connection;
                await using (connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    SqlTransaction transaction;
                    await using (transaction = connection.BeginTransaction($"week:{weekNumber:00}"))
                    {
                        try
                        {
                            SqlCommand command;
                            await using (command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure })
                            {
                                command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
                                command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
                                command.CommandTimeout = 0;
                                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                                transaction.Commit();
                                _logger.Log(LogLevel.Information, "{quotationsWeeklyCount} ticks saved. week:{weekNumber}.", quotationsWeekly.Count, weekNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (transaction == null)
                            {
                                _logger.LogError(ex, "Error during command execution.");
                                throw;
                            }

                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception exRollback)
                            {
                                _logger.LogError(exRollback, "Error during transaction rollback.");
                                throw;
                            }
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command execution.");
                throw;
            }
        }
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
                await LoadTicksToCacheAsync(index).ConfigureAwait(false);
            }
            result.AddRange(GetQuotations(key));
            index = index.Add(new TimeSpan(1, 0, 0));
        }
        while (index < end);
        return result;
    }

    private async Task LoadTicksToCacheAsync(DateTime dateTime)
    {
        var quotations = new List<Quotation>();

        var year = dateTime.Year.ToString();
        var month = dateTime.Month.ToString("D2");
        var week = dateTime.Week().ToString("D2");
        var quarter = DateTimeExtensionsAndHelpers.Quarter(dateTime.Week()).ToString();
        var day = dateTime.Day.ToString("D2");
        var hour = dateTime.Hour.ToString("D2");
        var key = $"{year}.{month}.{day}.{hour}";

        var server = GetConnectionString(_mainViewModel.Workplace);
        var databaseName = GetDatabaseName(dateTime.Year, dateTime.Week(), Entity);
        var connectionString = $"{server};Database={databaseName};Trusted_Connection=True;";
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

        SetQuotations(key, quotations);
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

    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";
    private static string GetDatabaseName(int yearNumber, int weekNumber, Entity entity) => $"{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}.{entity.ToString().ToLower()}";
    private string GetConnectionString(Workplace workplace)
    {
        return workplace switch
        {
            Workplace.Development => _configuration.GetConnectionString(Development) ?? Default,
            Workplace.Testing => _configuration.GetConnectionString(Testing) ?? Default,
            Workplace.Production => _configuration.GetConnectionString(Production) ?? Default,
            _ => throw new ArgumentOutOfRangeException(nameof(workplace), workplace, null)
        };
    }
}