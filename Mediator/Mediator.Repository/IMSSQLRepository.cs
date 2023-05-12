using Common.Entities;

namespace Mediator.Repository
{
    public interface IMSSQLRepository
    {
        Task SaveQuotationsAsync(IList<Quotation> quotationsToSave);
    }
}
