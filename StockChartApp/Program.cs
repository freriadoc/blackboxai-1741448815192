using Microsoft.Extensions.DependencyInjection;
using StockChartApp.Models;
using StockChartApp.Services;

var services = new ServiceCollection();

// Configure services
services.AddHttpClient();
services.AddSingleton(new AlphaVantageService(
    new HttpClient(), 
    Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY") ?? 
    throw new Exception("Please set ALPHA_VANTAGE_API_KEY environment variable")
));
services.AddSingleton<InstrumentRepository>();
services.AddSingleton<ChartService>();

var serviceProvider = services.BuildServiceProvider();

// Get service instances
var alphaVantageService = serviceProvider.GetRequiredService<AlphaVantageService>();
var instrumentRepository = serviceProvider.GetRequiredService<InstrumentRepository>();
var chartService = serviceProvider.GetRequiredService<ChartService>();

async Task DisplayMenu()
{
    while (true)
    {
        Console.WriteLine("Stock Chart Application");
        Console.WriteLine("1. Search and Add Instrument");
        Console.WriteLine("2. View Saved Instruments");
        Console.WriteLine("3. Generate Charts for Saved Instruments");
        Console.WriteLine("4. Compare Multiple Stocks");
        Console.WriteLine("5. Exit");
        Console.WriteLine("\nSelect an option: ");

        var choice = Console.ReadLine();
        if (string.IsNullOrEmpty(choice)) break;

        switch (choice)
        {
            case "1":
                await SearchAndAddInstrument();
                break;
            case "2":
                await ViewSavedInstruments();
                break;
            case "3":
                await GenerateCharts();
                break;
            case "4":
                await CompareStocks();
                break;
            case "5":
                return;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }
}

async Task SearchAndAddInstrument()
{
    Console.WriteLine("Enter search keyword: ");
    var keyword = Console.ReadLine();
    if (string.IsNullOrEmpty(keyword)) return;
    
    Console.WriteLine("\nSearching for instruments...");
    var instruments = await alphaVantageService.GetInstrumentListAsync();
    
    var filteredInstruments = instruments
        .Where(i => i.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase) || 
                    i.Value.Contains(keyword, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (!filteredInstruments.Any())
    {
        Console.WriteLine("No instruments found.");
        return;
    }

    Console.WriteLine("\nFound instruments:");
    for (int i = 0; i < filteredInstruments.Count; i++)
    {
        Console.WriteLine($"{i + 1}. {filteredInstruments[i].Key} - {filteredInstruments[i].Value}");
    }

    Console.WriteLine("\nSelect instrument number to add (or 0 to cancel): ");
    if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 0 || selection > filteredInstruments.Count)
    {
        Console.WriteLine("Invalid selection.");
        return;
    }

    if (selection == 0) return;

    var selected = filteredInstruments[selection - 1];
    
    Console.WriteLine("Enter start date (yyyy-MM-dd): ");
    if (!DateTime.TryParse(Console.ReadLine(), out DateTime startDate))
    {
        Console.WriteLine("Invalid date format.");
        return;
    }

    Console.WriteLine("Enter end date (yyyy-MM-dd): ");
    if (!DateTime.TryParse(Console.ReadLine(), out DateTime endDate))
    {
        Console.WriteLine("Invalid date format.");
        return;
    }

    var instrument = new FinancialInstrument
    {
        Symbol = selected.Key,
        Name = selected.Value,
        StartDate = startDate,
        EndDate = endDate
    };

    var savedInstruments = await instrumentRepository.LoadInstrumentsAsync();
    savedInstruments.Add(instrument);
    await instrumentRepository.SaveInstrumentsAsync(savedInstruments);

    Console.WriteLine($"\nAdded {instrument.Symbol} to saved instruments.");
}

async Task ViewSavedInstruments()
{
    var instruments = await instrumentRepository.LoadInstrumentsAsync();
    
    if (!instruments.Any())
    {
        Console.WriteLine("No saved instruments found.");
        return;
    }

    Console.WriteLine("Saved Instruments:");
    foreach (var instrument in instruments)
    {
        Console.WriteLine($"- {instrument.Symbol} ({instrument.Name})");
        Console.WriteLine($"  Date Range: {instrument.StartDate:yyyy-MM-dd} to {instrument.EndDate:yyyy-MM-dd}");
    }
}

async Task GenerateCharts()
{
    var instruments = await instrumentRepository.LoadInstrumentsAsync();
    
    if (!instruments.Any())
    {
        Console.WriteLine("No saved instruments found.");
        return;
    }

    Console.WriteLine("Generating charts for saved instruments...");
    
    foreach (var instrument in instruments)
    {
        Console.WriteLine($"\nFetching data for {instrument.Symbol}...");
        
        try
        {
            var historicalData = await alphaVantageService.GetHistoricalDataAsync(
                instrument.Symbol, 
                instrument.StartDate, 
                instrument.EndDate
            );

            if (historicalData.Any())
            {
                var outputPath = $"{instrument.Symbol}_daily.png";
                chartService.SaveStockChart(outputPath, historicalData, instrument.Symbol);
                Console.WriteLine($"Chart saved as {outputPath}");

                // Also get intraday data
                var intradayData = await alphaVantageService.GetIntradayDataAsync(instrument.Symbol);
                if (intradayData.Any())
                {
                    outputPath = $"{instrument.Symbol}_intraday.png";
                    chartService.SaveStockChart(outputPath, intradayData, instrument.Symbol, true);
                    Console.WriteLine($"Intraday chart saved as {outputPath}");
                }
            }
            else
            {
                Console.WriteLine($"No data available for {instrument.Symbol}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating chart for {instrument.Symbol}: {ex.Message}");
        }
    }
}

async Task CompareStocks()
{
    var instruments = await instrumentRepository.LoadInstrumentsAsync();
    
    if (instruments.Count < 2)
    {
        Console.WriteLine("Need at least 2 saved instruments for comparison.");
        return;
    }

    Console.WriteLine("Fetching data for comparison...");
    
    var stocksData = new Dictionary<string, List<StockPriceData>>();
    
    foreach (var instrument in instruments)
    {
        Console.WriteLine($"Fetching data for {instrument.Symbol}...");
        try
        {
            var historicalData = await alphaVantageService.GetHistoricalDataAsync(
                instrument.Symbol, 
                instrument.StartDate, 
                instrument.EndDate
            );

            if (historicalData.Any())
            {
                stocksData[instrument.Symbol] = historicalData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data for {instrument.Symbol}: {ex.Message}");
        }
    }

    if (stocksData.Count >= 2)
    {
        var outputPath = "comparison_chart.png";
        chartService.SaveMultipleStockCharts(outputPath, stocksData);
        Console.WriteLine($"\nComparison chart saved as {outputPath}");
    }
    else
    {
        Console.WriteLine("\nNot enough data available for comparison.");
    }
}

// Start the application
try
{
    await DisplayMenu();
}
catch (Exception ex)
{
    Console.WriteLine($"Application error: {ex.Message}");
}
