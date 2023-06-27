/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator.Contracts.Services;
using Mediator.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Mediator.Services;

public class DataService : ObservableRecipient, IDataService //todo: ObservableRecipient???
{
    private readonly Guid _guid = Guid.NewGuid();
    private const Provider Provider = Common.Entities.Provider.Mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IDataService> _logger;
    private readonly Dictionary<string, List<Quotation>> _ticksCache = new();
    private readonly Queue<string> _keys = new();
    private const int MaxItems = 744; //(24*31)
    private readonly DateTime _startDateTimeUtc;
    private const int QuartersInYear = 4;
   
    private readonly string? _server;
    private string _currentHoursKey = null!;

    public DataService(IConfiguration configuration, ILogger<IDataService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        Workplace = EnvironmentHelper.SetWorkplaceFromEnvironment();
        _server = DatabaseExtensionsAndHelpers.GetServerName();
        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>("StartDate")!, "yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _logger.LogTrace("({Guid}) is ON.", _guid);
    }

    private Workplace Workplace
    {
        get;
    }

    public async Task<int> SaveQuotationsAsync(IEnumerable<Quotation> quotations)
    {
        if (Workplace is not (Workplace.Production or Workplace.Development))
        {
            return 0;
        }

        int result = default;
        var groupedWeekly = quotations.GroupBy(q => new { q.DateTime.Year, Week = q.DateTime.Week() }).ToList();
        foreach (var weekGroup in groupedWeekly)
        {
            var yearNumber = weekGroup.Key.Year;
            var weekNumber = weekGroup.Key.Week;
            var quotationsWeekly = weekGroup.OrderBy(q => q.DateTime).ToList();

            var tableName = DatabaseExtensionsAndHelpers.GetTableName(weekNumber);
            DateTimeExtensionsAndHelpers.Check_ISO_8601(yearNumber, weekNumber, quotationsWeekly);
            var dataTable = CreateDataTable(quotationsWeekly);

            var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
            await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogExceptionHelper.LogException(_logger, exception);
                throw;
            }

            result += await InsertDataAsync(connection, tableName, dataTable, weekNumber).ConfigureAwait(false);
        }

        return result;
    }
    private static DataTable CreateDataTable(List<Quotation> quotationsWeekly)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Symbol", typeof(int));
        dataTable.Columns.Add("DateTime", typeof(DateTime));
        dataTable.Columns.Add("Ask", typeof(double));
        dataTable.Columns.Add("Bid", typeof(double));
        foreach (var quotation in quotationsWeekly)
        {
            dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);
        }

        return dataTable;
    }
    private async Task<int> InsertDataAsync(SqlConnection connection, string tableName, DataTable dataTable, int weekNumber)
    {
        int result;

        SqlTransaction transaction = null!;
        try
        {
            await using (transaction = connection.BeginTransaction($"week:{weekNumber:00}"))
            {
                await using var command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
                command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
                command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
                var rowCountParam = new SqlParameter("@RowCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
                command.Parameters.Add(rowCountParam);
                command.CommandTimeout = 0;
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                result = (int)rowCountParam.Value;
                transaction.Commit();
                _logger.LogInformation("{dataTable.Rows.Count} ticks saved. Week: {weekNumber}.", dataTable.Rows.Count, weekNumber);
            }
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception);

            if (transaction == null)
            {
                throw;
            }

            try
            {
                transaction.Rollback();
            }
            catch (Exception exRollback)
            {
                LogExceptionHelper.LogException(_logger, exRollback, "Error during transaction rollback.");
                throw;
            }

            throw;
        }

        return result;
    }

    public async Task<IEnumerable<Quotation>> GetSinceDateTimeHourTillNowAsync(DateTime startDateTime)
    {
        var result = new List<Quotation>();
        var start = startDateTime.Date.AddHours(startDateTime.Hour);
        var end = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
        _currentHoursKey = $"{end.Year}.{end.Month:D2}.{end.Day:D2}.{end.Hour:D2}";
        end = end.AddHours(1);

        var index = start;
        do
        {
            var key = $"{index.Year}.{index.Month:D2}.{index.Day:D2}.{index.Hour:D2}";
            if (!_ticksCache.ContainsKey(key))
            {
                try
                {
                    await LoadTicksToCacheAsync(index).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    LogExceptionHelper.LogException(_logger, exception);
                    throw;
                }
                
                if (!_ticksCache.ContainsKey(key))
                {
                    SetQuotations(key, new List<Quotation>());
                }
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
    private async Task LoadTicksToCacheAsync(DateTime dateTime)
    {
        var yearNumber = dateTime.Year;
        var weekNumber = dateTime.Week();
        var dayOfWeekNumber = ((int)dateTime.DayOfWeek + 6) % 7 + 1;
        var hourNumber = dateTime.Hour;
        var quotations = new List<Quotation>();
        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
        
        try
        {
            await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeekAndDayAndHour", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", weekNumber);
            command.Parameters.AddWithValue("@DayOfWeek", dayOfWeekNumber);
            command.Parameters.AddWithValue("@HourOfDay", hourNumber);
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

            //var yearsCounter = 0;
            //var monthsCounter = 0;
            //var daysCounter = 0;
            //var hoursCounter = 0;

            var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.DateTime.Year });
            foreach (var yearGroup in groupedByYear)
            {
                var year = yearGroup.Key.Year;
                //yearsCounter++;
                var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.DateTime.Month });
                foreach (var monthGroup in groupedByMonth)
                {
                    var month = monthGroup.Key.Month;
                    //monthsCounter++;
                    var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.DateTime.Day });
                    foreach (var dayGroup in groupedByDay)
                    {
                        var day = dayGroup.Key.Day;
                        //daysCounter++;
                        var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.DateTime.Hour });
                        foreach (var hourGroup in groupedByHour)
                        {
                            var hour = hourGroup.Key.Hour;
                            //hoursCounter++;
                            var key = $"{year}.{month:D2}.{day:D2}.{hour:D2}";
                            SetQuotations(key, hourGroup.ToList());
                            _logger.LogTrace("{key}",key);
                        }
                    }
                }
            }

            //_logger.LogTrace("year counter:{yearsCounter}, month counter:{monthsCounter:D2}, day counter:{daysCounter:D2}, hour counter:{hoursCounter:D2}", yearsCounter, monthsCounter, daysCounter, hoursCounter);
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception);
            throw;
        }
    }
    private void AddQuotation(string key, List<Quotation> quotationList)
    {
        if (_ticksCache.Count >= MaxItems)
        {
            var oldestKey = _keys.Dequeue();
            _ticksCache.Remove(oldestKey);
        }

        if (string.Equals(_currentHoursKey, key))
        {
            return;
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

    public async Task<Dictionary<ActionResult, int>> BackupAsync()
    {
        var resultCounts = new Dictionary<ActionResult, int>
        {
            [ActionResult.ActionNotPossible] = 0,
            [ActionResult.Failure] = 0,
            [ActionResult.NoActionRequired] = 0,
            [ActionResult.Success] = 0
        };
        var startYear = _startDateTimeUtc.Year;
        var endYear = DateTime.UtcNow.Year;

        if (Workplace is not (Workplace.Production or Workplace.Development))
        {
            for (var yearToBackup = startYear; yearToBackup <= endYear; yearToBackup++)
            {
                for (var quarter = 1; quarter <= QuartersInYear; quarter++)
                {
                    resultCounts[ActionResult.ActionNotPossible]++;
                }
            }
            
            return resultCounts;
        }

        try
        {
            for (var yearToBackup = startYear; yearToBackup <= endYear; yearToBackup++)
            {
                for (var quarter = 1; quarter <= QuartersInYear; quarter++)
                {
                    var result = await BackupProviderDatabase(yearToBackup, quarter).ConfigureAwait(false);
                    switch (result)
                    {
                        case -1:
                            resultCounts[ActionResult.Failure]++;
                            break;
                        case 0:
                            resultCounts[ActionResult.NoActionRequired]++;
                            break;
                        case 1:
                            resultCounts[ActionResult.Success]++;
                            break;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError("{Message}", exception.Message);
            _logger.LogError("{Message}", exception.InnerException?.Message);
            throw;
        }

        return resultCounts;

        async Task<int> BackupProviderDatabase(int yearNumber, int quarterNumber)
        {
            string dbBackupDrive = _configuration.GetValue<string>($"{nameof(dbBackupDrive)}")!;
            string dbBackupFolder = _configuration.GetValue<string>($"{nameof(dbBackupFolder)}")!;

            var databaseName = $"{yearNumber}.{quarterNumber}.{Provider.ToString().ToLower()}";
            var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("BackupProviderDatabase", connection) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Drive", dbBackupDrive));
            cmd.Parameters.Add(new SqlParameter("@Folder", dbBackupFolder));

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var result = (int)returnParameter.Value;
            return result;
        }
    }
}