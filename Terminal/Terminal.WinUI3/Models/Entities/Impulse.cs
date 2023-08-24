/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                       Impulse.cs |
  +------------------------------------------------------------------+*/

using System.Numerics;
using Common.Entities;
using Newtonsoft.Json;

namespace Terminal.WinUI3.Models.Entities;

public class Impulse
{
    public int ID { get; set; }
    public double Open { get; set; }
    public double Close { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    [JsonIgnore] public Direction Direction { get; }
    [JsonIgnore] public bool IsLeader { get; set; }
    [JsonIgnore] public Direction OppositeDirection => Direction == Direction.Up ? Direction.Down : Direction.Up;

    [JsonIgnore] public Vector2 StartPoint;
    [JsonIgnore] public Vector2 EndPoint;

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
        return $"direction:{Direction}, isLeader:{IsLeader}";
    } 
}