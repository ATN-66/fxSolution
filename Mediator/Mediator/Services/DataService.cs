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

    public DataService(IConfiguration configuration, MainViewModel mainViewModel, ILogger<IDataService> logger)
    {
        _configuration = configuration;
        _mainViewModel = mainViewModel;
        _logger = logger;

        _logger.Log(LogLevel.Trace, $"{GetType().Name}.({_guid}) is ON.");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during command execution or transaction commit.");
                if (transaction != null)
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception exRollback)
                    {
                        _logger.LogError(exRollback, "Error during transaction rollback.");
                    }
                }
                throw;
            }
        }

        _logger.Log(LogLevel.Trace, $"{GetType().Name}.({_guid}): {quotations.Count} ticks saved.");
    }

    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";
    private static string GetDatabaseName(int yearNumber, int weekNumber, Entity entity) => $"{yearNumber}.{DateTimeExtensionsAndHelpers.Quarter(weekNumber)}.{entity.ToString().ToLower()}";
    private string GetConnectionString(Workplace workplace)
    {
        switch (workplace)
        {
            case Workplace.Development:
                return _configuration.GetConnectionString(Development) ?? Default;
            case Workplace.Testing:
                return _configuration.GetConnectionString(Testing) ?? Default;
            case Workplace.Production:
                return _configuration.GetConnectionString(Production) ?? Default;
            default:
                throw new ArgumentOutOfRangeException(nameof(workplace), workplace, null);
        }
    }
}