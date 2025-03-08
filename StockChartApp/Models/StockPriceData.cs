namespace StockChartApp.Models;

public class StockPriceData
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm}] O:{Open:F2} H:{High:F2} L:{Low:F2} C:{Close:F2} V:{Volume}";
    }
}
