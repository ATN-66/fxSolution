namespace Terminal.WinUI3.Models.Trade;

public class HistoryDeal
{
    public long Ticket { get; set; }
    public long Order { get; set; }
    // DateTime
    public long Time { get; set; }
    public long ExecutionTime { get; set; }
    // enum
    public int Type { get; set; }
    // enum
    public int Entry { get; set; }
    public long Magic { get; set; }
    // enum
    public int Reason { get; set; }
    public long Position { get; set; }
    public double Volume { get; set; }
    public double Price { get; set; }
    public double Commission { get; set; }
    public double Swap { get; set; }
    public double Profit { get; set; }
    public double Fee { get; set; }
    public double Sl { get; set; }
    public double Tp { get; set; }
    public string Symbol { get; set; }
    public string Comment { get; set; }
    public string External { get; set; }
}