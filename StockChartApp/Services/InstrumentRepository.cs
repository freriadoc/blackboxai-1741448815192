using Newtonsoft.Json;
using StockChartApp.Models;

namespace StockChartApp.Services;

public class InstrumentRepository
{
    private readonly string _storageFile;

    public InstrumentRepository(string storageFile = "instruments.json")
    {
        _storageFile = storageFile;
    }

    public async Task SaveInstrumentsAsync(List<FinancialInstrument> instruments)
    {
        var json = JsonConvert.SerializeObject(instruments, Formatting.Indented);
        await File.WriteAllTextAsync(_storageFile, json);
    }

    public async Task<List<FinancialInstrument>> LoadInstrumentsAsync()
    {
        if (!File.Exists(_storageFile))
        {
            return new List<FinancialInstrument>();
        }

        var json = await File.ReadAllTextAsync(_storageFile);
        return JsonConvert.DeserializeObject<List<FinancialInstrument>>(json) ?? new List<FinancialInstrument>();
    }

    public bool HasSavedInstruments()
    {
        return File.Exists(_storageFile);
    }
}
