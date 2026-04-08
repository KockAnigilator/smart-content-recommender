using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Analytics.Interfaces;
using SmartContentRecommender.Application.UserActions.Interfaces;
using SmartContentRecommender.Application.UserActions.Models;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserActionsController : ControllerBase
{
    private readonly IUserActionService _userActionService;
    private readonly IAnalyticsService _analyticsService;

    public UserActionsController(IUserActionService userActionService, IAnalyticsService analyticsService)
    {
        _userActionService = userActionService;
        _analyticsService = analyticsService;
    }

    [HttpPost("log")]
    public async Task<IActionResult> LogAction([FromBody] LogUserActionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var success = await _userActionService.LogActionAsync(userId.Value, request, cancellationToken);
        if (!success)
        {
            return BadRequest("Не удалось сохранить действие: проверьте user/content.");
        }

        return Ok(new { Message = "Действие сохранено в историю." });
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var history = await _userActionService.GetUserHistoryAsync(userId.Value, limit, cancellationToken);
        return Ok(history);
    }

    [HttpGet("interest-profile")]
    public async Task<IActionResult> InterestProfile([FromQuery] int top = 5, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var profile = await _analyticsService.GetInterestProfileAsync(userId.Value, top, cancellationToken);
        return Ok(profile);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

