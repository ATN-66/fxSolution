using System.Globalization;
using Common.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Data;
using System.Data.SqlClient;

namespace Common.DataSource;

public abstract class DataBaseSource : DataSource, IDataBaseSource
{
    protected readonly Provider _thisProvider;
    protected readonly string _dataBaseSourceDateTimeFormat;
    protected readonly DateTime _startDateTimeUtc;
    private readonly ProviderBackupSettings _providerBackupSettings;
    private const int QuartersInYear = 4;

    protected DataBaseSource(IConfiguration configuration, IOptions<ProviderBackupSettings> providerBackupSettings, ILogger<IDataSource> logger, IAudioPlayer audioPlayer) : base(configuration, logger, audioPlayer) 
    {
        _thisProvider = Enum.Parse<Provider>(configuration.GetValue<string>($"{nameof(_thisProvider)}")!);
        _dataBaseSourceDateTimeFormat = configuration.GetValue<string>($"{nameof(_dataBaseSourceDateTimeFormat)}")!;

        _startDateTimeUtc = DateTime.ParseExact(configuration.GetValue<string>($"{nameof(_startDateTimeUtc)}")!, _dataBaseSourceDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
        _providerBackupSettings = providerBackupSettings.Value;
    }

    protected Workplace Workplace { get; init; }
    protected static Workplace SetWorkplaceFromEnvironment(string executingAssemblyName)
    {
        var environment = Environment.GetEnvironmentVariable(executingAssemblyName);

        if (Enum.TryParse<Workplace>(environment, out var workplace))
        {
            return workplace;
        }

        throw new InvalidOperationException($"{nameof(environment)} setting for {executingAssemblyName} is null or invalid.");
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
            LogException(exception, "");
            throw;
        }

        return resultCounts;

        async Task<int> BackupProviderDatabase(int yearNumber, int quarterNumber)
        {
            var databaseName = $"{yearNumber}.{quarterNumber}.{_thisProvider.ToString().ToLower()}";
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand("BackupProviderDatabase", connection) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add(new SqlParameter("@Drive", _providerBackupSettings.Drive));
            cmd.Parameters.Add(new SqlParameter("@Folder", _providerBackupSettings.Folder));

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            var result = (int)returnParameter.Value;
            return result;
        }
    }
    protected override async Task<IList<Quotation>> GetDataAsync(DateTime startDateTimeInclusive, CancellationToken token)
    {
        var quotations = new List<Quotation>();
        var yearNumber = startDateTimeInclusive.Year;
        var weekNumber = Week(startDateTimeInclusive);
        var dayOfWeekNumber = ((int)startDateTimeInclusive.DayOfWeek + 6) % 7 + 1;
        var hourNumber = startDateTimeInclusive.Hour;
        var databaseName = GetDatabaseName(yearNumber, weekNumber, _thisProvider);

        try
        {
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            await connection.OpenAsync(token).ConfigureAwait(false);
            await using var command = new SqlCommand("GetQuotationsByWeekAndDayAndHour", connection) { CommandTimeout = 0 };
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@Week", weekNumber.ToString());
            command.Parameters.AddWithValue("@DayOfWeek", dayOfWeekNumber.ToString());
            command.Parameters.AddWithValue("@HourOfDay", hourNumber.ToString());
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            int id = default;
            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                var resultSymbol = (Symbol)reader.GetInt32(0);
                var resultDateTime = reader.GetDateTime(1).ToUniversalTime();
                var resultAsk = reader.GetDouble(2);
                var resultBid = reader.GetDouble(3);
                var quotation = new Quotation(id++, resultSymbol, resultDateTime, resultAsk, resultBid);
                quotations.Add(quotation);
            }
        }
        catch (SqlException exception)
        {
            LogException(exception, "");
            throw;
        }
        catch (Exception exception)
        {
            LogException(exception, "");
            throw;
        }

        return quotations;
    }
    public async Task<int> SaveDataAsync(IList<Quotation> quotations)
    {
        int result = default;
        var groupedWeekly = quotations.GroupBy(q => new { q.DateTime.Year, Week = Week(q.DateTime) }).ToList();

        foreach (var weekGroup in groupedWeekly)
        {
            var yearNumber = weekGroup.Key.Year;
            var weekNumber = weekGroup.Key.Week;
            var quotationsWeekly = weekGroup.OrderBy(q => q.DateTime).ToList();

            var tableName = GetTableName(weekNumber);
            Check_ISO_8601(yearNumber, weekNumber, quotationsWeekly);
            var dataTable = CreateDataTable(quotationsWeekly);

            var databaseName = GetDatabaseName(yearNumber, weekNumber, _thisProvider);
            await using var connection = new SqlConnection($@"Server={Environment.MachineName}\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;");
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogException(exception, "");
                throw;
            }

            result += await InsertDataAsync(connection, tableName, dataTable, weekNumber).ConfigureAwait(false);
        }

        return result;
    }

    private DataTable CreateDataTable(List<Quotation> quotationsWeekly)
    {
        var dataTable = new DataTable();

        dataTable.Columns.Add("Symbol", typeof(int));
        dataTable.Columns.Add("DateTime", typeof(DateTime));
        dataTable.Columns.Add("Ask", typeof(double));
        dataTable.Columns.Add("Bid", typeof(double));

        foreach (var quotation in quotationsWeekly)
        {
            dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime.ToString(_dataBaseSourceDateTimeFormat), quotation.Ask.ToString("F8"), quotation.Bid.ToString("F8"));
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
                _logger.LogInformation("{dataTableRowsCount} ticks saved. Week: {weekNumber}.", dataTable.Rows.Count.ToString(), weekNumber.ToString());
            }
        }
        catch (Exception exception)
        {
            LogException(exception, "");

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
                LogException(exRollback, "Error during transaction rollback.");
                throw;
            }

            throw;
        }

        return result;
    }
    private static void Check_ISO_8601(int yearNumber, int weekNumber, IList<Quotation> list)
    {
        var start = list[0].DateTime.Date;
        var end = list[^1].DateTime.Date;

        switch (yearNumber, weekNumber)
        {
            case (2022, 1): if (start < DateTime.Parse("Monday, January 3, 2022") || end > DateTime.Parse("Sunday, January 9, 2022")) throw new InvalidOperationException(); break;
            case (2022, 2): if (start < DateTime.Parse("Monday, January 10, 2022") || end > DateTime.Parse("Sunday, January 16, 2022")) throw new InvalidOperationException(); break;
            case (2022, 3): if (start < DateTime.Parse("Monday, January 17, 2022") || end > DateTime.Parse("Sunday, January 23, 2022")) throw new InvalidOperationException(); break;
            case (2022, 4): if (start < DateTime.Parse("Monday, January 24, 2022") || end > DateTime.Parse("Sunday, January 30, 2022")) throw new InvalidOperationException(); break;
            case (2022, 5): if (start < DateTime.Parse("Monday, January 31, 2022") || end > DateTime.Parse("Sunday, February 6, 2022")) throw new InvalidOperationException(); break;
            case (2022, 6): if (start < DateTime.Parse("Monday, February 7, 2022") || end > DateTime.Parse("Sunday, February 13, 2022")) throw new InvalidOperationException(); break;
            case (2022, 7): if (start < DateTime.Parse("Monday, February 14, 2022") || end > DateTime.Parse("Sunday, February 20, 2022")) throw new InvalidOperationException(); break;
            case (2022, 8): if (start < DateTime.Parse("Monday, February 21, 2022") || end > DateTime.Parse("Sunday, February 27, 2022")) throw new InvalidOperationException(); break;
            case (2022, 9): if (start < DateTime.Parse("Monday, February 28, 2022") || end > DateTime.Parse("Sunday, March 6, 2022")) throw new InvalidOperationException(); break;
            case (2022, 10): if (start < DateTime.Parse("Monday, March 7, 2022") || end > DateTime.Parse("Sunday, March 13, 2022")) throw new InvalidOperationException(); break;
            case (2022, 11): if (start < DateTime.Parse("Monday, March 14, 2022") || end > DateTime.Parse("Sunday, March 20, 2022")) throw new InvalidOperationException(); break;
            case (2022, 12): if (start < DateTime.Parse("Monday, March 21, 2022") || end > DateTime.Parse("Sunday, March 27, 2022")) throw new InvalidOperationException(); break;
            case (2022, 13): if (start < DateTime.Parse("Monday, March 28, 2022") || end > DateTime.Parse("Sunday, April 3, 2022")) throw new InvalidOperationException(); break;
            case (2022, 14): if (start < DateTime.Parse("Monday, April 4, 2022") || end > DateTime.Parse("Sunday, April 10, 2022")) throw new InvalidOperationException(); break;
            case (2022, 15): if (start < DateTime.Parse("Monday, April 11, 2022") || end > DateTime.Parse("Sunday, April 17, 2022")) throw new InvalidOperationException(); break;
            case (2022, 16): if (start < DateTime.Parse("Monday, April 18, 2022") || end > DateTime.Parse("Sunday, April 24, 2022")) throw new InvalidOperationException(); break;
            case (2022, 17): if (start < DateTime.Parse("Monday, April 25, 2022") || end > DateTime.Parse("Sunday, May 1, 2022")) throw new InvalidOperationException(); break;
            case (2022, 18): if (start < DateTime.Parse("Monday, May 2, 2022") || end > DateTime.Parse("Sunday, May 8, 2022")) throw new InvalidOperationException(); break;
            case (2022, 19): if (start < DateTime.Parse("Monday, May 9, 2022") || end > DateTime.Parse("Sunday, May 15, 2022")) throw new InvalidOperationException(); break;
            case (2022, 20): if (start < DateTime.Parse("Monday, May 16, 2022") || end > DateTime.Parse("Sunday, May 22, 2022")) throw new InvalidOperationException(); break;
            case (2022, 21): if (start < DateTime.Parse("Monday, May 23, 2022") || end > DateTime.Parse("Sunday, May 29, 2022")) throw new InvalidOperationException(); break;
            case (2022, 22): if (start < DateTime.Parse("Monday, May 30, 2022") || end > DateTime.Parse("Sunday, June 5, 2022")) throw new InvalidOperationException(); break;
            case (2022, 23): if (start < DateTime.Parse("Monday, June 6, 2022") || end > DateTime.Parse("Sunday, June 12, 2022")) throw new InvalidOperationException(); break;
            case (2022, 24): if (start < DateTime.Parse("Monday, June 13, 2022") || end > DateTime.Parse("Sunday, June 19, 2022")) throw new InvalidOperationException(); break;
            case (2022, 25): if (start < DateTime.Parse("Monday, June 20, 2022") || end > DateTime.Parse("Sunday, June 26, 2022")) throw new InvalidOperationException(); break;
            case (2022, 26): if (start < DateTime.Parse("Monday, June 27, 2022") || end > DateTime.Parse("Sunday, July 3, 2022")) throw new InvalidOperationException(); break;
            case (2022, 27): if (start < DateTime.Parse("Monday, July 4, 2022") || end > DateTime.Parse("Sunday, July 10, 2022")) throw new InvalidOperationException(); break;
            case (2022, 28): if (start < DateTime.Parse("Monday, July 11, 2022") || end > DateTime.Parse("Sunday, July 17, 2022")) throw new InvalidOperationException(); break;
            case (2022, 29): if (start < DateTime.Parse("Monday, July 18, 2022") || end > DateTime.Parse("Sunday, July 24, 2022")) throw new InvalidOperationException(); break;
            case (2022, 30): if (start < DateTime.Parse("Monday, July 25, 2022") || end > DateTime.Parse("Sunday, July 31, 2022")) throw new InvalidOperationException(); break;
            case (2022, 31): if (start < DateTime.Parse("Monday, August 1, 2022") || end > DateTime.Parse("Sunday, August 7, 2022")) throw new InvalidOperationException(); break;
            case (2022, 32): if (start < DateTime.Parse("Monday, August 8, 2022") || end > DateTime.Parse("Sunday, August 14, 2022")) throw new InvalidOperationException(); break;
            case (2022, 33): if (start < DateTime.Parse("Monday, August 15, 2022") || end > DateTime.Parse("Sunday, August 21, 2022")) throw new InvalidOperationException(); break;
            case (2022, 34): if (start < DateTime.Parse("Monday, August 22, 2022") || end > DateTime.Parse("Sunday, August 28, 2022")) throw new InvalidOperationException(); break;
            case (2022, 35): if (start < DateTime.Parse("Monday, August 29, 2022") || end > DateTime.Parse("Sunday, September 4, 2022")) throw new InvalidOperationException(); break;
            case (2022, 36): if (start < DateTime.Parse("Monday, September 5, 2022") || end > DateTime.Parse("Sunday, September 11, 2022")) throw new InvalidOperationException(); break;
            case (2022, 37): if (start < DateTime.Parse("Monday, September 12, 2022") || end > DateTime.Parse("Sunday, September 18, 2022")) throw new InvalidOperationException(); break;
            case (2022, 38): if (start < DateTime.Parse("Monday, September 19, 2022") || end > DateTime.Parse("Sunday, September 25, 2022")) throw new InvalidOperationException(); break;
            case (2022, 39): if (start < DateTime.Parse("Monday, September 26, 2022") || end > DateTime.Parse("Sunday, October 2, 2022")) throw new InvalidOperationException(); break;
            case (2022, 40): if (start < DateTime.Parse("Monday, October 3, 2022") || end > DateTime.Parse("Sunday, October 9, 2022")) throw new InvalidOperationException(); break;
            case (2022, 41): if (start < DateTime.Parse("Monday, October 10, 2022") || end > DateTime.Parse("Sunday, October 16, 2022")) throw new InvalidOperationException(); break;
            case (2022, 42): if (start < DateTime.Parse("Monday, October 17, 2022") || end > DateTime.Parse("Sunday, October 23, 2022")) throw new InvalidOperationException(); break;
            case (2022, 43): if (start < DateTime.Parse("Monday, October 24, 2022") || end > DateTime.Parse("Sunday, October 30, 2022")) throw new InvalidOperationException(); break;
            case (2022, 44): if (start < DateTime.Parse("Monday, October 31, 2022") || end > DateTime.Parse("Sunday, November 6, 2022")) throw new InvalidOperationException(); break;
            case (2022, 45): if (start < DateTime.Parse("Monday, November 7, 2022") || end > DateTime.Parse("Sunday, November 13, 2022")) throw new InvalidOperationException(); break;
            case (2022, 46): if (start < DateTime.Parse("Monday, November 14, 2022") || end > DateTime.Parse("Sunday, November 20, 2022")) throw new InvalidOperationException(); break;
            case (2022, 47): if (start < DateTime.Parse("Monday, November 21, 2022") || end > DateTime.Parse("Sunday, November 27, 2022")) throw new InvalidOperationException(); break;
            case (2022, 48): if (start < DateTime.Parse("Monday, November 28, 2022") || end > DateTime.Parse("Sunday, December 4, 2022")) throw new InvalidOperationException(); break;
            case (2022, 49): if (start < DateTime.Parse("Monday, December 5, 2022") || end > DateTime.Parse("Sunday, December 11, 2022")) throw new InvalidOperationException(); break;
            case (2022, 50): if (start < DateTime.Parse("Monday, December 12, 2022") || end > DateTime.Parse("Sunday, December 18, 2022")) throw new InvalidOperationException(); break;
            case (2022, 51): if (start < DateTime.Parse("Monday, December 19, 2022") || end > DateTime.Parse("Sunday, December 25, 2022")) throw new InvalidOperationException(); break;
            case (2022, 52): if (start < DateTime.Parse("Monday, December 26, 2022") || end > DateTime.Parse("Friday, December 30, 2022")) throw new InvalidOperationException(); break;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            case (2023, 1): if (start < DateTime.Parse("Monday, January 2, 2023") || end > DateTime.Parse("Sunday, January 8, 2023")) throw new InvalidOperationException(); break;
            case (2023, 2): if (start < DateTime.Parse("Monday, January 9, 2023") || end > DateTime.Parse("Sunday, January 15, 2023")) throw new InvalidOperationException(); break;
            case (2023, 3): if (start < DateTime.Parse("Monday, January 16, 2023") || end > DateTime.Parse("Sunday, January 22, 2023")) throw new InvalidOperationException(); break;
            case (2023, 4): if (start < DateTime.Parse("Monday, January 23, 2023") || end > DateTime.Parse("Sunday, January 29, 2023")) throw new InvalidOperationException(); break;
            case (2023, 5): if (start < DateTime.Parse("Monday, January 30, 2023") || end > DateTime.Parse("Sunday, February 5, 2023")) throw new InvalidOperationException(); break;
            case (2023, 6): if (start < DateTime.Parse("Monday, February 6, 2023") || end > DateTime.Parse("Sunday, February 12, 2023")) throw new InvalidOperationException(); break;
            case (2023, 7): if (start < DateTime.Parse("Monday, February 13, 2023") || end > DateTime.Parse("Sunday, February 19, 2023")) throw new InvalidOperationException(); break;
            case (2023, 8): if (start < DateTime.Parse("Monday, February 20, 2023") || end > DateTime.Parse("Sunday, February 26, 2023")) throw new InvalidOperationException(); break;
            case (2023, 9): if (start < DateTime.Parse("Monday, February 27, 2023") || end > DateTime.Parse("Sunday, March 5, 2023")) throw new InvalidOperationException(); break;
            case (2023, 10): if (start < DateTime.Parse("Monday, March 6, 2023") || end > DateTime.Parse("Sunday, March 12, 2023")) throw new InvalidOperationException(); break;
            case (2023, 11): if (start < DateTime.Parse("Monday, March 13, 2023") || end > DateTime.Parse("Sunday, March 19, 2023")) throw new InvalidOperationException(); break;
            case (2023, 12): if (start < DateTime.Parse("Monday, March 20, 2023") || end > DateTime.Parse("Sunday, March 26, 2023")) throw new InvalidOperationException(); break;
            case (2023, 13): if (start < DateTime.Parse("Monday, March 27, 2023") || end > DateTime.Parse("Sunday, April 2, 2023")) throw new InvalidOperationException(); break;
            case (2023, 14): if (start < DateTime.Parse("Monday, April 3, 2023") || end > DateTime.Parse("Sunday, April 9, 2023")) throw new InvalidOperationException(); break;
            case (2023, 15): if (start < DateTime.Parse("Monday, April 10, 2023") || end > DateTime.Parse("Sunday, April 16, 2023")) throw new InvalidOperationException(); break;
            case (2023, 16): if (start < DateTime.Parse("Monday, April 17, 2023") || end > DateTime.Parse("Sunday, April 23, 2023")) throw new InvalidOperationException(); break;
            case (2023, 17): if (start < DateTime.Parse("Monday, April 24, 2023") || end > DateTime.Parse("Sunday, April 30, 2023")) throw new InvalidOperationException(); break;
            case (2023, 18): if (start < DateTime.Parse("Monday, May 1, 2023") || end > DateTime.Parse("Sunday, May 7, 2023")) throw new InvalidOperationException(); break;
            case (2023, 19): if (start < DateTime.Parse("Monday, May 8, 2023") || end > DateTime.Parse("Sunday, May 14, 2023")) throw new InvalidOperationException(); break;
            case (2023, 20): if (start < DateTime.Parse("Monday, May 15, 2023") || end > DateTime.Parse("Sunday, May 21, 2023")) throw new InvalidOperationException(); break;
            case (2023, 21): if (start < DateTime.Parse("Monday, May 22, 2023") || end > DateTime.Parse("Sunday, May 28, 2023")) throw new InvalidOperationException(); break;
            case (2023, 22): if (start < DateTime.Parse("Monday, May 29, 2023") || end > DateTime.Parse("Sunday, June 4, 2023")) throw new InvalidOperationException(); break;
            case (2023, 23): if (start < DateTime.Parse("Monday, June 5, 2023") || end > DateTime.Parse("Sunday, June 11, 2023")) throw new InvalidOperationException(); break;
            case (2023, 24): if (start < DateTime.Parse("Monday, June 12, 2023") || end > DateTime.Parse("Sunday, June 18, 2023")) throw new InvalidOperationException(); break;
            case (2023, 25): if (start < DateTime.Parse("Monday, June 19, 2023") || end > DateTime.Parse("Sunday, June 25, 2023")) throw new InvalidOperationException(); break;
            case (2023, 26): if (start < DateTime.Parse("Monday, June 26, 2023") || end > DateTime.Parse("Sunday, July 2, 2023")) throw new InvalidOperationException(); break;
            case (2023, 27): if (start < DateTime.Parse("Monday, July 3, 2023") || end > DateTime.Parse("Sunday, July 9, 2023")) throw new InvalidOperationException(); break;
            case (2023, 28): if (start < DateTime.Parse("Monday, July 10, 2023") || end > DateTime.Parse("Sunday, July 16, 2023")) throw new InvalidOperationException(); break;
            case (2023, 29): if (start < DateTime.Parse("Monday, July 17, 2023") || end > DateTime.Parse("Sunday, July 23, 2023")) throw new InvalidOperationException(); break;
            case (2023, 30): if (start < DateTime.Parse("Monday, July 24, 2023") || end > DateTime.Parse("Sunday, July 30, 2023")) throw new InvalidOperationException(); break;
            case (2023, 31): if (start < DateTime.Parse("Monday, July 31, 2023") || end > DateTime.Parse("Sunday, August 6, 2023")) throw new InvalidOperationException(); break;
            case (2023, 32): if (start < DateTime.Parse("Monday, August 7, 2023") || end > DateTime.Parse("Sunday, August 13, 2023")) throw new InvalidOperationException(); break;
            case (2023, 33): if (start < DateTime.Parse("Monday, August 14, 2023") || end > DateTime.Parse("Sunday, August 20, 2023")) throw new InvalidOperationException(); break;
            case (2023, 34): if (start < DateTime.Parse("Monday, August 21, 2023") || end > DateTime.Parse("Sunday, August 27, 2023")) throw new InvalidOperationException(); break;
            case (2023, 35): if (start < DateTime.Parse("Monday, August 28, 2023") || end > DateTime.Parse("Sunday, September 3, 2023")) throw new InvalidOperationException(); break;
            case (2023, 36): if (start < DateTime.Parse("Monday, September 4, 2023") || end > DateTime.Parse("Sunday, September 10, 2023")) throw new InvalidOperationException(); break;
            case (2023, 37): if (start < DateTime.Parse("Monday, September 11, 2023") || end > DateTime.Parse("Sunday, September 17, 2023")) throw new InvalidOperationException(); break;
            case (2023, 38): if (start < DateTime.Parse("Monday, September 18, 2023") || end > DateTime.Parse("Sunday, September 24, 2023")) throw new InvalidOperationException(); break;
            case (2023, 39): if (start < DateTime.Parse("Monday, September 25, 2023") || end > DateTime.Parse("Sunday, October 1, 2023")) throw new InvalidOperationException(); break;
            case (2023, 40): if (start < DateTime.Parse("Monday, October 2, 2023") || end > DateTime.Parse("Sunday, October 8, 2023")) throw new InvalidOperationException(); break;
            case (2023, 41): if (start < DateTime.Parse("Monday, October 9, 2023") || end > DateTime.Parse("Sunday, October 15, 2023")) throw new InvalidOperationException(); break;
            case (2023, 42): if (start < DateTime.Parse("Monday, October 16, 2023") || end > DateTime.Parse("Sunday, October 22, 2023")) throw new InvalidOperationException(); break;
            case (2023, 43): if (start < DateTime.Parse("Monday, October 23, 2023") || end > DateTime.Parse("Sunday, October 29, 2023")) throw new InvalidOperationException(); break;
            case (2023, 44): if (start < DateTime.Parse("Monday, October 30, 2023") || end > DateTime.Parse("Sunday, November 5, 2023")) throw new InvalidOperationException(); break;
            case (2023, 45): if (start < DateTime.Parse("Monday, November 6, 2023") || end > DateTime.Parse("Sunday, November 12, 2023")) throw new InvalidOperationException(); break;
            case (2023, 46): if (start < DateTime.Parse("Monday, November 13, 2023") || end > DateTime.Parse("Sunday, November 19, 2023")) throw new InvalidOperationException(); break;
            case (2023, 47): if (start < DateTime.Parse("Monday, November 20, 2023") || end > DateTime.Parse("Sunday, November 26, 2023")) throw new InvalidOperationException(); break;
            case (2023, 48): if (start < DateTime.Parse("Monday, November 27, 2023") || end > DateTime.Parse("Sunday, December 3, 2023")) throw new InvalidOperationException(); break;
            case (2023, 49): if (start < DateTime.Parse("Monday, December 4, 2023") || end > DateTime.Parse("Sunday, December 10, 2023")) throw new InvalidOperationException(); break;
            case (2023, 50): if (start < DateTime.Parse("Monday, December 11, 2023") || end > DateTime.Parse("Sunday, December 17, 2023")) throw new InvalidOperationException(); break;
            case (2023, 51): if (start < DateTime.Parse("Monday, December 18, 2023") || end > DateTime.Parse("Sunday, December 24, 2023")) throw new InvalidOperationException(); break;
            case (2023, 52): if (start < DateTime.Parse("Monday, December 25, 2023") || end > DateTime.Parse("Sunday, December 31, 2023")) throw new InvalidOperationException(); break;
            default: throw new Exception();
        }
    }
    private static string GetTableName(int weekNumber) => $"week{weekNumber:00}";
    protected static string GetDatabaseName(int yearNumber, int weekNumber, Provider provider) => $"{yearNumber}.{Quarter(weekNumber)}.{provider.ToString().ToLower()}";
}