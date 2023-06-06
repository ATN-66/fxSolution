/*+------------------------------------------------------------------+
  |                                Terminal.WinUI3.Contracts.Services|
  |                                                  IDataService.cs |
  +------------------------------------------------------------------+*/

using Terminal.WinUI3.Models;
using Terminal.WinUI3.Models.Maintenance;

namespace Terminal.WinUI3.Contracts.Services;

public interface IDataService
{    
    Task InitializeAsync();
    Task StartAsync();
    List<DailyContribution> GetTicksContributions();
    List<SampleDataObject> GetSampleDataObjects();
}
