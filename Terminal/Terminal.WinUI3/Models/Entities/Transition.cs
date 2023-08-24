/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                    Transition.cs |
  +------------------------------------------------------------------+*/

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Terminal.WinUI3.Models.Entities;

public class Transition
{
    public int ID { get; }
    public DateTime End { get; }
    public double Close { get; }
    [JsonConverter(typeof(StringEnumConverter))] public Stage Stage { get; }

    public Transition(int id, ThresholdBar tBar)
    {
        ID = id;
        Close = tBar.Close;
        End = tBar.End;
        Stage = tBar.Stage;
    }

    public override string ToString()
    {
        return $"ID: {ID}, End: {End}, Close: {Close}, stage: {Stage}";
    }
}