using Terminal.WinUI3.Models.Trade;

namespace Terminal.WinUI3.Helpers;

public class PositionsEventArgs : EventArgs
{
    public IEnumerable<HistoryPosition> Positions
    {
        get;
    }

    public PositionsEventArgs(IEnumerable<HistoryPosition> positions)
    {
        Positions = positions;
    }
}
