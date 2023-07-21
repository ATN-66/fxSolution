/*+------------------------------------------------------------------+
  |                                          Terminal.WinUI3.Services|
  |                                               DataBaseService.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using Common.DataSource;
using Common.Entities;
using Common.ExtensionsAndHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.WinUI3.Contracts.Services;
using Terminal.WinUI3.Models.Maintenance;
using DateTime = System.DateTime;

namespace Terminal.WinUI3.Services;

public class DataBaseService : DataBaseSource, IDataBaseService
{
    private readonly SolutionDataBaseSettings _solutionDataBaseSettings;

    public DataBaseService(IConfiguration configuration, IOptions<SolutionDataBaseSettings> solutionDataBaseSettings, ILogger<IDataSource> logger, IAudioPlayer audioPlayer) : base(configuration, logger, audioPlayer)//providerBackupSettings,IOptions<ProviderBackupSettings> providerBackupSettings,
    {
        _solutionDataBaseSettings = solutionDataBaseSettings.Value;
    }

    public Task<IList<HourlyContribution>> GetTicksContributionsAsync()
    {
        var start = _startDateTimeUtc.Date;
        var end = DateTime.Now.Date.AddDays(1);
        return GetTicksContributionsAsync(start, end);
    }
    public Task<IList<HourlyContribution>> GetTicksContributionsAsync(DateTime date)
    {
        var start = date.Date;
        var end = date.Date.AddDays(1);
        return GetTicksContributionsAsync(start, end);
    }
    private async Task<IList<HourlyContribution>> GetTicksContributionsAsync(DateTime startDateTimeInclusive, DateTime endDateTimeExclusive)
    {
        var list = new List<HourlyContribution>();
        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={_solutionDataBaseSettings.DataBaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("GetTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTime) { Value = startDateTimeInclusive.ToString(_dataBaseSourceDateTimeFormat) });
            cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTime) { Value = endDateTimeExclusive.ToString(_dataBaseSourceDateTimeFormat) });
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
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }

        return list;
    }
    public async Task<IList<Quotation>> GetSampleTicksAsync(IEnumerable<HourlyContribution> hourlyContributions, int yearNumber, int weekNumber)
    {
        var result = new List<Quotation>();
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var hourlyContribution in hourlyContributions)
        {
            dataTable.Rows.Add(hourlyContribution.DateTime.ToString(_dataBaseSourceDateTimeFormat));
        }

        try
        {
            var databaseName = GetDatabaseName(yearNumber, weekNumber, _thisProvider);
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("GetSampleTicks", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber.ToString() });
            command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
            command.CommandTimeout = 0;
            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(resultSymbol, resultDateTime, resultAsk, resultBid);
                result.Add(quotation);
            }
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }

        return result;
    }
    public async Task<int> UpdateContributionsAsync(IEnumerable<long> hourNumbers, bool status)
    {
        var hourNumbersTable = new DataTable();
        hourNumbersTable.Columns.Add("HourNumber", typeof(long));
        foreach (var hourNumber in hourNumbers)
        {
            hourNumbersTable.Rows.Add(hourNumber);
        }

        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={_solutionDataBaseSettings.DataBaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("UpdateTicksContributions", connection) { CommandType = CommandType.StoredProcedure };
            var parameter = new SqlParameter("@HourNumbers", SqlDbType.Structured)
            {
                TypeName = "dbo.HourNumbersTableType",
                Value = hourNumbersTable
            };
            cmd.Parameters.Add(parameter);
            cmd.Parameters.AddWithValue("@Status", status.ToString());

            var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            return (int)result!;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Position cancelled...");
            return 0;
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }
    }
    public Task<IList<DailyBySymbolContribution>> GetDayContributionAsync(DateTime selectedDate) => throw new NotImplementedException();
    public async Task<int> DeleteTicksAsync(IEnumerable<DateTime> dateTimes, int yearNumber, int weekNumber)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("DateTimeValue", typeof(DateTime));
        foreach (var dateTime in dateTimes)
        {
            dataTable.Rows.Add(dateTime);
        }

        var databaseName = DatabaseExtensionsAndHelpers.GetDatabaseName(yearNumber, weekNumber, _thisProvider);

        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = new SqlCommand("DeleteTicks", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add(new SqlParameter("@Week", SqlDbType.Int) { Value = weekNumber.ToString() });
            command.Parameters.Add(new SqlParameter("@DateTimes", SqlDbType.Structured) { Value = dataTable });
            command.CommandTimeout = 0;
            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            _logger.LogTrace("{count} ticks deleted.", (int)result!);
            return (int)result;
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }
    }
}