using System.Net.Http.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockChartApp.Models;

namespace StockChartApp.Services;

public class AlphaVantageService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://www.alphavantage.co/query";
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinRequestInterval = 15000; // 15 seconds between requests

    public AlphaVantageService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    private async Task WaitForRateLimit()
    {
        var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
        if (timeSinceLastRequest.TotalMilliseconds < MinRequestInterval)
        {
            var delayTime = MinRequestInterval - (int)timeSinceLastRequest.TotalMilliseconds;
            Console.WriteLine($"Waiting {delayTime/1000} seconds for API rate limit...");
            await Task.Delay(delayTime);
        }
        _lastRequestTime = DateTime.Now;
    }

    private decimal ParseDecimal(JToken? token, string defaultValue = "0")
    {
        return decimal.Parse(token?.ToString() ?? defaultValue);
    }

    private long ParseLong(JToken? token, string defaultValue = "0")
    {
        return long.Parse(token?.ToString() ?? defaultValue);
    }

    public async Task<Dictionary<string, string>> GetInstrumentListAsync()
    {
        var result = new Dictionary<string, string>();
        try
        {
            await WaitForRateLimit();
            var response = await _httpClient.GetAsync($"{BaseUrl}?function=SYMBOL_SEARCH&keywords=AAPL&apikey={_apiKey}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(jsonString);
                
                if (data?["bestMatches"] is JArray matches)
                {
                    foreach (JObject? match in matches)
                    {
                        if (match != null)
                        {
                            var symbol = match["1. symbol"]?.ToString();
                            var name = match["2. name"]?.ToString();
                            if (!string.IsNullOrEmpty(symbol) && !string.IsNullOrEmpty(name))
                            {
                                result[symbol] = name;
                            }
                        }
                    }
                }
                else if (data?["Note"] != null)
                {
                    Console.WriteLine("API Rate limit reached. Please wait a moment and try again.");
                    await Task.Delay(MinRequestInterval);
                }
                else if (data?["Error Message"] != null)
                {
                    Console.WriteLine("API Error: " + data["Error Message"]);
                }
            }
            else
            {
                Console.WriteLine($"API request failed with status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching instrument list: {ex.Message}");
        }
        
        return result;
    }

    public async Task<List<StockPriceData>> GetHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        var result = new List<StockPriceData>();
        try
        {
            await WaitForRateLimit();
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_apiKey}&outputsize=full");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(jsonString);
                
                if (data?["Time Series (Daily)"] is JObject timeSeries)
                {
                    foreach (var (dateStr, values) in timeSeries)
                    {
                        if (DateTime.TryParse(dateStr, out var date) && 
                            date >= startDate && 
                            date <= endDate &&
                            values is JObject priceData)
                        {
                            result.Add(new StockPriceData
                            {
                                Timestamp = date,
                                Open = ParseDecimal(priceData["1. open"]),
                                High = ParseDecimal(priceData["2. high"]),
                                Low = ParseDecimal(priceData["3. low"]),
                                Close = ParseDecimal(priceData["4. close"]),
                                Volume = ParseLong(priceData["5. volume"])
                            });
                        }
                    }
                }
                else if (data?["Note"] != null)
                {
                    Console.WriteLine("API Rate limit reached. Please wait a moment and try again.");
                    await Task.Delay(MinRequestInterval);
                }
                else if (data?["Error Message"] != null)
                {
                    Console.WriteLine("API Error: " + data["Error Message"]);
                }
            }
            else
            {
                Console.WriteLine($"API request failed with status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching historical data: {ex.Message}");
        }

        return result.OrderBy(x => x.Timestamp).ToList();
    }

    public async Task<List<StockPriceData>> GetIntradayDataAsync(string symbol)
    {
        var result = new List<StockPriceData>();
        try
        {
            await WaitForRateLimit();
            var response = await _httpClient.GetAsync(
                $"{BaseUrl}?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=5min&apikey={_apiKey}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(jsonString);
                
                if (data?["Time Series (5min)"] is JObject timeSeries)
                {
                    foreach (var (dateStr, values) in timeSeries)
                    {
                        if (DateTime.TryParse(dateStr, out var date) &&
                            values is JObject priceData)
                        {
                            result.Add(new StockPriceData
                            {
                                Timestamp = date,
                                Open = ParseDecimal(priceData["1. open"]),
                                High = ParseDecimal(priceData["2. high"]),
                                Low = ParseDecimal(priceData["3. low"]),
                                Close = ParseDecimal(priceData["4. close"]),
                                Volume = ParseLong(priceData["5. volume"])
                            });
                        }
                    }
                }
                else if (data?["Note"] != null)
                {
                    Console.WriteLine("API Rate limit reached. Please wait a moment and try again.");
                    await Task.Delay(MinRequestInterval);
                }
                else if (data?["Error Message"] != null)
                {
                    Console.WriteLine("API Error: " + data["Error Message"]);
                }
            }
            else
            {
                Console.WriteLine($"API request failed with status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching intraday data: {ex.Message}");
        }

        return result.OrderBy(x => x.Timestamp).ToList();
    }
}
