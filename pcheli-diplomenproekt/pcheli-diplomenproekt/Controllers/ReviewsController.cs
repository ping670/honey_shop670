using Microsoft.AspNetCore.Mvc;
using pcheli_diplomenproekt.Dtos;
using pcheli_diplomenproekt.Services;

namespace pcheli_diplomenproekt.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    public ReviewsController(IReviewService reviews) => _reviews = reviews;

    // GET: api/reviews – само одобрените
    [HttpGet]
    public async Task<IActionResult> GetApproved() => Ok(await _reviews.GetApprovedAsync());

    // POST: api/reviews – новият отзив чака модерация
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        await _reviews.CreateAsync(dto);
        return Ok(new { success = true, message = "Благодарим за отзива! Ще бъде публикуван след преглед от модератор." });
    }
}
