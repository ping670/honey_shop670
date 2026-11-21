using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BeekeepersController : ControllerBase
{
    private readonly IBeekeeperService _beekeepers;
    public BeekeepersController(IBeekeeperService beekeepers) => _beekeepers = beekeepers;

    // GET: api/beekeepers?region=Родопи
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? region) => Ok(await _beekeepers.GetAllAsync(region));

    // GET: api/beekeepers/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id) => Ok(await _beekeepers.GetByIdAsync(id));

    // GET: api/beekeepers/5/detail – страница на пчелара: история, снимки, продукти и отзиви
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id) => Ok(await _beekeepers.GetDetailAsync(id));

    // POST: api/beekeepers/apply – записва заявка „В обработка“; публикува се след одобрение от администратор.
    // Ако кандидатът е влязъл в акаунта си, при одобрение получава достъп до личния панел на пчелара.
    [HttpPost("apply")]
    [EnableRateLimiting("forms")]
    [RequireCaptcha]
    public async Task<IActionResult> Apply([FromBody] BeekeeperApplyDto dto)
    {
        await _beekeepers.SubmitApplicationAsync(dto, User.FindFirstValue(ClaimTypes.NameIdentifier));
        return Ok(new
        {
            success = true,
            message = "Заявката Ви е приета и чака одобрение от администратор. Ще се свържем с Вас на посочения телефон."
        });
    }
}
