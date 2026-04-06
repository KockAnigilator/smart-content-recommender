using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.UserActions.Interfaces;
using SmartContentRecommender.Application.UserActions.Models;

namespace SmartContentRecommender.RecommendationService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserActionsController : ControllerBase
{
    private readonly IUserActionService _userActionService;

    public UserActionsController(IUserActionService userActionService)
    {
        _userActionService = userActionService;
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] LogUserActionRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var success = await _userActionService.LogActionAsync(userId.Value, request, cancellationToken);
        return success ? Ok(new { Message = "Action logged" }) : BadRequest();
    }

    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var history = await _userActionService.GetUserHistoryAsync(userId.Value, limit, cancellationToken);
        return Ok(history);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

