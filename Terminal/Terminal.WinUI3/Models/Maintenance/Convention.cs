
/*+------------------------------------------------------------------+
  |                               Terminal.WinUI3.Models.Maintenance |
  |                                                    Convention.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Maintenance;

internal enum Convention
{
    SaturdayExcluded = 0,
    FridayWithoutLast2HoursIsAccepted = 1,
    MondayWithLast2HoursIsAccepted = 2
}
