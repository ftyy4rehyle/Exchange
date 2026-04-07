namespace Exchange.Models;

public class ExchangeRateViewModel
{
    public string BaseCurrency { get; set; } = "TWD";
    public decimal? UsdRate { get; set; }
    public decimal? JpyRate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}
