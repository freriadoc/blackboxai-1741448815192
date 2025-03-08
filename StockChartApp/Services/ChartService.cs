using ScottPlot;
using StockChartApp.Models;

namespace StockChartApp.Services;

public class ChartService
{
    public void SaveStockChart(string outputPath, List<StockPriceData> data, string symbol, bool isIntraday = false)
    {
        var plt = new Plot(600, 400);

        // Prepare data arrays
        double[] xs = data.Select(x => x.Timestamp.ToOADate()).ToArray();
        double[] ys = data.Select(x => (double)x.Close).ToArray();

        // Plot the data
        plt.AddScatter(xs, ys);

        // Customize the plot
        plt.Title(label: $"{symbol} Stock Price {(isIntraday ? "(Intraday)" : "(Daily)")}");
        plt.XAxis.Label(label: "Date");
        plt.YAxis.Label(label: "Price ($)");

        // Format dates on the X axis
        plt.XAxis.DateTimeFormat(true);

        // Save the plot
        plt.SaveFig(outputPath);
    }

    public void SaveMultipleStockCharts(string outputPath, Dictionary<string, List<StockPriceData>> stocksData)
    {
        var plt = new Plot(600, 400);

        foreach (var (symbol, data) in stocksData)
        {
            double[] xs = data.Select(x => x.Timestamp.ToOADate()).ToArray();
            double[] ys = data.Select(x => (double)x.Close).ToArray();
            plt.AddScatter(xs, ys, label: symbol);
        }

        // Customize the plot
        plt.Title("Stock Price Comparison");
        plt.XAxis.Label("Date");
        plt.YAxis.Label("Price ($)");

        // Format dates on the X axis
        plt.XAxis.DateTimeFormat(true);

        // Show the legend
        plt.Legend();

        // Save the plot
        plt.SaveFig(outputPath);
    }
}
