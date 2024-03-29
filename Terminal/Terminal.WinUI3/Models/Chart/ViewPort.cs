﻿/*+------------------------------------------------------------------+
  |                                      Terminal.WinUI3.Models.Chart|
  |                                                      ViewPort.cs |
  +------------------------------------------------------------------+*/

namespace Terminal.WinUI3.Models.Chart;

public class ViewPort
{
    public DateTime Start;
    public DateTime End;
    public double High;
    public double Low;

    public override string ToString()
    {
        return $"Start: {Start}, End: {End}, High: {High}, Low: {Low}";
    }
}
