using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IKernelService
{
    Task InitializeAsync(IDictionary<Symbol, List<Quotation>> quotations);
    void Add(Quotation quotation);
}