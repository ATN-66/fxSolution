using Common.Entities;
using Environment = Common.Entities.Environment;

namespace Mediator.Repository.Interfaces;

public interface IQuotationsRepository
{
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForDayAsync(int year, int week, int day, Environment env, Tick dest);
    Task<(Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)> GetQuotationsForWeekAsync(int year, int week, Environment env, Tick dest);
    Task<Dictionary<int, (Queue<Quotation> FirstQuotations, Queue<Quotation> Quotations)>> GetQuotationsForYearWeeklyAsync(int year, Environment env, Tick dest);
    Task<int> SaveQuotations(IList<Quotation> quotations);
}