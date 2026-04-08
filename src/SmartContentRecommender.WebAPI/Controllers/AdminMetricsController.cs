using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Recommendations.Interfaces;
using SmartContentRecommender.Application.Recommendations.Models;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/admin/metrics")]
[Authorize(Roles = "Admin")]
public class AdminMetricsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly IRecommendationMetricsService _metricsService;

    public AdminMetricsController(
        IRecommendationService recommendationService,
        IRecommendationMetricsService metricsService)
    {
        _recommendationService = recommendationService;
        _metricsService = metricsService;
    }

    [HttpGet("recommendations")]
    public async Task<ActionResult<RecommendationMetricsDto>> GetMetrics(
        [FromQuery] Guid userId,
        [FromQuery] string algorithm = "knn",
        [FromQuery] int k = 10,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest("userId обязателен.");
        }

        IReadOnlyList<RecommendationItemDto> items = algorithm.ToLowerInvariant() switch
        {
            "popular" => await _recommendationService.GetPopularAsync(k, cancellationToken),
            "by-categories" => await _recommendationService.GetByCategoriesAsync(userId, k, cancellationToken),
            _ => await _recommendationService.GetKnnAsync(userId, k, cancellationToken)
        };

        var contentIds = items.Select(i => i.ContentId).ToList();

        var metrics = await _metricsService.CalculateAsync(
            userId,
            algorithm,
            contentIds,
            k,
            cancellationToken);

        return Ok(metrics);
    }
}

