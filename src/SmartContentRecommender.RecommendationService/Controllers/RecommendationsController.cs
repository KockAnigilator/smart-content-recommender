using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Recommendations.Interfaces;

namespace SmartContentRecommender.RecommendationService.Controllers;

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
        => Ok(await _recommendationService.GetPopularAsync(limit, cancellationToken));

    [HttpGet("by-categories")]
    [Authorize]
    public async Task<IActionResult> ByCategories([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        return userId is null
            ? Unauthorized()
            : Ok(await _recommendationService.GetByCategoriesAsync(userId.Value, limit, cancellationToken));
    }

    [HttpGet("knn")]
    [Authorize]
    public async Task<IActionResult> Knn([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        return userId is null
            ? Unauthorized()
            : Ok(await _recommendationService.GetKnnAsync(userId.Value, limit, cancellationToken));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

