/*+------------------------------------------------------------------+
  |                                  Terminal.WinUI3.Models.Entities |
  |                                                  ThresholdBar.cs |
  +------------------------------------------------------------------+*/

using Common.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Terminal.WinUI3.Models.Entities;

public class ThresholdBar : IChartItem
{
    private readonly double _threshold;

    [JsonIgnore] public readonly double[] OpenArray = new double[2];
    [JsonIgnore] public readonly double[] CloseArray = new double[2];

    public ThresholdBar(int id, Symbol symbol, double open, double close, DateTime start, DateTime end, double threshold)
    {
        ID = id;
        Symbol = symbol;
        _threshold = threshold;
        OpenArray[0] = CloseArray[0] = OpenArray[1] = open;
        CloseArray[1] = close;
        Start = start;
        End = end;
        Direction = DetermineDirection(open, close);

        if (Direction == Direction.Up)
        {
            Stage = Stage.ExtensionContinue | Stage.Up;
            Threshold = Close - threshold;
        }
        else
        {
            Stage = Stage.ExtensionContinue | Stage.Down;
            Threshold = Close + threshold;
        }
    }

    public ThresholdBar(Stage stage, int id, double threshold, Symbol symbol, double open, double close, DateTime start, DateTime end)
    {
        Stage = stage;
        ID = id;
        _threshold = threshold;
        Symbol = symbol;
        Open = open;
        Close = close;
        Start = start;
        End = end;
        Direction = DetermineDirection(open, close);

        if (Direction == Direction.Up)
        {
            Threshold = Close - threshold;
        }
        else
        {
            Threshold = Close + threshold;
        }
    }

    public int ID { get; }

    [JsonIgnore] public Symbol Symbol { get; }

    [JsonConverter(typeof(StringEnumConverter))] public Stage Stage { get; set; }

    public double Open
    {
        get => OpenArray[0];
        private init => OpenArray[0] = value;
    }

    public double Close
    {
        get => (Stage & Stage.ExtensionStart) == Stage.ExtensionStart || (Stage & Stage.ExtensionContinue) == Stage.ExtensionContinue ? CloseArray[1] : CloseArray[0];
        set
        {
            if ((Stage & Stage.ExtensionStart) == Stage.ExtensionStart || (Stage & Stage.ExtensionContinue) == Stage.ExtensionContinue)
            {
                CloseArray[1] = value;
            }
            else
            {
                CloseArray[0] = OpenArray[1] = CloseArray[1] = value;
            }

            if (Direction == Direction.Up)
            {
                Threshold = Close - _threshold;
            }
            else
            {
                Threshold = Close + _threshold;
            }
        }
    }

    [JsonIgnore] public double Threshold { get; private set; }

    [JsonIgnore] public Direction Direction { get; init; }

    public DateTime Start { get; }

    public DateTime End { get; set; }

    [JsonIgnore] public double Ask { get; set; }

    [JsonIgnore] public double Bid { get; set; }

    [JsonIgnore] public double Length => Math.Abs(Open - Close);

    private static Direction DetermineDirection(double open, double close)
    {
        if (close > open)
        {
            return Direction.Up;
        }

        if (close < open)
        {
            return Direction.Down;
        }

        throw new Exception("DetermineDirection: Open and close cannot be equal.");
    }

    public override string ToString() =>
        //return $"{Start:yyyy-MM-dd}, from: {Start:HH:mm:ss} to: {End:HH:mm:ss}; OC: {Open}, {Close}";
        $"{ID}, direction:{Direction}, stage: {Stage}";
}