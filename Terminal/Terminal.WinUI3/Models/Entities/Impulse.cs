/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                       Impulse.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;

namespace Terminal.WinUI3.Models.Entities;

public class Impulse
{
    public int ID { get; set; }
    public double Open { get; set; }
    public double Close { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Direction Direction { get; init; }
    public bool IsLeader { get; set; }
    public Direction OppositeDirection =>
        Direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => Direction.NaN
        };

    public Impulse(int id, double open, double close, DateTime start, DateTime end, Direction direction)
    {
        ID = id;
        Open = open;
        Close = close;
        Start = start;
        End = end;
        Direction = direction;
    }

    public override string ToString()
    {
        return $"direction:{Direction}, length:{Math.Abs(Open - Close)}, isLeader:{IsLeader}";
    } 
}
