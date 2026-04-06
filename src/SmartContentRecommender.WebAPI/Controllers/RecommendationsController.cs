using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Recommendations.Interfaces;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<IActionResult> Popular([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var result = await _recommendationService.GetPopularAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-categories")]
    [Authorize]
    public async Task<IActionResult> ByCategories([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var result = await _recommendationService.GetByCategoriesAsync(userId.Value, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("knn")]
    [Authorize]
    public async Task<IActionResult> Knn([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var result = await _recommendationService.GetKnnAsync(userId.Value, limit, cancellationToken);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

