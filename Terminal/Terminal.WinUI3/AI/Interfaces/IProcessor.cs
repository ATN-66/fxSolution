/*+------------------------------------------------------------------+
  |                                    Terminal.WinUI3.AI.Interfaces |
  |                                                    IProcessor.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.AI.Interfaces;

public interface IProcessor
{
    Task InitializeAsync(Quotation quotation);
    Task TickAsync(Quotation quotation);
}