using Common.Entities;

namespace Common.DataSource;

public interface IDataBaseSource : IDataSource
{
    Task<Dictionary<ActionResult, int>> BackupAsync();
    Task<int> SaveDataAsync(IList<Quotation> quotations);
}