using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegionsController : ControllerBase
{
    private readonly IRegionService _regions;
    public RegionsController(IRegionService regions) => _regions = regions;

    // GET: api/regions
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _regions.GetAllAsync());
}
