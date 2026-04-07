using System.Text.Json;
using Exchange.Models;

namespace Exchange.Services;

public interface IExchangeRateService
{
    Task<ExchangeRateViewModel> GetRatesAsync(string baseCurrency);
}

public class ExchangeRateService : IExchangeRateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateService> _logger;

    // open.er-api.com 免費方案，無需 API key
    private const string ApiBaseUrl = "https://open.er-api.com/v6/latest/{0}";

    public ExchangeRateService(IHttpClientFactory httpClientFactory, ILogger<ExchangeRateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ExchangeRateViewModel> GetRatesAsync(string baseCurrency)
    {
        var vm = new ExchangeRateViewModel { BaseCurrency = baseCurrency };

        try
        {
            var client = _httpClientFactory.CreateClient("ExchangeRate");
            var url = string.Format(ApiBaseUrl, baseCurrency);
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                vm.HasError = true;
                vm.ErrorMessage = $"API 回傳錯誤：HTTP {(int)response.StatusCode}";
                return vm;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var result) && result.GetString() != "success")
            {
                vm.HasError = true;
                vm.ErrorMessage = "API 回傳非成功狀態，請稍後再試。";
                return vm;
            }

            if (root.TryGetProperty("rates", out var rates))
            {
                if (baseCurrency == "TWD")
                {
                    vm.UsdRate = rates.TryGetProperty("USD", out var usd) ? usd.GetDecimal() : null;
                    vm.JpyRate = rates.TryGetProperty("JPY", out var jpy) ? jpy.GetDecimal() : null;
                }
                else // USD base
                {
                    vm.UsdRate = 1m; // base currency itself
                    vm.JpyRate = rates.TryGetProperty("JPY", out var jpy) ? jpy.GetDecimal() : null;

                    // 取得 TWD/USD 換算成 TWD base 顯示時用
                    // 這裡維持 USD base：直接顯示 1 USD = X JPY
                }
            }

            if (root.TryGetProperty("time_last_update_unix", out var ts))
            {
                vm.LastUpdated = DateTimeOffset.FromUnixTimeSeconds(ts.GetInt64()).UtcDateTime;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("匯率 API 請求逾時（base={Base}）", baseCurrency);
            vm.HasError = true;
            vm.ErrorMessage = "請求逾時，請稍後再試。";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "匯率 API 連線失敗（base={Base}）", baseCurrency);
            vm.HasError = true;
            vm.ErrorMessage = "無法連線至匯率服務，請確認網路或稍後再試。";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "匯率 API 回傳格式錯誤（base={Base}）", baseCurrency);
            vm.HasError = true;
            vm.ErrorMessage = "API 回傳資料格式錯誤，請稍後再試。";
        }

        return vm;
    }
}
