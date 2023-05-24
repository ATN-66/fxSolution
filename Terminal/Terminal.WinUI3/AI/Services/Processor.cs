/*+------------------------------------------------------------------+
  |                                      Terminal.WinUI3.AI.Services |
  |                                                     Processor.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Terminal.WinUI3.AI.Data;
using Terminal.WinUI3.AI.Interfaces;
using Terminal.WinUI3.Contracts.Services;

namespace Terminal.WinUI3.AI.Services;

public class Processor : IProcessor
{
    private readonly IDictionary<Symbol, Kernel> _kernels = new Dictionary<Symbol, Data.Kernel>();
    private readonly IVisualService _visualService;

    public Processor(IVisualService visualService)
    {
        foreach (var symbol in Enum.GetValues(typeof(Symbol)))
        {
            _kernels[(Symbol)symbol] = new Kernel((Symbol)symbol);
        }

        _visualService = visualService;
        _visualService.Initialize(_kernels);
    }

    public Task InitializeAsync(Quotation quotation)
    {
        _kernels[quotation.Symbol].Initialize(quotation);

        return Task.CompletedTask;
    }

    public Task TickAsync(Quotation quotation)
    {
        _kernels[quotation.Symbol].Tick(quotation);
        _visualService.Tick();
        return Task.CompletedTask;
    }
}