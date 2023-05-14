/*+------------------------------------------------------------------+
  |                                              Mediator.Repository |
  |                                               MSSQLRepository.cs |
  +------------------------------------------------------------------+*/

using System.Data;
using System.Data.SqlClient;
using Common.Entities;
using Environment = Common.Entities.Environment;

namespace Mediator.Repository;

public class MSSQLRepository : IMSSQLRepository
{
    public async Task SaveQuotationsAsync(IList<Quotation> quotationsToSave)
    {
        //foreach (var quotation in quotationsToSave)
        //{
        //    Console.WriteLine($"{quotation.ID}");
        //}
        

        //var tableName = RepositoryHelper.GetTableName(weekNumber);
        //RepositoryHelper.CheckISO8601(yearNumber, weekNumber, quotationsToSave);

        //var dataTable = new DataTable();
        //dataTable.Columns.Add("Symbol", typeof(int));
        //dataTable.Columns.Add("DateTime", typeof(DateTime));
        //dataTable.Columns.Add("Ask", typeof(double));
        //dataTable.Columns.Add("Bid", typeof(double));
        //foreach (var quotation in quotationsToSave) dataTable.Rows.Add((int)quotation.Symbol, quotation.DateTime, quotation.Ask, quotation.Bid);

        //var databaseName = RepositoryHelper.GetDatabaseName(yearNumber, weekNumber, env, modification);
        //var connectionString = $"Server=localhost\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;";
        //await using var connection = new SqlConnection(connectionString);
        //await connection.OpenAsync().ConfigureAwait(false);
        //await using var transaction = connection.BeginTransaction($"week:{weekNumber:00}");
        //await using var command = new SqlCommand("InsertQuotations", connection, transaction) { CommandType = CommandType.StoredProcedure };
        //command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128) { Value = tableName });
        //command.Parameters.Add(new SqlParameter("@Quotations", SqlDbType.Structured) { TypeName = "dbo.QuotationTableType", Value = dataTable });
        //command.CommandTimeout = 0;

        //try
        //{
        //    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        //    transaction.Commit();
        //    Console.ForegroundColor = ConsoleColor.Green;
        //    Console.WriteLine($"Week:{weekNumber:00} {quotationsToSave.Count:##,###} quotations saved.");
        //    Console.ForegroundColor = ConsoleColor.White;
        //}
        //catch (Exception e)
        //{
        //    Console.WriteLine(e.Message);
        //    try
        //    {
        //        transaction.Rollback();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //        throw;
        //    }
        //    throw;
        //}
    }
}