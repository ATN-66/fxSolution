﻿/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                     IMediator.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Contracts.Services;

public interface IMediator
{
    Task<IEnumerable<Quotation>> GetTicksAsync(DateTime startDateTime, DateTime endDateTime);
}