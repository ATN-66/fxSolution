/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                    Transition.cs |
  +------------------------------------------------------------------+*/

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Terminal.WinUI3.Models.Entities;

public class Transition
{
    public int ID
    {
        get;
    }
    public DateTime DateTime
    {
        get;
    }
    public double Close
    {
        get;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public Force Force
    {
        get;
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public TransitionType Type
    {
        get;
    }

    public Transition(int id, double close, DateTime dateTime,  Force force, TransitionType transitionType)
    {
        ID = id;
        DateTime = dateTime;
        Close = close;
        Force = force;
        Type = transitionType;
    }

    public Transition(int id, ThresholdBar tBar, TransitionType transitionType)
    {
        ID = id;
        Close = tBar.Close;
        DateTime = tBar.End;
        Force = tBar.Force;
        Type = transitionType;
    }

    public override string ToString()
    {
        return $"ID: {ID}, DateTime: {DateTime}, Close: {Close}, force: {Force}, Type: {Type}";
    }
}