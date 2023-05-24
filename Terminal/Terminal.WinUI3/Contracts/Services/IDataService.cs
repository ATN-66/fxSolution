/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{    
    Task InitializeAsync();
    Task StartAsync();
}
