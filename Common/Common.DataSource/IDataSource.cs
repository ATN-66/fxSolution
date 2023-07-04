using Common.Entities;

namespace Common.DataSource;

public interface IDataSource
{
    Task<IList<Quotation>> GetHistoricalDataAsync(DateTime startDateTimeInclusive, DateTime endDateTimeInclusive, CancellationToken token);
}