/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                          IQuotationsMessenger.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Environment = Common.Entities.Environment;

namespace Common.MetaQuotes.Mediator;

public interface IQuotationsMessenger
{
    void DeInit(Symbol symbol, DeInitReason reason);
    string Init(Quotation quotation, Environment environment);
    string Tick(Quotation quotation);
}