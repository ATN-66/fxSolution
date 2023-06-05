/*+------------------------------------------------------------------+
  |                                 Terminal.WinUI3.Models.Dashboard |
  |                                              DashboardMessage.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Dashboard;

public class DashboardMessage
{
    public DashboardItem DashboardItem { get; set; }
    public DashboardMessage(DashboardItem dashboardItem)
    {
        DashboardItem = dashboardItem;
    }
}
