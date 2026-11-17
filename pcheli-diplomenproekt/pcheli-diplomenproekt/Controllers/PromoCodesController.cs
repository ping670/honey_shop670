using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromoCodesController : ControllerBase
{
    private readonly IPromoService _promos;
    public PromoCodesController(IPromoService promos) => _promos = promos;

    // GET: api/promocodes/validate?code=BG10
    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string code)
        => string.IsNullOrWhiteSpace(code) ? NotFound() : Ok(await _promos.ValidateAsync(code));
}
