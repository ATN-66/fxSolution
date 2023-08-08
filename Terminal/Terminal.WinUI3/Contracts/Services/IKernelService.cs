using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IKernelService
{
    void Initialize(IDictionary<Symbol, List<Quotation>> quotations);
    void Add(Quotation quotation);
}