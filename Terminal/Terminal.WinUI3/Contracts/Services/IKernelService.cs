using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IKernelService
{
    void Initialize(IDictionary<Symbol, List<Quotation>> quotations, CancellationToken token);
    void Add(Quotation quotation);
}