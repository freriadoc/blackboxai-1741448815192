namespace StockChartApp.Models;

public class FinancialInstrument
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<StockPriceData> HistoricalData { get; set; } = new();

    public bool IsValidDateRange()
    {
        return StartDate <= EndDate && 
               StartDate != DateTime.MinValue && 
               EndDate != DateTime.MinValue;
    }

    public override string ToString()
    {
        return $"{Symbol} - {Name} ({StartDate:d} to {EndDate:d})";
    }
}
