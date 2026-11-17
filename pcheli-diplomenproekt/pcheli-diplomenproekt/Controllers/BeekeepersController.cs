using Microsoft.AspNetCore.Mvc;
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

    // POST: api/beekeepers/apply – записва заявка „В обработка“; публикува се след одобрение от администратор
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] BeekeeperApplyDto dto)
    {
        await _beekeepers.SubmitApplicationAsync(dto);
        return Ok(new
        {
            success = true,
            message = "Заявката Ви е приета и чака одобрение от администратор. Ще се свържем с Вас на посочения телефон."
        });
    }
}
