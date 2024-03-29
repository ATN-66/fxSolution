﻿/*+------------------------------------------------------------------+
  |                                       Common.MetaQuotes.Mediator |
  |                                                IDataMessenger.cs |
  +------------------------------------------------------------------+*/

using System.Threading.Tasks;

namespace Common.MetaQuotes.Mediator;

public interface IDataMessenger
{
    void DeInit(int symbol, int reason);
    Task<string> InitAsync(int symbol, string datetime, double ask, double bid, int workplace);
    string Tick(int symbol, string datetime, double ask, double bid);
}