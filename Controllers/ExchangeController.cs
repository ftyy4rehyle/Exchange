using Exchange.Services;
using Microsoft.AspNetCore.Mvc;

namespace Exchange.Controllers;

public class ExchangeController : Controller
{
    private readonly IExchangeRateService _service;

    public ExchangeController(IExchangeRateService service)
    {
        _service = service;
    }

    // GET /Exchange/Index?base=TWD
    public async Task<IActionResult> Index(string @base = "TWD")
    {
        var allowed = new[] { "TWD", "USD" };
        if (!allowed.Contains(@base.ToUpper()))
            @base = "TWD";

        var vm = await _service.GetRatesAsync(@base.ToUpper());
        return View(vm);
    }
}
