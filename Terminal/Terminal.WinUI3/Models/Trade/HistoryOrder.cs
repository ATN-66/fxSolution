namespace Terminal.WinUI3.Models.Trade;

public class HistoryOrder
{
    public long Ticket { get; set; }
    // Start
    public long TimeSetup { get; set; }
    // enum
    public int Type { get; set; }
    // enum
    public int State { get; set; }
    // Start
    public long TimeExpiration { get; set; }
    // Start
    public long TimeDone { get; set; }
    public long TimeSetupMsc { get; set; }
    public long TimeDoneMsc { get; set; }
    // enum
    public int Filling { get; set; }
    // enum
    public int TypeTime { get; set; }
    public long Magic { get; set; }
    // enum
    public int Reason { get; set; }
    public long Position { get; set; }
    public long PositionById { get; set; }
    public double VolumeInitial { get; set; }
    public double VolumeCurrent { get; set; }
    public double PriceOpen { get; set; }
    public double Sl { get; set; }
    public double Tp { get; set; }
    public double PriceCurrent { get; set; }
    public double PriceStopLimit { get; set; }
    public string Symbol { get; set; }
    public string Comment { get; set; }
    public string External { get; set; }
}