using Common.Entities;

namespace Terminal.WinUI3.AI.Interfaces;

public interface IKernelManager
{
    Task InitializeAsync(IDictionary<Symbol, List<Quotation>> quotations);
    void Add(Quotation quotation);
}