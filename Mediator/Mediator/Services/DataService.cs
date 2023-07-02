/*+------------------------------------------------------------------+
  |                                                 Mediator.Services|
  |                                                   DataService.cs |
  +------------------------------------------------------------------+*/

using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using CommunityToolkit.Mvvm.ComponentModel;
using Mediator.Contracts.Services;
using Mediator.Helpers;
using Mediator.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quotation = Common.Entities.Quotation;
using Symbol = Common.Entities.Symbol;

namespace Mediator.Services;

public class DataService : ObservableRecipient, IDataService //todo: ObservableRecipient???
{
    private readonly Guid _guid = Guid.NewGuid();
    private const int QuartersInYear = 4;
    private const Provider Provider = Common.Entities.Provider.Mediator;

    private readonly ILogger<IDataService> _logger;
    private readonly ConcurrentDictionary<DateTime, List<Quotation>?> _hoursCache = new();
    private readonly ConcurrentQueue<DateTime> _hoursKeys = new();
    private DateTime _currentHoursKey;

    private readonly DateTime _startDateTimeUtc;
    private readonly BackupSettings _backupSettings;
    private readonly string? _server;
    private readonly int _maxHoursInCache;

    public DataService(IConfiguration configuration, IOptions<BackupSettings> backupSettings, ILogger<IDataService> logger)
    {
        _logger = logger;

        Workplace = EnvironmentHelper.SetWorkplaceFromEnvironment();
        _server = DatabaseExtensionsAndHelpers.GetServerName();
        _backupSettings = backupSettings.Value;
        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>("StartDate")!, "yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _maxHoursInCache = configuration.GetValue<int>($"{nameof(_maxHoursInCache)}");

        _logger.LogTrace("({Guid}) is ON.", _guid);
    }

    private Workplace Workplace
    {
        get;
    }

    #region Save
    public async Task<int> SaveDataAsync(IEnumerable<Quotation> quotations)
    {
        if (Workplace is not Workplace.Production)
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
                LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
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
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");

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
                LogExceptionHelper.LogException(_logger, exRollback, MethodBase.GetCurrentMethod()!.Name, "Error during transaction rollback.");
                throw;
            }

            throw;
        }

        return result;
    }
    #endregion Save

    #region Retrieve
    public async Task<IEnumerable<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive)
    {
        var difference = Math.Ceiling((endDateTimeInclusive - startDateTimeInclusive).TotalHours);
        if (difference > _maxHoursInCache)
        {
            throw new InvalidOperationException($"Requested hours exceed maximum cache size of {_maxHoursInCache} hours.");
        }
        if (difference < 0)
        {
            throw new InvalidOperationException("Start date cannot be later than end date.");
        }

        var tasks = new List<Task>();
        var quotations = new List<Quotation>();
        var start = startDateTimeInclusive.Date.AddHours(startDateTimeInclusive.Hour);
        var end = endDateTimeInclusive.Date.AddHours(endDateTimeInclusive.Hour).AddHours(1);
        _currentHoursKey = DateTime.Now.Date.AddHours(DateTime.Now.Hour);
        var key = start;
        do
        {
            if (!_hoursCache.ContainsKey(key) || _currentHoursKey.Equals(key))
            {
                tasks.Add(LoadHourToCacheAsync(key));
            }

            key = key.Add(new TimeSpan(1, 0, 0));
            
        }
        while (key < end);

        await Task.WhenAll(tasks).ConfigureAwait(false);

        key = start;
        do
        {
            if (_hoursCache.ContainsKey(key))
            {
                quotations.AddRange(GetData(key));
            }
            else
            {
               throw new InvalidOperationException("The key is absent.");
            }

            key = key.Add(new TimeSpan(1, 0, 0));
        }
        while (key < end);

        return quotations;
    }
    private async Task LoadHourToCacheAsync(DateTime key)
    {
        var yearNumber = key.Year;
        var weekNumber = key.Week();
        var dayOfWeekNumber = ((int)key.DayOfWeek + 6) % 7 + 1;
        var hourNumber = key.Hour;
        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, Provider);
        var quotations = new List<Quotation>();
        var done = false;

        try
        {
            await using var connection = new SqlConnection($"{_server};Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeekAndDayAndHour", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", weekNumber.ToString());
            command.Parameters.AddWithValue("@DayOfWeek", dayOfWeekNumber.ToString());
            command.Parameters.AddWithValue("@HourOfDay", hourNumber.ToString());
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

            var yearsCounter = 0;
            var monthsCounter = 0;
            var daysCounter = 0;
            var hoursCounter = 0;

            var groupedByYear = quotations.GroupBy(q => new QuotationKey { Year = q.DateTime.Year });
            foreach (var yearGroup in groupedByYear)
            {
                var year = yearGroup.Key.Year;
                yearsCounter++;
                var groupedByMonth = yearGroup.GroupBy(q => new QuotationKey { Month = q.DateTime.Month });
                foreach (var monthGroup in groupedByMonth)
                {
                    var month = monthGroup.Key.Month;
                    monthsCounter++;
                    var groupedByDay = monthGroup.GroupBy(q => new QuotationKey { Day = q.DateTime.Day });
                    foreach (var dayGroup in groupedByDay)
                    {
                        var day = dayGroup.Key.Day;
                        daysCounter++;
                        var groupedByHour = dayGroup.GroupBy(q => new QuotationKey { Hour = q.DateTime.Hour });
                        foreach (var hourGroup in groupedByHour)
                        {
                            var hour = hourGroup.Key.Hour;
                            hoursCounter++;
                            var checkKey = new DateTime(year, month, day, hour, 0, 0);
                            var quotationsToSave = hourGroup.ToList();
                            if (!key.Equals(checkKey) || yearsCounter != 1 || monthsCounter != 1 || daysCounter != 1 || hoursCounter != 1 || quotations.Count != quotationsToSave.Count)
                            {
                                throw new InvalidOperationException("Keys are not identical or data corrupted.");
                            }
                            SetData(key, quotationsToSave);
                            done = true;
                            _logger.LogTrace("year:{Year}, month:{Month:D2}, day:{Day:D2}, hour:{Hour:D2}. Count:{QuotationsToSaveCount}", year, month, day, hour, quotationsToSave.Count);
                        }
                    }
                }
            }

            if (!done)
            {
                SetData(key, quotations: new List<Quotation>());
            }
        }
        catch (SqlException exception)
        {
            //Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
        catch (Exception exception)
        {
            LogExceptionHelper.LogException(_logger, exception, MethodBase.GetCurrentMethod()!.Name, "");
            throw;
        }
    }
    private void AddData(DateTime key, List<Quotation>? quotations)
    {
        if (_hoursCache.Count > _maxHoursInCache)
        {
            if (_hoursKeys.TryDequeue(out var oldestKey))
            {
                _hoursCache.TryRemove(oldestKey, out _);
            }
        }

        if (!_hoursCache.ContainsKey(key))
        {
            _hoursCache[key] = quotations;
            _hoursKeys.Enqueue(key);
        }
        else
        {
            if (!_currentHoursKey.Equals(key))
            {
                throw new InvalidOperationException("The key already exists.");
            }
            _hoursCache[key] = quotations;
        }
    }
    private void SetData(DateTime key, List<Quotation>? quotations)
    {
        AddData(key, quotations);
    }
    private IEnumerable<Quotation> GetData(DateTime key)
    {
        _hoursCache.TryGetValue(key, out var quotations);
        return quotations!;
    }
    #endregion Retrieve

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
            var databaseName = $"{yearNumber}.{quarterNumber}.{Provider.ToString().ToLower()}";
            var connectionString = $"{_server};Database={databaseName};Trusted_Connection=True;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("BackupProviderDatabase", connection) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Drive", _backupSettings.Drive));
            cmd.Parameters.Add(new SqlParameter("@Folder", _backupSettings.Folder));

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var result = (int)returnParameter.Value;
            return result;
        }
    }
}